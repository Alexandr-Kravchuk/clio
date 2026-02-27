---
name: clio
description: 'CLI tool for Creatio platform integration with development and CI/CD workflows. Use when asked to manage Creatio environments, install or push packages, create workspaces, compile configuration, deploy applications, run SQL scripts, manage system settings, restart Creatio, work with NuGet packages, apply GitOps manifests, or perform any Creatio development task. Triggers on mentions of clio, Creatio, Terrasoft, creatio packages, creatio environment, creatio workspace, or bpm online.'
---

# Clio — Creatio CLI Tool

Clio is a command-line utility for integrating the Creatio platform with development and CI/CD tools. It manages environments, packages, workspaces, applications, and infrastructure for Creatio instances.

**Repository**: https://github.com/Advance-Technologies-Foundation/clio
**Full command reference**: See `references/commands-reference.md` bundled with this skill.

## Prerequisites

- .NET 8 SDK installed
- Clio installed: `dotnet tool install clio -g`
- For advanced features: cliogate installed on Creatio instance (`clio install-gate -e <ENV>`)

Verify installation:
```bash
clio info
# or
clio ver
```

## When to Use This Skill

- User mentions **clio**, **Creatio**, **Terrasoft**, or **bpm'online**
- User wants to manage Creatio **environments** (register, ping, healthcheck)
- User wants to **install, push, pull, or compile** Creatio packages
- User asks about **workspaces** (create, restore, push, build)
- User needs to **restart**, **compile configuration**, or manage **system settings**
- User wants to **deploy applications**, manage **licenses**, or work with **features**
- User asks about **CI/CD** with Creatio, GitOps manifests, or deployment scenarios
- User wants to execute **SQL scripts** or **service calls** against Creatio
- User needs to set up **infrastructure** (Kubernetes, Docker) for Creatio

## General Syntax

```bash
clio <COMMAND> [arguments] [command_options]
```

Common options for most commands:
- `-e <ENV>` — environment name (from registered environments)
- `-u <URI>` — Creatio application URL
- `-l <LOGIN>` — user login
- `-p <PASSWORD>` — user password

## Core Workflows

### 1. Environment Setup

Register and manage Creatio environments:

```bash
# Register environment
clio reg-web-app myenv -u https://mysite.creatio.com -l administrator -p password

# Set active environment
clio reg-web-app -a myenv

# List all environments
clio show-web-app-list --short

# Ping to verify
clio ping myenv
# or: clio ping-app myenv

# Health check
clio healthcheck myenv

# Get instance info (requires cliogate)
clio get-info -e myenv

# Interactive environment manager (TUI)
clio env-ui

# Open in browser
clio open myenv

# Remove environment
clio unreg-web-app myenv
```

### 2. Package Management

Create, install, pull, and manage packages:

```bash
# Create new package
clio new-pkg MyPackage

# Install package from directory
clio push-pkg MyPackage -e myenv

# Install .gz package
clio push-pkg package.gz -e myenv

# Install from marketplace by ID
clio push-pkg --id 22966 -e myenv

# For composable apps use push-app
clio push-app package.gz -e myenv

# Download package from environment
clio pull-pkg MyPackage -e myenv

# Compile package
clio compile-package MyPackage -e myenv

# Delete package
clio delete-pkg-remote MyPackage -e myenv

# List installed packages
clio get-pkg-list -e myenv
clio get-pkg-list -e myenv -f CustomPrefix -j

# Compress/extract
clio generate-pkg-zip MyPackage
clio extract-pkg-zip package.gz -d ./output

# Lock/unlock
clio lock-package MyPackage -e myenv
clio unlock-package MyPackage -e myenv

# Package version
clio set-pkg-version ./MyPackage -v 1.2.0
clio get-pkg-version ./MyPackage

# Activate/deactivate (8.1.2+)
clio activate-pkg MyPackage -e myenv
clio deactivate-pkg MyPackage -e myenv

# Hotfix mode
clio pkg-hotfix MyPackage true -e myenv
```

### 3. Application Management

Control Creatio application lifecycle:

```bash
# Restart application
clio restart-web-app myenv

# Start/stop local Creatio
clio start -e myenv
clio stop -e myenv
clio stop --all --silent

# Clear Redis cache
clio clear-redis-db myenv

# Compile all configuration
clio compile-configuration -e myenv
clio compile-configuration --all -e myenv

# Get compilation log
clio last-compilation-log -e myenv

# System settings
clio set-syssetting MySetting "MyValue" -e myenv
clio get-syssetting MySetting --GET -e myenv

# Developer mode
clio set-dev-mode true -e myenv

# Features
clio set-feature MyFeature 1 -e myenv
clio set-feature MyFeature 0 -e myenv

# Web service URL
clio set-webservice-url ServiceName https://api.example.com -e myenv
clio get-webservice-url -e myenv

# Applications
clio get-app-list -e myenv
clio download-application MyApp -e myenv
clio deploy-application MyApp -e source -d target
clio install-application ./MyApp.gz -e myenv
clio uninstall-app-remote MyApp -e myenv

# Upload license
clio upload-license license.lic -e myenv

# File system mode
clio pkg-to-file-system -e myenv
clio pkg-to-db -e myenv
```

### 4. Workspace Development

Professional development flow with workspaces:

```bash
# Create workspace (connected to environment)
clio create-workspace -e myenv

# Create empty workspace
clio create-workspace my-workspace --empty

# Restore workspace from environment
clio restore-workspace -e myenv

# Build workspace
clio build-workspace

# Push to environment
clio push-workspace -e myenv

# Configure workspace packages (canonical: cfg-worspace)
clio cfgw --Packages Pkg1,Pkg2 -e myenv

# Download configuration (libraries)
clio download-configuration -e myenv
clio dconf --build path/to/creatio.zip

# Install cliogate (required for advanced features)
clio install-gate -e myenv

# Install T.I.D.E.
clio install-tide -e myenv

# Merge workspaces
clio merge-workspaces --workspaces path1,path2 -e myenv

# Publish workspace
clio publish-workspace --file ./output.zip --repo-path ./workspace

# Link to file design mode
clio link-from-repository -e myenv --repoPath ./packages --packages "*"
```

### 5. Development Tools

Code generation, SQL, service calls:

```bash
# Add item from template
clio add-item service MyService -n MyCompany.Services
clio add-item entity-listener MyListener -n MyCompany.Listeners

# Generate ATF model
clio add-item model Contact -f Name,Email -n MyNameSpace -d . -e myenv

# Generate all models
clio add-item model -n MyCompany.Models -e myenv

# Generate process model for ATF.Repository
clio generate-process-model MyProcess -n MyNameSpace -e myenv

# Add schema
clio add-schema MySchema -t source-code -p MyPackage

# Create test project
clio new-test-project --package MyPackage

# Execute SQL
clio execute-sql-script "SELECT Id FROM SysSettings WHERE Code = 'CustomPackageId'" -e myenv
clio execute-sql-script -f query.sql -e myenv

# Call service (GET)
clio call-service --service-path ServiceModel/AppInfoService.svc/GetInfo -e myenv

# Call service (POST with inline body)
clio call-service --service-path ServiceModel/YourService.svc/Method \
  --body '{"key":"value"}' -e myenv

# DataService
clio ds -t select --body '{"rootSchemaName":"Contact","operationType":0}' -e myenv
clio ds -t insert --body '{"rootSchemaName":"Contact","values":{"Name":"John"}}' -e myenv

# Convert package to project
clio convert MyPackage

# Set references
clio ref-to src
clio ref-to bin

# Switch NuGet to DLL
clio nuget2dll MyPackage

# Mock data for tests
clio mock-data -m ./Models -d ./TestData -e myenv

# Listen to logs
clio listen --loglevel Debug -e myenv

# Show package files (canonical: show-package-file-content)
clio show-files --package MyPackage -e myenv
```

### 6. NuGet Package Management

```bash
# Pack Creatio package as NuGet
clio pack-nuget-pkg ./MyPackage

# Push to NuGet repository
clio push-nuget-pkg ./MyPackage.nupkg --ApiKey KEY --Source URL

# Restore NuGet package
clio restore-nuget-pkg PackageName

# Install NuGet to Creatio
clio install-nuget-pkg PackageName -e myenv

# Check for updates
clio check-nuget-update
```

### 7. CI/CD & GitOps

```bash
# Apply manifest to instance
clio apply-manifest manifest.yaml -e myenv

# Save instance state to manifest
clio save-state manifest.yaml -e myenv

# Compare two environments
clio show-diff --source production --target qa
clio show-diff --source production --target qa --file diff.yaml

# Run automation scenario
clio run --file-name scenario.yaml

# Clone environment
clio clone-env --source Dev --target QA
```

### 8. Infrastructure & Deployment

```bash
# Deploy Kubernetes infrastructure (PostgreSQL, Redis, pgAdmin)
clio deploy-infrastructure

# Generate K8s files with custom resources
clio create-k8-files --pg-limit-memory 8Gi --pg-limit-cpu 4

# Deploy Creatio from ZIP
clio deploy-creatio --ZipFile ~/Downloads/creatio.zip

# List deployed hosts
clio hosts

# Uninstall Creatio
clio uninstall-creatio -e myenv

# Delete infrastructure
clio delete-infrastructure

# Restore database
clio restore-db --dbServerName my-local-postgres --dbName mydb --backupPath backup.backup

# Update clio
clio update-cli
```

## Troubleshooting

| Issue | Solution |
|-------|----------|
| `clio` not found | Run `dotnet tool install clio -g` and ensure `~/.dotnet/tools` is in PATH |
| Ping fails | Check URL, credentials, and network. Use `clio ping <ENV>` |
| cliogate required | Install with `clio install-gate -e <ENV>` |
| Compilation errors | Check `clio last-compilation-log -e <ENV>` |
| Permission denied | Ensure administrator-level Creatio credentials |
| Package locked | Unlock with `clio unlock-package <PKG> -e <ENV>` |

## Important Notes

- Always verify command options with `clio <CMD> --help` — it is the authoritative source
- Always use `-e <ENV>` to target a specific registered environment
- For composable applications use `push-app` instead of `push-pkg`
- cliogate package is required for many advanced features (workspace, get-info, etc.)
- T.I.D.E. requires cliogate: install with `clio install-tide -e <ENV>`
- Use `clio help` for full command list, `clio <CMD> --help` for command details
- Manifest YAML files support GitOps: apps, syssettings, features, webservices
