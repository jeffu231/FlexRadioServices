# Redesign CAT configuration around profiles and Configuration API v2

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds. Maintain this document in accordance with `.agents/plans.md` from the repository root.

## Purpose / Big Picture

After this change, an operator can define reusable CAT listener profiles and assign one enabled radio client to each profile at service startup. Profiles can be retained while inactive, so operators can prepare configurations without opening sockets; an enabled client opens every TCP listener in its selected profile even when that client is not currently connected to a Flex radio. The read-only Configuration API v2 will show both the saved configuration and the listeners that are actually active.

The old `CatPorts:PortSettings` configuration shape and every v1 `ConfigurationController` route will deliberately stop working. A legacy configuration key must make startup fail with a clear validation error instead of silently starting with CAT disabled. The independently versioned `RadioController` v1 routes remain unchanged.

## Progress

- [ ] Record the profile/client configuration contract, add strict binding, and implement startup validation.
- [ ] Resolve validated settings once into immutable active CAT bindings and replace direct listener registration with a supervised CAT coordinator.
- [ ] Update CAT client matching and runtime diagnostics for profile-based bindings.
- [ ] Replace Configuration API v1 with v2 read models and document the new configuration.
- [ ] Add unit, runtime, API, and OpenAPI coverage; run the complete release test suite and manually verify the HTTP and TCP behavior.

## Surprises & Discoveries

- Observation: the existing `CatPortSettings` has only `PortSettings`, and every `PortSettings` contains `ClientId`; `Program.cs` creates one `FlexCatPortService` singleton per item before options validation is used as the source of runtime topology.
  Evidence: `FlexRadioServices/Models/Settings/CatPortSettings.cs` declares `List<PortSettings> PortSettings`, while `FlexRadioServices/Program.cs` reads that list directly and loops over it to register `IHostedService` instances.

- Observation: runtime options already use `IValidateOptions<T>` plus `ValidateOnStart`, but binding currently accepts unrecognized keys.
  Evidence: `FlexRadioServices/Utils/ServiceCollectionExtensions.cs` calls `.Bind(...)`, `.AddSingleton<IValidateOptions<CatPortSettings>, CatPortSettingsValidator>()`, and `.ValidateOnStart()` without binder options.

- Observation: `FlexCatPortService` starts its TCP listener independently of whether the associated Flex GUI client is connected, which is the desired availability behavior for an enabled configuration.
  Evidence: `FlexRadioServices/Services/FlexCatPortService.cs` starts `ITcpServer.RunAsync` before it reacts to radio connection events; missing client lookup merely produces no selected slice.

- Observation: existing API-host tests remove all `IHostedService` registrations, so endpoint/OpenAPI tests do not start FlexLib, MQTT, or CAT TCP listeners.
  Evidence: `FlexRadioServices.Tests/OpenApi/OpenApiDocumentTests.cs` defines `OpenApiWebApplicationFactory` and calls `services.RemoveAll<IHostedService>()`.

## Decision Log

- Decision: treat `Clients[].Enabled` as a startup-only switch in this phase; neither radio presence nor the Configuration API changes listener state.
  Rationale: listener topology must be deterministic for a process lifetime. A later writable API may update saved configuration, but it will still require a restart before CAT listeners change.
  Date/Author: 2026-08-27 / Codex

- Decision: make profiles own `PortSettings`, remove `ClientId` from `PortSettings`, and bind exactly one profile to each client record.
  Rationale: ports are reusable per profile and client identity is an activation concern. This removes repeated client IDs from every listener definition.
  Date/Author: 2026-08-27 / Codex

- Decision: resolve names with `StringComparer.OrdinalIgnoreCase`, preserve configured display casing in returned records, and reject ambiguous duplicates.
  Rationale: client IDs and profile names should not change behavior merely due to letter casing, while operators should see the spelling they supplied.
  Date/Author: 2026-08-27 / Codex

- Decision: enforce TCP port uniqueness over all configured profiles, including profiles with no client or only disabled clients.
  Rationale: a port number is the global identity of a CAT endpoint and reserving it prevents a latent conflict when a profile is enabled later.
  Date/Author: 2026-08-27 / Codex

- Decision: the Configuration controller is v2-only, but no v1 mapping is removed from `RadioController`.
  Rationale: this is a breaking redesign of configuration only; it does not authorize removal of unrelated radio operations.
  Date/Author: 2026-08-27 / Codex

## Outcomes & Retrospective

No implementation has started. On completion, record the exact test counts, an example validated profile/client configuration, the observed v2 response, and any coordinator startup or shutdown behavior discovered during implementation.

## Context and Orientation

This is a .NET 10 ASP.NET Core Web API. `FlexRadioServices/Program.cs` composes dependency injection and registers hosted services, which are background services started and stopped with the web application. `FlexRadioServices/Utils/ServiceCollectionExtensions.cs` binds settings from `appsettings` JSON and validates them at startup. `FlexRadioServices/Models/Settings/` contains configuration binding types and validators.

CAT is the TCP command protocol used by radio-control clients. Today `CatPortSettings` binds `CatPorts:PortSettings[]`, where each listener combines port behavior and a `ClientId`. `FlexCatPortService` is an internal hosted CAT listener. It uses an `ITcpServer` to accept TCP connections and an `IConnectedRadioCoordinator` to locate Flex radio slices for the configured client. It must keep listening while an enabled client has no radio presence; only its slice selection can be unavailable.

The new root shape is exactly this, with ordinary JSON property names accepted case-insensitively by ASP.NET configuration binding:

    CatPorts:
      Profiles[]:
        ProfileName
        PortSettings[]:
          PortFriendlyName
          Protocol
          PortNumber
          PortSliceType
          VfoASliceLetter
          VfoBSliceLetter
          AutoSwitchTxSlice
      Clients[]:
        ClientId
        ClientFriendlyName
        Enabled
        ProfileName

`ProfileName` is a required nonblank profile identity. A `ClientId` is the Flex GUI client identifier and `ClientFriendlyName` is the required operator-facing label. A profile is active only when exactly one client that references it has `Enabled: true`. A profile may have no client or only disabled clients and is then inactive. An empty `CatPorts` root, including no `Profiles` and no `Clients`, is valid and means CAT is disabled.

`ResolvedCatPortBinding` is an immutable runtime value containing `(ProfileName, ClientId, ClientFriendlyName, PortSettings)`. It represents one listener that should be running. The configuration provider creates those bindings once after validation; the hosted coordinator owns their lifetimes. The provider must not infer activity from the current Flex radio connection.

The current `ConfigurationController` is annotated only for API v1 and returns mutable options directly for CAT and radio settings. `RadioController` deliberately has both v1 and v2 operations; do not change its v1 mappings. API response records belong in `FlexRadioServices/Models/Configuration/`, one public type per file, so Swagger has stable, explicit schemas rather than exposing mutable configuration types.

Tests are in `FlexRadioServices.Tests/`. `RuntimeConfigurationOptionsTests` currently validates only the old CAT shape. `ConfigurationControllerTests` constructs the controller directly. `OpenApiDocumentTests` obtains generated Swagger documents from a `WebApplicationFactory`. `TcpServerTests` demonstrates using loopback TCP and a reserved `TcpListener` to observe start and stop behavior without radio hardware.

## Plan of Work

First replace the CAT binding model. In `FlexRadioServices/Models/Settings/CatPortSettings.cs`, replace `PortSettings` with non-null `Profiles` and `Clients` lists. Add `CatPortProfileSettings.cs` with a required `ProfileName` and non-null `List<PortSettings> PortSettings`; add `CatClientSettings.cs` with `ClientId`, `ClientFriendlyName`, `Enabled`, and `ProfileName`. Remove `ClientId` from `PortSettings.cs` and update its XML documentation to describe profile-owned listener behavior. Keep the existing port behavior fields and defaults so a profile’s listener behavior remains compatible apart from the intentional configuration redesign.

Rewrite `CatPortSettingsValidator.Validate` to accumulate every failure in one `ValidateOptionsResult.Fail` result. Use paths such as `CatPorts:Profiles:0:ProfileName`, `CatPorts:Clients:1:ClientId`, and `CatPorts:Profiles:0:PortSettings:2:PortNumber` in every message, so an operator can correct JSON without guessing. Validate nonblank profile name, client ID, client friendly name, and client profile name. Reject duplicate normalized profile names and client IDs with ordinal, case-insensitive comparison. Require each profile to contain at least one port, but permit zero profiles and zero clients at the root. For every client, look up its profile case-insensitively and fail a missing reference. Count enabled clients by normalized profile name and reject a count greater than one.

For every `PortSettings` in every profile, retain the existing validation rules: nonblank `PortFriendlyName`; protocol equal to `TCP` without case sensitivity; `PortNumber` from 1 through 65535; optional slice letters only from A through H; and a nonblank VFO A letter for `PortSliceType.Designated`. Add global duplicate-port detection across every profile, including inactive and unreferenced profiles, and report every colliding path or an unambiguous aggregate message naming the port and participating paths. Do not validate a client’s radio presence because that changes at runtime and is not configuration validity.

In `ServiceCollectionExtensions.AddRuntimeConfiguration`, call the options binder overload with `BinderOptions.ErrorOnUnknownConfiguration = true` for `CatPortSettings`. This makes the removed `CatPorts:PortSettings` key, a stray `ClientId` under profile `PortSettings`, and other misspelled CAT keys fail at startup. Keep `ValidateDataAnnotations`, `CatPortSettingsValidator`, and `ValidateOnStart`; validation must occur before the application begins serving requests. Apply strict unknown-key binding to CAT only unless a separate requirement explicitly broadens it to other configuration sections.

Add `FlexRadioServices/Models/Settings/ResolvedCatPortBinding.cs` as an immutable record with properties/primary constructor parameters `ProfileName`, `ClientId`, `ClientFriendlyName`, and `PortSettings`. Add a singleton `ICatPortConfigurationProvider` and implementation, preferably in `FlexRadioServices/Services/`, whose constructor receives `IOptions<CatPortSettings>`. It must read the startup-validated value once, preserve profile, client, and port order from configuration, build a case-insensitive profile lookup, and produce a cached immutable sequence of all active bindings. Its public surface must also provide the validated configured profiles and clients, or an immutable configuration snapshot, so the API can build configured/effective views without returning `CatPortSettings` or any mutable `List<T>`. Choose explicit methods such as `GetConfiguredProfiles()`, `GetConfiguredClients()`, and `GetActiveBindings()` returning immutable collections; document that callers must not observe later configuration reloads. The resolution work must be linear, O(profiles + clients + total ports): create lookups/counts in one pass and enumerate each active profile’s port list once, rather than scanning all clients for every port.

Add `ICatPortServiceFactory` and its implementation in `FlexRadioServices/Services/`. Define a `Create(ResolvedCatPortBinding binding)` method returning `ICatPortService`. The implementation obtains a fresh `ITcpServer`, `ILogger<FlexCatPortService>`, and `IConnectedRadioCoordinator` from its dependencies and constructs a `FlexCatPortService` for the supplied binding. Register the factory and provider as singletons; keep `ITcpServer` transient so every binding gets its own listener state. Do not register a CAT listener directly as `IHostedService`.

Add `CatPortHostedService` in `FlexRadioServices/Services/` as the sole registered CAT `IHostedService`. On `StartAsync`, get active bindings from the provider, create the corresponding child services, and start them with the application cancellation token. It must retain started children, observe their completion tasks, and propagate an unexpected child fault so host supervision sees a failed CAT service. A normal stop requested by the host is not a fault. If creating or starting any child fails, stop every child already started, await their cleanup, clear retained state, and rethrow the original startup failure; this is partial-start rollback. If there are no active bindings, start successfully without opening any listener. On `StopAsync`, stop all retained children, await their completion with the provided cancellation token, and release references. Make start/stop safe for the normal hosted-service lifecycle, including an attempted shutdown after partial-start failure, and log profile/client/port context for startup, rollback, fault, and stop.

Refactor `FlexCatPortService` to accept `ResolvedCatPortBinding` rather than `PortSettings`. Store the binding and use `binding.PortSettings` where current code reads `_portSettings`; use the resolved friendly client name in lifecycle logs where helpful. In `IsCurrentClientSlice` compare FlexLib `client.ClientID` and `binding.ClientId` with `StringComparison.OrdinalIgnoreCase`. In `GetClientHandle`, either use FlexLib’s case-insensitive lookup when its contract guarantees that behavior or enumerate/find GUI clients with `OrdinalIgnoreCase`; the lookup must have the same casing behavior as slice ownership. Do not add logic that stops TCP when the Flex client cannot be found. While touching this file, preserve existing command and socket behavior; this plan does not redesign CAT parsing.

In `Program.cs`, remove the configuration read and loop that directly registers `FlexCatPortService` instances. After the TCP dependencies and runtime-configuration registration, register the provider, factory, and `CatPortHostedService` through the service collection extension or directly in the same composition area. The final process must have exactly one CAT coordinator hosted service, which may own zero or more listeners. Update `RuntimeConfigurationDiagnosticsService` to depend on the provider/snapshot rather than `IOptions<CatPortSettings>` and log configured profile/client counts plus active listener ports or `disabled`; it must not leak credentials and must clearly say the result remains fixed until restart.

Replace `ConfigurationController`’s `[ApiVersion("1.0")]` declaration with `[ApiVersion("2.0")]` and remove all v1 action mappings. Keep only the requested CAT action unless product requirements add v2 replacements for version, MQTT, or radio configuration: `GET /api/frs/v2/configuration/catport/settings`. Its response must be a dedicated top-level record, for example `CatPortSettingsResponse`, with `Configured` and `EffectiveProfiles` properties. Add one record per type in `Models/Configuration/`: a configured container with `Profiles` and `Clients`; configured profile, client, and port records that copy safe scalar values; an effective profile record `(ProfileName, IsActive, ActiveClient, Listeners)`; an effective active-client record `(ClientId, ClientFriendlyName)`; and an effective listener record that exposes the profile `PortSettings` behavior needed by consumers. Set `ActiveClient` to null and `Listeners` to an empty immutable/read-only collection for inactive profiles. Map all profiles in their configured order, set `IsActive` by matching active bindings, and construct each active listener from the binding’s `PortSettings`. Do not return `CatPortSettings`, `CatPortProfileSettings`, `CatClientSettings`, `PortSettings`, or mutable options directly. Use a synchronous `ActionResult<CatPortSettingsResponse>` unless there is genuine asynchronous I/O.

Update `FlexRadioServices/Example/appsettings.user.json` to the new shape. Convert repeated groups of current ports into named profiles and create a matching client record for each currently intended active client, setting one client per profile to `Enabled: true`. Ensure every old per-port `ClientId` is removed. Update the README Configuration section with a compact complete example, the profile/client ownership rules, case-insensitive uniqueness and matching rules, global port reservation, startup-only enable semantics, the required restart after changes, strict failure for legacy `PortSettings`, and the v2 GET endpoint. State explicitly that an enabled client’s temporary absence from a radio does not close listeners.

## Concrete Steps

Work from `/Users/jeffu/Dev/FlexRadioServices`.

1. Establish the red tests first. Extend `FlexRadioServices.Tests/Models/Settings/RuntimeConfigurationOptionsTests.cs`, or split CAT-specific cases into `CatPortSettingsValidatorTests.cs`, to build in-memory new-shape settings through `AddRuntimeConfiguration`. Test a valid shared profile with one enabled client; empty root CAT configuration; all clients disabled; an unused profile; and multiple active profiles. Assert the new options bind correctly and, once the provider is introduced, assert its active bindings preserve configured ordering and share the expected port settings.

2. Add invalid-setting tests that demand path-qualified `OptionsValidationException` messages for blank required values, duplicate profile names differing only by case, duplicate client IDs differing only by case, missing client profile references, two enabled clients selecting one profile, empty profile port lists, invalid TCP protocol, invalid designated VFO A, and duplicate `PortNumber` values within one profile and across inactive/active profiles. Include `CatPorts:PortSettings:0:...` in a strict-binder test and assert resolution/startup fails instead of yielding zero CAT bindings. Do not retain old-shape tests as compatibility assertions.

3. Implement the new model, validator, strict CAT binding, configuration provider, factory, and coordinator. Add focused provider tests that prove a disabled or unused profile produces no binding, profile lookup is case-insensitive, an enabled profile retains its profile/client/port ordering, and resolution never needs a connected radio. Test the coordinator with a fake `ICatPortServiceFactory` and fake `ICatPortService`: no active binding causes no factory call; active bindings are started; an already-started child is stopped when a later child start throws; an unexpectedly faulted child faults the coordinator; and `StopAsync` stops every child exactly once. Use task-completion sources and cancellation-aware fakes instead of timing-based sleeps.

4. Refactor `FlexCatPortService` and add a direct unit test with a fake connected radio/client or extracted internal comparison helper that proves client IDs match regardless of case. Preserve a test that a missing client returns no selected handle/slice without ending the service; add a loopback coordinator integration test where an active profile opens a configured TCP port and an inactive profile does not. Reserve the inactive port in a separate `TcpListener`, start the coordinator with one active profile, and prove the active port accepts a connection while no attempt is made to bind the inactive port. Use ports allocated by loopback fixtures, not fixed machine-wide port numbers.

5. Replace the controller mappings and create response records. Update `ConfigurationControllerTests` to construct the provider/snapshot and assert the v2 response contains configured profiles/clients plus effective profile state: one active client and listeners for an active profile, `activeClient: null` and an empty listener list for inactive/unused profiles. Serialize the response with web JSON options and assert its property names and safe values. Remove assertions or construction paths that require v1 CAT settings options.

6. Update `OpenApiDocumentTests`: assert `/api/frs/v1/configuration/version` and `/api/frs/v1/configuration/catport/settings` are absent, `/api/frs/v2/configuration/catport/settings` is present with GET and a 200 response, and its response schema references/contains the configured and effective record fields. Continue asserting the v1 radio path exists, proving the configuration-only v1 removal did not remove `RadioController` v1. Because API host tests remove hosted services, add a separate startup test that retains CAT validation/coordinator registration but substitutes safe hosted/factory dependencies when proving invalid configuration fails before listeners run.

7. Update the example and README, then run formatting, tests, and a local process verification:

       dotnet build FlexRadioServices.sln -c Release
       dotnet test FlexRadioServices.sln -c Release
       dotnet format FlexRadioServices.sln --verify-no-changes --no-restore --severity info
       dotnet run --project FlexRadioServices/FlexRadioServices.csproj --no-launch-profile

   With a local `FlexRadioServices/appsettings/appsettings.user.json` using one enabled profile and one disabled profile, request:

       curl -i http://localhost:5000/api/frs/v2/configuration/catport/settings

   The response must be HTTP 200. It must list both profiles under `configured`, mark only the enabled profile `isActive: true`, show that profile’s enabled client and listeners, and show `activeClient: null` with `listeners: []` for the disabled profile. Use the actual Kestrel URL logged by `dotnet run` if it differs from port 5000. Stop the process with Ctrl+C and verify its CAT listener logs show clean shutdown.

8. Repeat startup with a local configuration that still contains the old `CatPorts:PortSettings` key. Expect host startup to fail before it listens, with an options/binding error that names `CatPorts:PortSettings`. Restore the valid new-shape user configuration afterward. Do not commit a real radio client ID, broker password, token, or user-specific `appsettings.user.json`.

## Validation and Acceptance

Acceptance is behavioral, not just compilation:

- Starting the service with no `CatPorts` profiles succeeds and opens no CAT socket.
- Starting with an enabled client and a profile of two TCP ports opens exactly those two ports, even if the configured Flex GUI client is absent from every connected radio. Starting with all clients disabled or with an unreferenced profile opens zero listeners for those profiles.
- A second enabled client selecting the same profile, an unknown profile reference, case-only duplicate client/profile identity, a blank required identity, an empty profile, an invalid port/slice/protocol, or a duplicate port anywhere in configured profiles makes startup fail with all applicable path-qualified errors.
- An old `CatPorts:PortSettings` key makes startup fail because unknown CAT configuration keys are rejected; it cannot silently disable CAT.
- An active profile’s slice matching accepts the same client ID in different letter casing. A profile’s configured port stays alive through that client’s temporary radio absence.
- If one child listener fails during coordinator startup, every already-started listener is stopped. If a running child faults unexpectedly, the coordinator reports/fails rather than leaving a silently degraded configuration. Normal host shutdown stops all CAT children cleanly.
- `GET /api/frs/v2/configuration/catport/settings` returns copied, dedicated response records. It displays configured profiles and clients and each profile’s effective active state. Inactive profiles have a null active client and an empty listener array.
- Swagger v2 documents the CAT configuration response; Swagger v1 has no Configuration controller paths, while v1 Radio controller paths still exist.
- `dotnet build`, `dotnet test`, and `dotnet format --verify-no-changes` from the repository root complete successfully. The exact number of passing tests is recorded in Outcomes & Retrospective at implementation completion.

## Idempotence and Recovery

All configuration validation and provider resolution are read-only and repeatable. Do not attempt an in-process listener reconfiguration: changing an `Enabled` flag, client, profile, or port requires editing the user configuration and restarting the process. The provider intentionally captures one validated startup snapshot, so a configuration reload cannot leave API output and active sockets inconsistent.

Implement the coordinator additively and retain existing `TcpServer` cancellation behavior. If a child listener cannot bind because its port is occupied, the coordinator must stop only children it started during that attempt and rethrow the bind failure; fix the configuration or release the conflicting external process, then restart. Never kill an unknown process merely to free a configured port. For test cleanup, cancel owned tokens, await child stop tasks, dispose loopback listeners, and use `try/finally` so a failing test cannot reserve a port for later tests.

This is intentionally a breaking configuration migration. Do not write a converter, fallback binder, or legacy route redirect. Recover from a deployment failure by restoring a previously valid new-shape configuration or rolling back the application release together with its configuration; do not reintroduce `PortSettings` as a root compatibility alias.

## Artifacts and Notes

The example v2 API shape should be observably similar to this; exact port fields mirror the profile-owned `PortSettings` record:

    {
      "configured": {
        "profiles": [{ "profileName": "Operator" }],
        "clients": [{ "clientId": "gui-a", "clientFriendlyName": "Operator GUI", "enabled": true, "profileName": "operator" }]
      },
      "effectiveProfiles": [{
        "profileName": "Operator",
        "isActive": true,
        "activeClient": { "clientId": "gui-a", "clientFriendlyName": "Operator GUI" },
        "listeners": [{ "portFriendlyName": "CAT A", "protocol": "TCP", "portNumber": 6005 }]
      }]
    }

The expected legacy-key startup failure need not have framework-dependent wording, but it must contain the rejected configuration path. A concise acceptable observation is:

    OptionsValidationException: CatPorts:PortSettings is not a recognized CAT configuration property.

## Interfaces and Dependencies

Use no new NuGet package. Use .NET options binding, `Microsoft.Extensions.Options`, hosted-service lifecycle APIs, existing `ITcpServer`, and existing FlexLib abstractions.

In `FlexRadioServices/Models/Settings/ResolvedCatPortBinding.cs`, define an immutable public record equivalent to:

    public sealed record ResolvedCatPortBinding(
        string ProfileName,
        string ClientId,
        string ClientFriendlyName,
        PortSettings PortSettings);

In `FlexRadioServices/Services/ICatPortConfigurationProvider.cs`, define a public or internal interface consistent with its consumers. It must expose immutable configured profile/client data and active `ResolvedCatPortBinding` values, captured at startup. Prefer names that make snapshots explicit, such as:

    ImmutableArray<CatPortProfileSettings> GetConfiguredProfiles();
    ImmutableArray<CatClientSettings> GetConfiguredClients();
    ImmutableArray<ResolvedCatPortBinding> GetActiveBindings();

If the settings records retain mutable `List<T>` properties for binder compatibility, the provider must deep-copy into immutable API/runtime snapshot records before exposing them, including copies of nested port settings. The controller must consume that snapshot, not `IOptions<CatPortSettings>`.

In `FlexRadioServices/Services/ICatPortServiceFactory.cs`, define:

    ICatPortService Create(ResolvedCatPortBinding binding);

`CatPortHostedService` implements `IHostedService` and depends on `ICatPortConfigurationProvider`, `ICatPortServiceFactory`, and `ILogger<CatPortHostedService>`. It is the only CAT service registered as an application hosted service. Keep `ICatPortService` as the child abstraction; its existing `StartAsync` and `StopAsync` methods are sufficient, but tests may use a small fake implementation that exposes a completion/fault task. If fault observation cannot be expressed with the existing interface, extend it deliberately with a documented completion task rather than relying on implementation casts; update every implementation and test fake together.

`FlexCatPortService` accepts `ResolvedCatPortBinding`, `ITcpServer`, `ILogger<FlexCatPortService>`, and `IConnectedRadioCoordinator`. It remains internal and continues to implement `ICatPortService` and `ICatCommandSink`. Its client-ID comparisons must use `StringComparison.OrdinalIgnoreCase`.

Create one public response record per file in `FlexRadioServices/Models/Configuration/`. At a minimum the v2 endpoint needs a top-level CAT configuration response, configured container/profile/client/port records, effective profile/active-client/listener records. Use non-null immutable/read-only collections and make inactive `ActiveClient` nullable. Apply XML comments to exported C# types and public properties so generated Swagger remains understandable.

Plan created 2026-08-27: translates the approved breaking CAT profile/client redesign into a startup-validated, supervisor-owned runtime topology and Configuration API v2. No implementation was performed.
