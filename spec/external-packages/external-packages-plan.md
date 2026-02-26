# External Packages in Workspace Settings — Implementation Plan

## 1. Summary of Requirements (from spec)

The `workspaceSettings.json` file gets one new property: **`ExternalPackages`**. The existing **`IgnorePackages`** property is reused for exclusion logic.

```json
{
  "Packages": ["CrtFinservAppMgmt", "..."],
  "ExternalPackages": ["CommonModule"],
  "IgnorePackages": ["CrtCustomer360"],
  "ApplicationVersion": "1.0.0"
}
```

### Behavior Matrix

| Command | Packages | ExternalPackages | ExternalPackages deps | IgnorePackages |
|---|---|---|---|---|
| `publish-app` (ZIP) | ✅ Include | ✅ Include | ✅ Include (if in `../packages` and not ignored) | ❌ Exclude |
| `pushw` (push-workspace) | ✅ Push | ❌ Skip | ❌ Skip | ❌ Skip |
| `restorew` (restore-workspace) | ✅ Download | ❌ Skip | ❌ Skip | ❌ Skip |

### Dependency Resolution Rule
For each package in `ExternalPackages`, read its `descriptor.json` → `DependsOn` list. Include each dependency in the ZIP **only if**:
1. The dependency package folder exists in `../packages` (relative to workspace packages folder)
2. The dependency is **not** in `IgnorePackages`
3. The dependency is **not** already in `Packages` (avoid duplicates)

---

## 2. Current State Analysis

### What exists:
- `WorkspaceSettings` class ([clio/Workspace/WorkspaceSettings.cs](../../clio/Workspace/WorkspaceSettings.cs)) — has `Packages`, `ApplicationVersion`, `IgnorePackages`
- `WorkspacePackageFilter` ([clio/Workspace/WorkspacePackageFilter.cs](../../clio/Workspace/WorkspacePackageFilter.cs)) — filters by `IgnorePackages` patterns
- `Workspace.PublishToFile` / `PublishToFolder` ([clio/Workspace/Workspace.cs](../../clio/Workspace/Workspace.cs)) — creates ZIP from filtered packages
- `Workspace.Install` (pushw) — pushes filtered packages
- `WorkspaceRestorer.Restore` (restorew) — downloads `WorkspaceSettings.Packages`
- `PackageDescriptor` / `PackageDependency` models exist ([clio/Package/PackageDescriptor.cs](../../clio/Package/PackageDescriptor.cs))

### What does NOT exist:
- `ExternalPackages` property on `WorkspaceSettings`
- Dependency resolution logic reading `descriptor.json` → `DependsOn`
- Logic to find packages in `../packages` folder
- Exclusion of `ExternalPackages` from pushw / restorew
- `IgnorePackages` filtering in `restorew` (currently only applied in pushw and publish)

---

## 3. Implementation Plan

### Phase 1: Model Changes

#### Task 1.1 — Extend `WorkspaceSettings`
**File:** `clio/Workspace/WorkspaceSettings.cs`

Add one new property (`IgnorePackages` already exists):
```csharp
public IList<string> ExternalPackages { get; set; } = new List<string>();
```

**Risk:** Low. JSON deserialization will auto-map this from `workspaceSettings.json`. Missing property defaults to empty list.

**Note:** `IgnorePackages` already exists and supports wildcard patterns. It will be reused for package exclusion logic — no new exclusion property needed.

---

### Phase 2: External Package Dependency Resolution

#### Task 2.1 — Create `IExternalPackageDependencyResolver` interface
**File:** `clio/Workspace/IExternalPackageDependencyResolver.cs` (new)

```csharp
public interface IExternalPackageDependencyResolver
{
    /// <summary>
    /// Resolves transitive dependencies for external packages.
    /// Reads descriptor.json for each external package, finds dependencies
    /// in the external packages folder, and excludes packages from the exclusion list.
    /// </summary>
    IEnumerable<string> ResolveDependencies(
        IEnumerable<string> externalPackages,
        IEnumerable<string> ignorePackages,
        IEnumerable<string> alreadyIncludedPackages,
        string externalPackagesPath);
}
```

#### Task 2.2 — Implement `ExternalPackageDependencyResolver`
**File:** `clio/Workspace/ExternalPackageDependencyResolver.cs` (new)

Logic:
1. For each package in `externalPackages`:
   a. Build path: `{externalPackagesPath}/{packageName}/descriptor.json`
   b. Deserialize `descriptor.json` → read `DependsOn` list
   c. For each dependency in `DependsOn`:
      - Check if `{externalPackagesPath}/{dependencyName}` folder exists
      - Check if dependency is NOT matched by `ignorePackages` patterns
      - Check if dependency is NOT in `alreadyIncludedPackages`
      - If all checks pass → add to result
2. Consider recursive dependency resolution (dependencies of dependencies)
3. Use `IFileSystem` abstraction for all filesystem operations
4. Use `IJsonConverter` for JSON deserialization

**Dependencies:** `IFileSystem`, `IJsonConverter`, `ILogger`

#### Task 2.3 — Register in DI container
**File:** `clio/BindingsModule.cs`

Register `ExternalPackageDependencyResolver` as `IExternalPackageDependencyResolver`.

---

### Phase 3: Modify `publish-app` Command

#### Task 3.1 — Update `Workspace.PublishToFile` and `Workspace.PublishToFolder`
**File:** `clio/Workspace/Workspace.cs`

Current flow:
```
Packages → IncludedPackages → FilterPackages → Publish
```

New flow:
```
1. Start with Packages (filtered by IgnorePackages)
2. Add ExternalPackages to result
3. Resolve ExternalPackages dependencies → add to result (skip if matched by IgnorePackages)
4. Publish all to ZIP
```

**Note:** `IgnorePackages` already handles exclusion in step 1 via `GetFilteredPackages()`. Steps 2-3 add external packages and their deps, skipping any that match `IgnorePackages` patterns.

Inject `IExternalPackageDependencyResolver` into `Workspace` constructor.

#### Task 3.2 — Create helper method `GetPublishPackages()`
**File:** `clio/Workspace/Workspace.cs`

```csharp
public IEnumerable<string> GetPublishPackages()
{
    var filtered = GetFilteredPackages(); // already applies IgnorePackages
    
    var externalDeps = _dependencyResolver.ResolveDependencies(
        WorkspaceSettings.ExternalPackages,
        WorkspaceSettings.IgnorePackages,
        filtered,
        GetExternalPackagesPath());
    
    return filtered
        .Concat(WorkspaceSettings.ExternalPackages)
        .Concat(externalDeps)
        .Distinct();
}
```

#### Task 3.3 — Determine external packages folder path
**File:** `clio/Workspace/WorkspacePathBuilder.cs` (or `Workspace.cs`)

Add property/method to compute the external packages path (`../packages` relative to workspace packages folder). Verify the convention with the team.

---

### Phase 4: Modify `pushw` and `restorew` Commands

#### Task 4.1 — Update `Workspace.Install` (pushw)
**File:** `clio/Workspace/Workspace.cs`

Ensure `GetFilteredPackages()` (which feeds `Install`) also excludes `ExternalPackages`:
```csharp
public IEnumerable<string> GetFilteredPackages()
{
    return _workspacePackageFilter.FilterPackages(WorkspaceSettings.Packages, WorkspaceSettings)
        .Where(p => !WorkspaceSettings.ExternalPackages.Contains(p));
}
```

**Note:** `IgnorePackages` filtering is already applied by `FilterPackages()`. We only need to additionally exclude `ExternalPackages` since they are external (not developed in this workspace).

#### Task 4.2 — Update `WorkspaceRestorer.Restore` (restorew)
**File:** `clio/Workspace/WorkspaceRestorer.cs`

Currently downloads `workspaceSettings.Packages` directly. Must filter out `ExternalPackages` and apply `IgnorePackages`:
```csharp
var packagesToDownload = _workspacePackageFilter
    .FilterPackages(workspaceSettings.Packages, workspaceSettings)
    .Where(p => !workspaceSettings.ExternalPackages.Contains(p));
_packageDownloader.DownloadPackages(packagesToDownload, ...);
```

**Note:** `FilterPackages` applies `IgnorePackages` patterns. Additionally exclude `ExternalPackages` since they should not be downloaded from the server.

---

### Phase 5: Verify `IgnorePackages` + `ExternalPackages` in Existing Commands

#### Task 5.1 — Audit all workspace commands
Verify that `IgnorePackages` and `ExternalPackages` are respected in:
- [x] `publish-app` / `publishw` (Phase 3)
- [x] `pushw` / `push-workspace` (Phase 4)
- [x] `restorew` / `restore-workspace` (Phase 4)
- [ ] `build-workspace` — check if it also needs filtering
- [ ] `merge-workspaces` — check if it also needs filtering
- [ ] Any other command that iterates `WorkspaceSettings.Packages`

---

### Phase 6: Unit Tests

#### Task 6.1 — Tests for `WorkspaceSettings` deserialization
**File:** `clio.tests/WorkspaceSettingsTests.cs` (new or extend existing)

- Deserialize JSON with `ExternalPackages` → property populated
- Deserialize JSON without `ExternalPackages` → defaults to empty list
- Roundtrip serialization preserves `ExternalPackages`
- `IgnorePackages` continues to work as before (already tested)

#### Task 6.2 — Tests for `ExternalPackageDependencyResolver`
**File:** `clio.tests/ExternalPackageDependencyResolverTests.cs` (new)

Test cases (equivalence classes):
- External package has no dependencies → empty result
- External package has dependencies, all exist in `../packages` → all included
- External package has dependencies, some don't exist → only existing included
- External package dependency matches `IgnorePackages` pattern → excluded
- External package dependency is already in `Packages` → not duplicated
- External package descriptor.json doesn't exist → graceful handling
- External package descriptor.json is malformed → graceful handling
- Multiple external packages with overlapping dependencies → no duplicates
- Recursive dependencies (dep of dep) → resolved correctly (if implemented)

#### Task 6.3 — Tests for publish-app with external packages
**File:** `clio.tests/PublishWorkspaceCommandTests.cs` (extend existing)

- Publish includes packages from `Packages`
- Publish includes packages from `ExternalPackages`
- Publish includes resolved dependencies of `ExternalPackages`
- Publish excludes packages matching `IgnorePackages` patterns
- Publish excludes dependencies matching `IgnorePackages` patterns

#### Task 6.4 — Tests for pushw exclusions
**File:** `clio.tests/PushWorkspaceCommandTests.cs` (extend existing)

- pushw does NOT push `ExternalPackages`
- pushw does NOT push packages matching `IgnorePackages`

#### Task 6.5 — Tests for restorew exclusions
**File:** `clio.tests/RestoreWorkspaceCommandTests.cs` (extend existing)

- restorew does NOT download `ExternalPackages`
- restorew does NOT download packages matching `IgnorePackages`

---

### Phase 7: Documentation

#### Task 7.1 — Update `Commands.md`
**File:** `clio/Commands.md`

Add documentation for `ExternalPackages` in the Workspace section. Update `IgnorePackages` docs to cover new exclusion behavior in `restorew`. Explain behavior per command.

#### Task 7.2 — Update CLI help
**File:** `clio/help/en/publish-app.txt` (if exists)

Add description of new workspace settings fields.

#### Task 7.3 — Update `README.md`
**File:** `README.md`

Add section about external packages support.

---

## 4. Open Questions

| # | Question | Impact | Suggested Default |
|---|---|---|---|
| 1 | Should dependency resolution be **recursive** (deps of deps)? | High — could significantly expand included packages | Start with single-level, add recursive later |
| 2 | What is the exact path for external packages? `../packages` relative to what? | Critical — determines where to look for dependencies | Relative to workspace `packages/` folder |
| 3 | Should `ExternalPackages` themselves be located in `../packages`? | High — affects folder structure assumption | Yes, external packages live in `../packages` |
| 4 | ~~Should `ExcludedPackages` support wildcard patterns?~~ | Resolved — reusing `IgnorePackages` | `IgnorePackages` already supports wildcards |
| 5 | Should `build-workspace` and `merge-workspaces` also respect these new properties? | Medium — scope creep risk | Yes, for consistency |
| 6 | How to handle version conflicts between `Packages` deps and `ExternalPackages` deps? | Low for v1 — can be deferred | Log warning, include latest |

---

## 5. Implementation Order & Dependencies

```
Phase 1 (Model) ──────────────────────────┐
                                           ▼
Phase 2 (Dependency Resolver) ────────► Phase 3 (publish-app)
                                           │
Phase 4 (pushw / restorew) ◄──────────────┘
         │
         ▼
Phase 5 (Audit other commands)
         │
         ▼
Phase 6 (Unit Tests) ── runs in parallel with each phase
         │
         ▼
Phase 7 (Documentation)
```

**Estimated effort:** 3–5 days for full implementation with tests and docs.

---

## 6. Files to Create / Modify

### New files:
| File | Purpose |
|---|---|
| `clio/Workspace/IExternalPackageDependencyResolver.cs` | Interface for dependency resolution |
| `clio/Workspace/ExternalPackageDependencyResolver.cs` | Implementation |
| `clio.tests/ExternalPackageDependencyResolverTests.cs` | Tests for resolver |

### Modified files:
| File | Change |
|---|---|
| `clio/Workspace/WorkspaceSettings.cs` | Add `ExternalPackages` property (reuse existing `IgnorePackages`) |
| `clio/Workspace/Workspace.cs` | New `GetPublishPackages()`, update `Install`, inject resolver |
| `clio/Workspace/WorkspaceRestorer.cs` | Filter out external/ignored from download |
| `clio/Workspace/WorkspacePathBuilder.cs` | Add external packages path |
| `clio/BindingsModule.cs` | Register new service |
| `clio.tests/WorkspacePackageFilterTests.cs` | Extend with ExternalPackages exclusion tests |
| `clio.tests/PublishWorkspaceCommandTests.cs` | Extend with external packages tests |
| `clio/Commands.md` | Document new properties |
| `README.md` | Document new feature |
