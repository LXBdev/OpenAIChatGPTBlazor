# .NET 10.0 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that a .NET 10.0 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 10.0 upgrade.
3. Upgrade OpenAIChatGPTBlazor\OpenAIChatGPTBlazor.csproj
4. Upgrade Tests\UiTests\UiTests.csproj
5. Update global.json to .NET 10.0 SDK version
6. Update OpenAIChatGPTBlazor\Dockerfile to use .NET 10.0 base images
7. Update Tests\UiTests\Dockerfile to use .NET 10.0 base images
8. Update docker-compose.yml (if Docker base images need updating)
9. Update .github\workflows\azure-webapps-dotnet-core.yml to use .NET 10.0
10. Update .github\workflows\ui-tests.yml to use .NET 10.0
11. Run tests in Tests\UiTests\UiTests.csproj

## Settings

This section contains settings and data used by execution steps.

### Excluded projects

No projects are excluded from this upgrade.

### Aggregate NuGet packages modifications across all projects

NuGet packages used across all selected projects or their dependencies that need version update in projects that reference them.

| Package Name                                        | Current Version           | New Version                  | Description                                           |
|:----------------------------------------------------|:-------------------------:|:----------------------------:|:------------------------------------------------------|
| Aspire.Azure.AI.OpenAI                              | 9.4.1-preview.1.25408.4   | 13.0.0-preview.1.25560.3     | Recommended for .NET 10.0                             |
| Microsoft.VisualStudio.Azure.Containers.Tools.Targets | 1.21.0                  |                              | Remove (no supported version for .NET 10.0)           |

### Project upgrade details

This section contains details about each project upgrade and modifications that need to be done in the project.

#### OpenAIChatGPTBlazor\OpenAIChatGPTBlazor.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - Aspire.Azure.AI.OpenAI should be updated from `9.4.1-preview.1.25408.4` to `13.0.0-preview.1.25560.3` (*recommended for .NET 10.0*)
  - Microsoft.VisualStudio.Azure.Containers.Tools.Targets should be removed (*no supported version for .NET 10.0*)

#### Tests\UiTests\UiTests.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - Microsoft.VisualStudio.Azure.Containers.Tools.Targets should be removed (*no supported version for .NET 10.0*)

#### Infrastructure files modifications

The following files need to be updated to support .NET 10.0:

- **global.json**: Update SDK version from `9.0.100` to `10.0.100` (or latest available .NET 10 SDK version)
- **OpenAIChatGPTBlazor\Dockerfile**: Update base images from `mcr.microsoft.com/dotnet/aspnet:9.0` and `mcr.microsoft.com/dotnet/sdk:9.0` to version `10.0`
- **Tests\UiTests\Dockerfile**: Update base images from `mcr.microsoft.com/dotnet/sdk:9.0` to version `10.0`
- **.github\workflows\azure-webapps-dotnet-core.yml**: Workflow uses `global.json`, so it will automatically use .NET 10.0 SDK after global.json is updated
- **.github\workflows\ui-tests.yml**: Workflow uses `global.json`, so it will automatically use .NET 10.0 SDK after global.json is updated
