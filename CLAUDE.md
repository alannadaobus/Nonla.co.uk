# CLAUDE.md — Đánh Cù Việt Nam

Rules for anyone (human or AI) changing this repository. Read the design docs
in `Docs/` before non-trivial work; update them in the same change when
behaviour, architecture or data fields change.

## Repository

| Path | What |
|---|---|
| `DanhCuVietnam/` | Unity 6.3 LTS project root (open this folder in Unity Hub) |
| `DanhCuVietnam/Assets/_Project/` | **All** first-party game content and code |
| `Docs/` | SRS, GDD, TECHNICAL_DESIGN, PHYSICS_SPEC, TEST_PLAN |
| `Tools/CoreTests/` | .NET projects that compile and test `DanhCu.Core` without Unity |
| `README.md` | Repository is shared with the Nonla website; leave website content alone |

## Commands

```bash
# Engine-free tests (fast, no Unity) — run before every commit touching Core
dotnet test Tools/CoreTests/Tests/DanhCu.Core.Tests.csproj

# Unity tests (batch)
Unity -batchmode -projectPath DanhCuVietnam -runTests -testPlatform EditMode
Unity -batchmode -projectPath DanhCuVietnam -runTests -testPlatform PlayMode

# Idempotent setup + data validation (exit code 1 on invalid data)
Unity -batchmode -quit -projectPath DanhCuVietnam -executeMethod DanhCu.Editor.ProjectSetup.RunBatch
```

Editor menus: **Danh Cu → Setup → Run Project Setup** / **Validate Data**.

## Architecture rules (see Docs/TECHNICAL_DESIGN.md)

1. **Every script belongs to an assembly definition.** Never add scripts to
   `Assembly-CSharp`. New module = new folder `Scripts/<Module>/` +
   `DanhCu.<Module>.asmdef` + a row in TECHNICAL_DESIGN §2.
2. **Dependencies point down only:** Core ← Data/Input/Simulation ← Gameplay/AI/Networking ← Runtime ← UI. No cycles, no upward references.
3. **`DanhCu.Core` (and future `DanhCu.Simulation`) must never reference
   UnityEngine** (`noEngineReferences: true`). Use `System.Numerics` for math
   there, `DanhCu.Core.Diagnostics.Log` for logging.
4. **`GameManager` is the only singleton.** Everything else is owned and
   passed explicitly (constructor parameters, `ServiceRegistry` at composition boundaries).
5. **Top-level flow changes only through `GameStateMachine`.** Legal transitions
   are defined solely in `GameStateTransitions`; keep the diagram in
   TECHNICAL_DESIGN §5 in sync.
6. **Events (`EventBus`) are for low-frequency notifications**, never per-frame data.
7. **ScriptableObjects are read-only at runtime.** Runtime state lives in
   plain C# objects (`GameSession`, simulation state). Never write to an SO in play mode.
8. **Content is referenced by stable `snake_case` ids** across saves, network
   and settings. Never rename an id after release.
9. Every definition implements `IValidatable`; add validation for every new field with constraints.
10. **Physics is SI units** and every tunable maps to a symbol in
    PHYSICS_SPEC.md. Change the spec and the field together.
11. **Same throw model for player, AI and network.** AI may not read or set
    simulation state it could not observe.
12. Web-safe code: no threads in gameplay, no synchronous loads, no new
    `Resources` usage (only `GameConfig`), no file IO outside a storage service.

## C# conventions

* **C# 9 only** (Unity's compiler): no file-scoped namespaces, global usings,
  `record struct`, `required`, raw strings, or `init` accessors (no `IsExternalInit` in Unity).
* Namespaces mirror assemblies: `DanhCu.<Module>[.<Sub>]`.
* `[SerializeField] private` fields + read-only public properties; `camelCase`
  serialized fields, `_camelCase` private fields, `PascalCase` members.
* `sealed` by default; `internal` unless used by another assembly.
* No `FindObjectOfType` / `GameObject.Find` / `SendMessage` in gameplay code.
* No allocations in per-frame/per-physics-step paths (no LINQ, closures, boxing there).
* XML doc comments on public types explaining *why*, not *what*.
* Keep `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` statics resets for
  anything static so "Enter Play Mode Options" without domain reload stays safe.

## Unity asset rules

* **Always commit `.meta` files** with their assets; never commit `Library/`,
  `Temp/`, `Logs/`, `UserSettings/`, `obj/`, generated `.csproj`/`.sln`.
* Asset serialization is Force Text. Do not hand-edit `.unity`/`.prefab`
  YAML except for trivial fixes; prefer editor scripts.
* Binary art (fbx, png, psd, wav, …) goes through **Git LFS** (`git lfs install` once).
* Configure UnityYAMLMerge locally (see `.gitattributes` header).
* Do not overwrite existing assets from tools; `ProjectSetup` only creates what is missing — keep it that way.
* Scenes in Build Settings: Bootstrap must stay at index 0.

## First open of the project (one-time)

The foundation was authored without a Unity editor, so on first open Unity
generates `ProjectSettings/*`, `Packages/packages-lock.json`, all `.meta`
files, and the setup tool creates URP assets, default data and scenes. After
it finishes (and the editor restarts for Active Input Handling):

1. Check the Console is free of errors; run EditMode + PlayMode tests.
2. Commit everything generated (metas, ProjectSettings, scenes, Data assets,
   URP assets, packages-lock). Review the diff: no `Library/`.

If Package Manager cannot resolve a pinned version for your 6000.3 patch,
accept the version it proposes and update `Packages/manifest.json` and
TECHNICAL_DESIGN §2.

## Workflow

* Small, focused commits; message in imperative mood (`Add ring-out ruleset`).
* Tests accompany behaviour changes. Core changes must pass `dotnet test` and EditMode.
* Do not implement features from a later phase (TECHNICAL_DESIGN §10) without agreement.
* Vietnamese text: keep diacritics correct (UTF-8); Vietnamese is primary, English fallback.
