# Repository Guidelines

## Project Structure & Module Organization

This is a single .NET 10 ASP.NET Core Web API solution. `FlexRadioServices.sln`
is the solution entry point and `FlexRadioServices/` contains the application.
Keep HTTP endpoints in `Controllers/`, radio, CAT, and MQTT coordination in
`Services/`, and transport/domain data in `Models/`. Configuration binding types
live in `Models/Settings/`; shared helpers belong in `Utils/`; event argument
types belong in `Events/`. `Program.cs` composes dependency injection, API
versioning, Swagger, and hosted services. The `Example/` directory provides
sample user configuration and is intentionally excluded from compilation.

## Build, Test, and Development Commands

- `dotnet restore FlexRadioServices.sln` restores the NuGet dependencies.
- `dotnet build FlexRadioServices.sln -c Release` builds the deployable API.
- `dotnet run --project FlexRadioServices/FlexRadioServices.csproj` starts the
  service locally using the active ASP.NET Core environment.
- `dotnet test FlexRadioServices.sln` is the standard test command; no test
  project is currently committed, so add one when introducing automated tests.
- `docker compose -f docker-compose.yml -f docker-compose.dev.yml up -d`
  builds and starts the development container. It requires `.env` values for
  `GITHUB_USERNAME` and `GITHUB_TOKEN` to access the GitHub NuGet feed.

## Coding Style & Naming Conventions

Follow `.editorconfig`: UTF-8, LF endings, a final newline, and four-space
indentation for C#. Use file-scoped namespaces, put `System` usings first, and
always use braces. Use PascalCase for public types, methods, and properties;
use `I`-prefixed interfaces (for example, `IFlexRadioService`); use camelCase
for locals and parameters. Keep controllers thin and register application
services through `Program.cs` or the existing service-collection extensions.

## Testing Guidelines

Add tests in a separate `*.Tests` project and name test files after the unit
under test (for example, `BandConverterTests.cs`). Name test methods for the
observable behavior, such as `Convert_ReturnsExpectedBand_ForValidFrequency`.
Cover new API behavior, configuration validation, and service edge cases; run
`dotnet test` before opening a pull request.

## Commit & Pull Request Guidelines

Use concise Conventional Commit-style subjects, matching history: `fix(API):
Correct slice connection logic` or `feat: Add radio discovery endpoint`.
Keep each commit focused. Pull requests should explain the behavior change,
link the relevant issue when available, list verification performed, and include
request/response examples or screenshots for API or Swagger-visible changes.

## Configuration & Security

Do not commit broker credentials, tokens, or local radio settings. Put local
overrides in `FlexRadioServices/appsettings/appsettings.user.json` (see
`FlexRadioServices/Example/`) and supply container feed credentials through
environment variables or `.env`.
