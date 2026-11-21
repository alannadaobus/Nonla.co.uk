# Đánh Cù Việt Nam — Test Plan

| | |
|---|---|
| Status | v0.1 (Phase 0) |
| Related | [SRS](SRS.md) · [PHYSICS_SPEC](PHYSICS_SPEC.md) · [TECHNICAL_DESIGN](TECHNICAL_DESIGN.md) |

## 1. Strategy

Test the deterministic, engine-free parts hardest and fastest; use Unity
only for what needs Unity.

| Level | Tool | Runs where | Scope |
|---|---|---|---|
| L1 Core unit | NUnit via `dotnet test` **and** Unity EditMode | CI on every push (no Unity licence needed) + editor | `DanhCu.Core`, later `DanhCu.Simulation` |
| L2 Data | Unity EditMode | Editor / batch CI | All ScriptableObjects valid; config ids resolve; scenes in Build Settings |
| L3 Flow | Unity PlayMode | Editor / batch CI | Boot → menu → match → pause → result → menu with real scenes |
| L4 Physics acceptance | PlayMode + headless Simulation tests | CI nightly | PHYSICS_SPEC §9 P1–P9 |
| L5 Performance | Unity Profiler, Memory Profiler, device builds | Per milestone | NFR-PERF-* on device matrix |
| L6 Network | Simulated latency/loss (Unity Transport simulator), soak | Phase 5 nightly | FR-NET-* |
| L7 Manual / playtest | Checklists, recorded sessions | Per milestone | Feel, UX, cultural review, accessibility |
| L8 Compliance | Store pre-launch reports (Google), TestFlight | Release | NFR-SEC-*, store policies |

## 2. How to run

```bash
# L1 — engine-free Core tests, no Unity needed
dotnet test Tools/CoreTests/Tests/DanhCu.Core.Tests.csproj

# L2 + L3 — Unity, batch mode (replace with your editor path)
Unity -batchmode -projectPath DanhCuVietnam -runTests -testPlatform EditMode -testResults results-editmode.xml
Unity -batchmode -projectPath DanhCuVietnam -runTests -testPlatform PlayMode -testResults results-playmode.xml

# Setup + data validation only (exits 1 on invalid data)
Unity -batchmode -quit -projectPath DanhCuVietnam -executeMethod DanhCu.Editor.ProjectSetup.RunBatch
```

In the editor: *Window → General → Test Runner*; *Danh Cu → Setup → Validate Data*.

## 3. Current automated suites (Phase 0)

| Suite | File | Covers |
|---|---|---|
| StateMachineTests | `Tests/EditMode/Core/StateMachineTests.cs` | Enter/Exit order, guards, self-transition, queued transitions during Enter/Tick, loop detection — FR-FLOW-8 |
| GameStateMachineTests | `Tests/EditMode/Core/GameStateMachineTests.cs` | Transition table: offline/online happy paths, illegal transitions, no dead ends, all states reachable |
| EventBusTests | `Tests/EditMode/Core/EventBusTests.cs` | Delivery, unsubscribe, exception isolation, mutation during publish |
| ServiceRegistryTests | `Tests/EditMode/Core/ServiceRegistryTests.cs` | Registration rules |
| MatchSettingsTests | `Tests/EditMode/Core/MatchSettingsTests.cs` | Validation per mode, defensive copies, pause rules — FR-FLOW-4/5 |
| DataAssetTests | `Tests/EditMode/Data/DataAssetTests.cs` | Project data valid, id format, inertia & critical-spin formulas (PHYSICS_SPEC §3–4), defaults playable — NFR-QUAL-4 |
| RevolvedMassIntegratorTests | `Tests/EditMode/Core/Tops/RevolvedMassIntegratorTests.cs` | Quadrature vs closed-form cylinder/cone, two materials, step at breakpoint |
| TopShapeTests | `Tests/EditMode/Core/Tops/TopShapeTests.cs` | Presets valid; profile continuity; tip point on the axis at the pivot; axisymmetry; winding vs normals; no degenerate triangles; rope submesh; hull above tip sphere and < 255 vertices; mesh volume = integrated volume; CoM ordering |
| TopModelControllerTests | `Tests/EditMode/Tops/TopModelControllerTests.cs` | Rigidbody mass/CoM/inertia from geometry; tip sphere touches pivot; mesh bounds = configured dimensions; children at identity; separate wood/metal materials; invalid shape refused; prefab wired |
| GameFlowSmokeTests | `Tests/PlayMode/GameFlowSmokeTests.cs` | Full offline flow with real scenes; pause freezes time; max angular speed raised at boot — FR-FLOW-1/4/6 |

## 4. Physics validation (Phase 1)

Each PHYSICS_SPEC §9 criterion becomes an automated test against the
headless Simulation with fixed seeds, plus a visual "physics lab" scene with
live graphs (ω₃, θ, Ω_p, energy) for tuning. Reference data: high-speed
video (240 fps) of real tops thrown on earth and brick, digitised for spin
decay and precession rate.

## 5. Device matrix (Phase 6)

| Tier | Android | iOS | Desktop |
|---|---|---|---|
| Low | 2020 entry (Helio G85, 4 GB) — 30 FPS floor | iPhone XR | Intel UHD 620 laptop |
| Reference | 2022 mid (SD 7 Gen 1, 6 GB) | iPhone 12 | GTX 1660 / Apple M1 |
| High | 2024 flagship | iPhone 15 Pro | RTX 3070 / M3 |

Checks: FPS and frame-time p95, thermals over 30 min, memory peak, load times, input latency, battery drain/hour.

## 6. Traceability

Every SRS requirement id maps to at least one test (automated or manual
checklist) before its phase exits. The mapping table lives here and grows per phase:

| Requirement | Test |
|---|---|
| FR-FLOW-1, FR-FLOW-6 | GameFlowSmokeTests.Boot_ReachesMainMenu_ThenOfflineMatchFlowWorks |
| FR-FLOW-4 | GameFlowSmokeTests (timeScale = 0 when paused) |
| FR-FLOW-5 | MatchSettingsTests.ModeRules_OnlineCannotFreezeOnPause |
| FR-FLOW-8 | StateMachineTests.Transition_RejectedByGuard_StaysAndRaisesRejected; GameStateMachineTests.IllegalTransitions_AreRejected |
| FR-CONT-3, NFR-QUAL-4 | DataAssetTests.AllProjectDataAssets_AreValid |
| FR-SPIN-1 (partial) | DataAssetTests.TopDefinition_CriticalSpeed_MatchesStabilityCriterion; GameFlowSmokeTests.Physics_MaxAngularSpeed_IsRaisedAtBoot |
| NFR-QUAL-1 | CI compile (Core); Unity batch compile (when Unity CI is configured) |
| NFR-QUAL-3 | `dotnet test Tools/CoreTests/Tests` |
| FR-CONT-3 (top shapes) | TopShapeTests, TopModelControllerTests, DataAssetTests.TopDefinition_* |

## 7. Release gates

* 0 compile errors/warnings-as-errors in Core; 0 failing tests in all suites.
* 0 open P0/P1 bugs; P2 triaged.
* NFR-PERF targets met on the Reference tier on every platform.
* Crash-free ≥ 99.5 % in closed beta (≥ 500 sessions).
* Vietnamese text reviewed by a native speaker; diacritics render on every platform.
* Store pre-launch reports clean.

## 8. Bug severity

| Severity | Definition |
|---|---|
| P0 | Crash, data loss, cannot start or finish a match, online exploit |
| P1 | Major feature broken; physics clearly wrong (fails a §9 criterion) |
| P2 | Minor feature issue, visual glitch with workaround |
| P3 | Cosmetic, polish |
