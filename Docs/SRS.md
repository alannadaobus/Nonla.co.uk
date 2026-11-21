# Đánh Cù Việt Nam — Software Requirements Specification

| | |
|---|---|
| Status | Draft v0.1 (Phase 0) |
| Scope | Game client for Windows, macOS, Android, iOS; Web later |
| Related | [GDD](GDD.md) · [TECHNICAL_DESIGN](TECHNICAL_DESIGN.md) · [PHYSICS_SPEC](PHYSICS_SPEC.md) · [TEST_PLAN](TEST_PLAN.md) |

Requirement keywords: **must** (release-blocking), **should** (expected,
may slip with sign-off), **may** (optional). Each requirement has an id that
tests reference (TEST_PLAN §6).

## 1. Purpose and scope

*Đánh Cù Việt Nam* is a commercial, realistic 3D game of the Vietnamese
traditional spinning top (*con cù*, the game *đánh cù* / *đánh quay*). Players
wind a rope around a wooden top, throw it, and compete on spin duration,
striking (*bổ cù*) and ring-out. The game must feel physically authentic and
celebrate the cultural context (village courtyards, Tết).

Out of scope for v1.0: user-generated content, esports/tournament tooling,
VR, console platforms.

## 2. Stakeholders

| Role | Interest |
|---|---|
| Players (casual mobile, nostalgic adults, families) | Easy to learn, authentic, fair |
| Product owner (Nonla) | Commercial quality, cultural pride, store success |
| Developers / artists | Maintainable, data-driven, testable |
| Platform holders (Apple, Google, Microsoft, Valve) | Policy compliance |

## 3. Functional requirements

### 3.1 Game flow

| Id | Requirement |
|---|---|
| FR-FLOW-1 | The game **must** start in a main menu after a boot sequence of ≤ 5 s on reference devices (NFR-PERF-4) |
| FR-FLOW-2 | The player **must** be able to choose a mode: Practice (single-player), Versus AI, Online |
| FR-FLOW-3 | Before a match the player **must** be able to choose their top, and (where applicable) arena, opponent difficulty and ruleset |
| FR-FLOW-4 | The player **must** be able to pause an offline match; time **must** freeze while paused offline |
| FR-FLOW-5 | Pausing an online match **must not** freeze the simulation for any player |
| FR-FLOW-6 | After a match the player **must** see a result and be able to rematch or return to the menu |
| FR-FLOW-7 | Every scene transition **must** show loading progress and **must not** block the main thread for > 100 ms |
| FR-FLOW-8 | Illegal state transitions **must** be rejected and logged, never crash |

### 3.2 Throwing and spinning

| Id | Requirement |
|---|---|
| FR-THROW-1 | The player **must** wind, aim and release a throw with mouse, touch and gamepad |
| FR-THROW-2 | Throw quality (wind-up, power, release timing, aim) **must** determine initial spin, velocity and tilt per PHYSICS_SPEC §7 |
| FR-THROW-3 | The game **should** offer throw assistance levels for accessibility |
| FR-SPIN-1 | Tops **must** exhibit precession, sleeping, wandering, wobble and fall per PHYSICS_SPEC §9 acceptance criteria |
| FR-SPIN-2 | Spin behaviour **must** depend on top properties (mass, size, wood, tip) and arena surface |

### 3.3 Rules

| Id | Requirement |
|---|---|
| FR-RULE-1 | **LongestSpin** (*quay lâu*): the last top spinning wins the round |
| FR-RULE-2 | **Strike** (*bổ cù*): players alternately throw at the opponent's spinning top; knocking it down or out-spinning it wins |
| FR-RULE-3 | **RingOut** (*đẩy vòng*): knocking the opponent's top outside the ring wins |
| FR-RULE-4 | Matches **must** support best-of-N rounds (N = 1..5) |
| FR-RULE-5 | Rules **must** be evaluated identically on every peer and the server |

### 3.4 AI

| Id | Requirement |
|---|---|
| FR-AI-1 | Versus AI **must** offer at least three difficulties (Dễ / Vừa / Khó) |
| FR-AI-2 | The AI **must** use the same throw model as the player (no physics cheating) |
| FR-AI-3 | AI throws **should** look human (wind-up time, aiming pause) |

### 3.5 Online multiplayer

| Id | Requirement |
|---|---|
| FR-NET-1 | Players **must** be able to play 1v1 online via quick match and private room code |
| FR-NET-2 | The match **must** remain playable at 150 ms RTT and 2 % packet loss |
| FR-NET-3 | A disconnected player **should** be able to rejoin within 30 s |
| FR-NET-4 | The server **must** be authoritative for spin state and rule outcomes |
| FR-NET-5 | Cross-play **should** work between all shipped platforms |

### 3.6 Content and progression

| Id | Requirement |
|---|---|
| FR-CONT-1 | v1.0 **should** ship ≥ 8 tops across ≥ 4 woods and ≥ 3 tip types |
| FR-CONT-2 | v1.0 **should** ship ≥ 4 arenas with distinct surfaces |
| FR-CONT-3 | Content **must** be data-driven (ScriptableObjects) and addable without code changes |
| FR-CONT-4 | Progress (unlocked tops, settings) **must** persist locally |
| FR-CONT-5 | Monetisation, if any, **must** be cosmetic only — no pay-to-win physics advantages |

### 3.7 Settings, localisation, accessibility

| Id | Requirement |
|---|---|
| FR-SET-1 | Audio volumes, graphics quality, language, control sensitivity, assist level |
| FR-LOC-1 | Vietnamese (primary) and English **must** be supported, with full diacritic rendering |
| FR-ACC-1 | UI **must** be usable without colour discrimination; text scalable to 150 % |
| FR-ACC-2 | All throws **should** be possible one-handed on mobile |

## 4. Non-functional requirements

### 4.1 Performance

| Id | Requirement |
|---|---|
| NFR-PERF-1 | 60 FPS on reference mid-range mobile (2022 Android, ~Snapdragon 7 Gen 1; iPhone 12) |
| NFR-PERF-2 | 60 FPS minimum, 120+ uncapped on reference desktop (GTX 1660 / M1) |
| NFR-PERF-3 | Simulation of 8 tops < 0.5 ms per fixed step on reference mobile |
| NFR-PERF-4 | Cold start to main menu ≤ 5 s mobile, ≤ 3 s desktop |
| NFR-PERF-5 | Install size ≤ 500 MB mobile (initial download ≤ 150 MB with Addressables) |
| NFR-PERF-6 | 30-minute session must not throttle below 50 FPS on reference mobile |

### 4.2 Quality and reliability

| Id | Requirement |
|---|---|
| NFR-QUAL-1 | The project **must** compile with zero errors on every commit to the main branch |
| NFR-QUAL-2 | Crash-free sessions ≥ 99.5 % |
| NFR-QUAL-3 | Engine-free code (Core, Simulation) **must** be unit-testable without Unity |
| NFR-QUAL-4 | All data assets **must** pass validation in CI |

### 4.3 Portability

| Id | Requirement |
|---|---|
| NFR-PORT-1 | Single codebase for Windows, macOS, Android, iOS |
| NFR-PORT-2 | Architecture **must not** preclude a Web build (TECHNICAL_DESIGN §9) |
| NFR-PORT-3 | Minimum OS: Windows 10, macOS 12, Android 8.0 (API 26), iOS 15 |

### 4.4 Security and privacy

| Id | Requirement |
|---|---|
| NFR-SEC-1 | No personal data collected without consent; analytics opt-in |
| NFR-SEC-2 | Online clients **must not** be able to set authoritative state |
| NFR-SEC-3 | Comply with App Store / Google Play policies, COPPA/GDPR-K for child audiences |

## 5. Constraints

* Unity 6 LTS, URP, C#, Input System (TECHNICAL_DESIGN §2).
* Repository shared with the Nonla website until a dedicated repository is created.
* Assets must be original or properly licensed; fonts must cover Vietnamese.

## 6. Assumptions and open questions

| # | Item | Owner |
|---|---|---|
| Q1 | Exact rules variants by region (Bắc/Trung/Nam) — which become official modes? | Design |
| Q2 | Business model: premium vs free with cosmetics | Product |
| Q3 | Online backend: Unity Gaming Services vs Photon (cost per CCU) | Tech, Phase 5 |
| Q4 | Age rating target (affects online chat — none planned) | Product |
