## 2026-02-22 - Canonical command-doc instructions
Context: Align duplicated command documentation guidance used by Codex skills and GitHub Copilot prompts.
Decision: Introduce a single canonical instruction file and convert both agent-specific files into thin wrappers with Canonical-Source and Canonical-Version markers.
Discovery: A lightweight PowerShell verifier can enforce wrapper-to-canonical sync and prevent silent drift.
Files: docs/agent-instructions/document-command.md, .codex/skills/document-command/SKILL.md, .github/prompts/doc-command.prompt.md, scripts/verify-agent-docs.ps1, .codex/workspace-diary.md
Impact: Future updates to command documentation guidance can be made once in canonical docs and validated automatically for both agent surfaces.
## 2026-02-22 - Document verifier inline help
Context: Improve maintainability of agent-doc drift checker by documenting script purpose and parameters.
Decision: Add PowerShell comment-based help and concise inline comments in verify-agent-docs script.
Discovery: Script remains CI-friendly; exit behavior and validation output unchanged after documentation updates.
Files: scripts/verify-agent-docs.ps1, .codex/workspace-diary.md
Impact: Team members can discover usage via Get-Help and understand script intent without reading full implementation.
## 2026-02-22 - Add DI instance-creation policy
Context: Establish repository-wide guidance for service instantiation and dependency injection consistency.
Decision: Add AGENTS.md directive to resolve behavior classes from DI, require interfaces for behavior classes, and allow `new` only for simple DTO/value carriers (prefer records).
Discovery: Clear exceptions are needed to avoid over-constraining value objects and data-only models.
Files: AGENTS.md, .codex/workspace-diary.md
Impact: Reduces ad-hoc object creation, improves testability, and enforces consistent dependency management.
## 2026-02-22 - Add CLIO001 DI manual-construction analyzer
Context: Enforce repository policy to avoid `new` for DI-registered behavior classes.
Decision: Create `Clio.Analyzers` project with `CLIO001` analyzer, wire it as Analyzer reference in `clio` and `cliogate`, and add analyzer project to `clio.slnx`.
Discovery: Analyzer infers registered implementations from Autofac and Microsoft.Extensions.DependencyInjection registration calls, and intentionally skips record types and test assemblies.
Files: Clio.Analyzers/Clio.Analyzers.csproj, Clio.Analyzers/DependencyInjectionManualConstructionAnalyzer.cs, clio/clio.csproj, cliogate/cliogate.csproj, clio.slnx, .codex/workspace-diary.md
Impact: Builds now flag manual `new` construction of DI-registered types with diagnostic `CLIO001`.
## 2026-02-22 - Migrate clio DI and JSON stack
Context: Replace Autofac with Microsoft.Extensions.DependencyInjection and move off System.Json in clio and clio.tests.
Decision: Ported the existing remove-autofac migration with targeted reconciliation on current branch, then completed remaining DI conversion callsites manually.
Discovery: Keeping BuildServiceProvider validation with ValidateOnBuild=false avoids premature DI graph failures while preserving runtime resolution behavior; test fixture overrides map cleanly to IServiceCollection + GetRequiredService patterns.
Files: clio/BindingsModule.cs, clio/Program.cs, clio/Command/CloneEnvironmentCommand.cs, clio/Command/ShowDiffEnvironmentsCommand.cs, clio/Command/ScenarioRunnerCommand.cs, clio/AppUpdater.cs, clio/ComposableApplication/ComposableApplicationManager.cs, clio/Package/PackageConverter.cs, clio/ExceptionReadableMessageExtension.cs, clio/clio.csproj, clio.tests/Command/BaseClioModuleTests.cs, clio.tests/GlobalUsings.cs, clio.tests/clio.tests.csproj
Impact: clio now uses Microsoft DI container APIs end-to-end, System.Json dependency is removed, and full clio.tests suite passes on net8.
## 2026-02-22 - Retarget CLIO001 analyzer to Microsoft DI
Context: After migrating clio away from Autofac, analyzer registration detection still included Autofac methods/namespaces.
Decision: Restrict CLIO001 registration-source detection to Microsoft.Extensions.DependencyInjection APIs and add support for TryAdd*/Replace method names.
Discovery: Existing type extraction logic for Add* generic overloads still works for IServiceCollection patterns; no Autofac-specific extraction paths are needed.
Files: Clio.Analyzers/DependencyInjectionManualConstructionAnalyzer.cs, .codex/workspace-diary.md
Impact: CLIO001 now reflects the current DI stack and avoids Autofac-based assumptions/false positives.
## 2026-02-22 - Stabilize CLIO001 matches after MS DI migration
Context: Manual `new` in `Command/AddSchema.cs` was not reported even though CLIO001 analyzer assembly was loaded and executed.
Decision: Extended CLIO001 registration matching from symbol-only to symbol+name fallback, preserving registration discovery from Microsoft DI methods while handling cases where type binding is not symbol-resolvable.
Discovery: `dotnet build` with analyzer reporting confirmed analyzer execution; diagnostics were missed by symbol-only matching in this code path and restored by name fallback.
Files: Clio.Analyzers/DependencyInjectionManualConstructionAnalyzer.cs, .codex/workspace-diary.md
Impact: CLIO001 now reports manual construction in AddSchema and other DI-registered usages more reliably under current MS DI setup.
## 2026-02-22 - Validate CLIO001 on RfsEnvironment
Context: User requested validation of CLIO001 warnings in `Command/RfsEnvironment.cs` after analyzer retargeting to MS DI.
Decision: Classified warnings by exact object-creation site: outer `new Clio.Common.FileSystem(...)` is a true positive, inner `new System.IO.Abstractions.FileSystem()` is a false positive.
Discovery: Name-based fallback in CLIO001 can collide on short type names (`FileSystem`) across namespaces, producing duplicate warnings on the same line/statement.
Files: clio/Command/RfsEnvironment.cs, Clio.Analyzers/DependencyInjectionManualConstructionAnalyzer.cs, .codex/workspace-diary.md
Impact: Future analyzer tightening should prefer fully-qualified type matching (or short-name matching only for unbound symbols) to avoid namespace-collision false positives.
## 2026-02-22 - Tighten CLIO001 matching and validate RfsEnvironment warnings
Context: User asked to proceed with analyzer tightening and validate CLIO001 findings in `Command/RfsEnvironment.cs`.
Decision: Updated CLIO001 to prioritize symbol/FQN matching, limit simple-name fallback to unresolved simple syntax, and include anonymous-method factory extraction; also switched registration collection to non-cancelled pass for stability.
Discovery: `BindingsModule` aliases `FileSystem` to `System.IO.Abstractions.FileSystem` and registers it (`AddTransient<IFileSystem, FileSystem>`), so inner `new System.IO.Abstractions.FileSystem()` warnings in RfsEnvironment are semantically consistent with current analyzer policy.
Files: Clio.Analyzers/DependencyInjectionManualConstructionAnalyzer.cs, clio/BindingsModule.cs, clio/Command/RfsEnvironment.cs, .codex/workspace-diary.md
Impact: CLIO001 behavior is more deterministic, but RfsEnvironment still reports four warnings because both concrete `FileSystem` types are currently DI-registered/treated as services.
## 2026-02-22 - Refactor RfsEnvironment to DI-managed filesystem
Context: User requested removing manual filesystem construction in `RfsEnvironment` and consuming `Clio.Common.FileSystem` through DI.
Decision: Converted `RfsEnvironment` to an injectable service with constructor dependencies `IFileSystem` and `IPackageUtilities`; replaced static `new FileSystem(...)`/`new PackageUtilities(...)` with injected services; updated link commands and wiring to resolve through DI.
Discovery: `Link2RepoCommand` was created via `CreateCommand` and had to be switched to `Resolve` in `Program` so constructor injection works.
Files: clio/Command/RfsEnvironment.cs, clio/Command/Link2RepoCommand.cs, clio/Command/Link4RepoCommand.cs, clio/Program.cs, clio/BindingsModule.cs, .codex/workspace-diary.md
Impact: `RfsEnvironment` no longer manually constructs filesystem helpers, and CLIO001 warnings for `RfsEnvironment.cs` are eliminated.
## 2026-02-22 - Add target-typed new support to CLIO001
Context: Analyzer reported `new EnvironmentSettings()` but missed `new()` for the same type.
Decision: Extended CLIO001 to analyze both `ObjectCreationExpression` and `ImplicitObjectCreationExpression` and resolve created type from semantic model for both forms.
Discovery: Enabling implicit object creation analysis surfaced additional existing manual-construction sites (many previously hidden due target-typed syntax).
Files: Clio.Analyzers/DependencyInjectionManualConstructionAnalyzer.cs, .codex/workspace-diary.md
Impact: CLIO001 now consistently detects DI manual construction for explicit and target-typed `new`.
## 2026-02-22 - Add CLIO001 attention directive to AGENTS
Context: User requested stronger repository guidance to prioritize DI and treat manual instance creation as last resort.
Decision: Added a dedicated `CLIO001 handling` section in `AGENTS.md` emphasizing DI-first, last-resort `new` usage, and justification for suppressions.
Discovery: Existing DI policy was broad; explicit CLIO001 guidance reduces ambiguity during warning triage.
Files: AGENTS.md, .codex/workspace-diary.md
Impact: Future work should consistently treat CLIO001 warnings as actionable and avoid normalizing manual construction patterns.
## 2026-02-22 - Add CLIO002 analyzer for Console output
Context: User requested a new analyzer to discourage direct `Console` output and encourage `ConsoleLogger` usage.
Decision: Added `CLIO002` analyzer that reports `System.Console.Write`/`WriteLine` invocations, excluding `Clio.*.ConsoleLogger` implementation.
Discovery: Running build immediately surfaces many existing direct console writes across command and utility classes, which confirms analyzer coverage.
Files: Clio.Analyzers/ConsoleOutputAnalyzer.cs, .codex/workspace-diary.md
Impact: Repository now has automatic enforcement pressure toward `ILogger`/`ConsoleLogger` instead of direct console printing.
## 2026-02-22 - Introduce CLIO severity policy in .editorconfig
Context: User requested scalable analyzer governance without documenting every rule in AGENTS.
Decision: Added `clio/.editorconfig` with CLIO severity tiers and explicit per-ID settings (`CLIO001` warning, `CLIO002` suggestion), and updated AGENTS to make `.editorconfig` the source of truth.
Discovery: Numeric ID ranges cannot be expressed directly in .editorconfig, so each new CLIO rule must be added explicitly while preserving the tier convention.
Files: clio/.editorconfig, AGENTS.md, .codex/workspace-diary.md
Impact: Analyzer severity management is centralized and scalable as CLIO analyzer set grows.

## 2026-02-22 - CLIO002 cleanup in LoadPackagesToFileSystemCommand
Context: User requested addressing CLIO002 in LoadPackagesToFileSystemCommand.
Decision: Replaced direct Console usage with injected ILogger in the command and updated the direct-construction test.
Discovery: Only one unit test instantiated this command directly (TurnFsmCommand login-retry test) and required constructor update.
Files: clio/Command/LoadPackagesToFileSystemCommand.cs, clio.tests/Command/TurnFsmCommand.LoginRetry.Tests.cs
Impact: CLIO002 is removed from this command while keeping behavior and tests intact.

## 2026-02-22 - Refactor NugetPackagesProvider logging, DI HttpClient, and JSON parser
Context: User requested fixing CLIO002 warnings in NugetPackagesProvider and migrating its HTTP/JSON flow to DI + System.Text.Json.
Decision: Replaced direct Console output with ILogger, added constructor injection for HttpClient/ILogger, switched versions parsing from Newtonsoft JObject/JToken to System.Text.Json model binding, and registered typed client via AddHttpClient<INugetPackagesProvider, NugetPackagesProvider>.
Discovery: Registering plain HttpClient globally introduced broad CLIO001 fallout, so typed HttpClient registration was used to scope DI creation to INugetPackagesProvider.
Files: clio/Package/NuGet/NugetPackagesProvider.cs, clio/BindingsModule.cs, clio/clio.csproj, .codex/workspace-diary.md
Impact: The targeted CLIO002 warnings are resolved, NugetPackagesProvider now follows DI for HttpClient, and Newtonsoft dependency is removed from this provider path.

## 2026-02-22 - Add NugetPackagesProvider tests with DelegatingHandler and DI wiring
Context: User requested coverage for NugetPackagesProvider with mocked DI and DelegatingHandler-based HTTP responses.
Decision: Added a dedicated NUnit fixture that registers INugetPackagesProvider via AddHttpClient and injects a custom DelegatingHandler from DI to control responses; covered success, no-versions warning, and HTTP-error logging paths.
Discovery: Existing PackageVersion ordering treats "-rc" as greater than empty suffix, so Last resolves to rc in current implementation and tests were aligned to current behavior.
Files: clio.tests/Package/NuGet/NugetPackagesProvider.Tests.cs, clio/Package/NuGet/NugetPackagesProvider.cs, .codex/workspace-diary.md
Impact: NugetPackagesProvider behavior is now regression-protected and demonstrates project-preferred DI+HttpClient testing approach.

## 2026-02-22 - Add test style directives to AGENTS
Context: User requested stricter test authoring rules for consistency and readability.
Decision: Added a dedicated Test style policy section requiring explicit AAA structure, `because` in every assertion, and `[Description]` on every test method.
Discovery: Existing AGENTS guidance covered DI/analyzers/docs but did not define shared test-writing conventions.
Files: AGENTS.md, .codex/workspace-diary.md
Impact: Future tests should follow uniform structure and clearer intent documentation.

## 2026-02-22 - Align NugetPackagesProvider tests with AGENTS test policy
Context: User requested applying new test style directives to NugetPackagesProvider tests.
Decision: Updated tests to explicit AAA sections, added Description attribute to each test, and ensured all assertions include because; replaced NSubstitute Received/DidNotReceive checks with counted call assertions supporting because.
Discovery: Log verification via ReceivedCalls allows assertion-style checks that comply with mandatory because rule.
Files: clio.tests/Package/NuGet/NugetPackagesProvider.Tests.cs, .codex/workspace-diary.md
Impact: NugetPackagesProvider tests now comply with repository testing conventions and remain green.

## 2026-02-22 - Refactor NugetPackagesProvider tests to BaseClioModuleTests DI flow
Context: User requested resolving INugetPackagesProvider from shared test container and registering StubDelegatingHandler + provider in DI.
Decision: Reworked NugetPackagesProviderTests to inherit BaseClioModuleTests, override AdditionalRegistrations to add singleton logger/handler and typed AddHttpClient registration, and resolve provider from Container in each test.
Discovery: Per-test container recreation via BaseClioModuleTests keeps handler/logger state isolated while enabling DI-first test composition.
Files: clio.tests/Package/NuGet/NugetPackagesProvider.Tests.cs, .codex/workspace-diary.md
Impact: Tests now follow repository DI testing style and validate the actual resolution path used by BindingsModule-based setups.

## 2026-02-22 - Remove Task covariance in NuGet provider
Context: Address compiler warning about co-variant array conversion in NugetPackagesProvider.
Decision: Replaced Task.WaitAll(Task<T>[]) usage with Task.WhenAll(tasks).GetAwaiter().GetResult() to keep strong typing.
Discovery: Task.WaitAll requires Task[], which triggers array covariance from Task<T>[]; Task.WhenAll avoids that path.
Files: clio/Package/NuGet/NugetPackagesProvider.cs
Impact: Removes potential runtime array type mismatch risk and clears analyzer/compiler warning.

## 2026-02-22 - Convert NuGet provider API to async/await
Context: User requested replacing blocking .GetAwaiter().GetResult() in NugetPackagesProvider with async/await.
Decision: Changed INugetPackagesProvider methods to Task-based signatures and updated provider implementation to wait Task.WhenAll(tasks).
Discovery: Synchronous call sites in NuGet manager/restorer now bridge via .Result until broader async propagation is done.
Files: clio/Package/NuGet/INugetPackagesProvider.cs, clio/Package/NuGet/NugetPackagesProvider.cs, clio/Package/NuGet/NuGetManager.cs, clio/Package/NuGet/NugetPackageRestorer.cs, clio.tests/Package/NuGet/NugetPackagesProvider.Tests.cs
Impact: Provider no longer blocks with GetAwaiter().GetResult() and keeps async flow where version aggregation happens.
