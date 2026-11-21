# Đánh Cù Việt Nam — Physics Specification

| | |
|---|---|
| Status | Draft for Phase 1 spike. Coefficients are first-pass estimates to be calibrated against real tops |
| Code | `PhysicsTuningProfile`, `TopDefinition`, `TopMaterialDefinition`, `ArenaDefinition` (data) · `DanhCu.Simulation` (Phase 1) |

The spinning top is the product. If it does not precess, wobble, wander and
die like a real *con cù*, nothing else matters. This document defines what
"real" means in numbers so it can be implemented and tested.

## 1. Conventions

* **SI units everywhere** in data and code: metres, kilograms, seconds,
  radians, rad/s. RPM appears only in authoring fields and UI
  (`ω[rad/s] = RPM · 2π / 60`).
* Unity world axes: **+Y up**, left-handed. Gravity `g = (0, −9.81, 0)` m/s².
* A top's body frame: `e₃` = symmetry axis pointing from tip to crown.
* Spin `ω₃ = ω · e₃`. Tilt `θ` = angle between `e₃` and world up.

## 2. Simulation approach

Three options were considered:

| Option | Description | Pros | Cons |
|---|---|---|---|
| A. Pure PhysX | Rigidbody + MeshCollider, let PhysX do everything | No custom code | Max angular speed clamp; gyroscopic behaviour not guaranteed (PhysX 4 integrators commonly omit or approximate the ω×Iω term — **must be verified in the spike**); single-point tip contact jitters; not deterministic across platforms; no headless server without Unity |
| B. PhysX + custom torques | Rigidbody for contacts; a `TopDynamics` component adds gyroscopic, tip-friction, righting and drag torques each fixed step | Reuses PhysX collisions | Fighting the solver; tuning interacts with PhysX internals |
| **C. Dedicated simulation** (recommended) | `DanhCu.Simulation`, engine-free: each top is an axisymmetric rigid body with an analytic tip–ground contact; top–top collisions via capsule/cone proxies; PhysX only for cosmetic debris | Exact control of the behaviours that matter; unit-testable; runs headless on a server; repeatable within one build | More code to own; we must write contact handling |

**Decision:** prototype **C** in Phase 1, with **B** as fallback. The arena is
a near-plane plus a ring and a match has ≤ 4 tops, so the collision problem is
small. The decision is final at the end of the Phase 1 spike against §9.

## 3. Body model

### 3.1 Parameters

A top = **shape** (`TopData`: every dimension of body, tip and rope zone, plus
metal density) + **wood** (`TopMaterialDefinition`: density ρ_w, restitution e,
body friction μ_b) + gameplay values on `TopDefinition`. Geometry, prototype
dimensions (assumptions, not historical data) and screenshots: [TOP_MODEL.md](TOP_MODEL.md).

| Symbol | Source | Medium · gỗ ổi |
|---|---|---|
| H, D = 2R | `TopData.totalHeight`, `bodyDiameter` | 75 mm, 55 mm |
| ρ (tip point radius) | `TopData.tipPointRadius` | 0.8 mm |
| ρ_w, ρ_m | wood density, `TopData.metalDensity` | 900, 7850 kg/m³ |
| m | **integrated** (§3.2) | 90.3 g |
| l | **integrated** CoM height above the pivot | 45.7 mm |
| ω_max | `TopDefinition.maxLaunchRpm` | 2800 RPM (293 rad/s) |

### 3.2 Mass properties (integrated from the geometry)

The top is a solid of revolution about +Y with outer wood radius r_w(y) and a
metal core r_m(y) (point, taper, shank and embedded shank). Thin slices give

```
dm  = π[ρ_m r_m² + ρ_w (r_w² − r_m²)] dy
dI₃ = π/2 [ρ_m r_m⁴ + ρ_w (r_w⁴ − r_m⁴)] dy
I₁(pivot) = ∫ (dI₃/2 + y² dm),   l = ∫ y dm / m,   I₁(CoM) = I₁(pivot) − m·l²
```

integrated with composite 3-point Gauss–Legendre quadrature, split at every
profile kink (`RevolvedMassIntegrator`; verified against the closed-form cylinder
and cone). This replaces the earlier solid-cone approximation. The Rigidbody gets
`mass`, `centerOfMass = (0, l, 0)` and `inertiaTensor = (I₁(CoM), I₃, I₁(CoM))`.

Default tops (computed):

| Top | Shape | m | l | I₃ (kg·m²) | I₁ pivot (kg·m²) | ω_crit (rad/s) | ω_max / ω_crit |
|---|---|---|---|---|---|---|---|
| Cù gỗ mít | Small | 32.6 g | 36.3 mm | 6.2e-6 | 5.1e-5 | 247 (2362 RPM) | 1.27 |
| Cù gỗ ổi | Medium | 90.3 g | 45.7 mm | 2.6e-5 | 2.2e-4 | 229 (2183 RPM) | 1.28 |
| Cù gỗ sến | Large | 181.8 g | 54.9 mm | 7.6e-5 | 6.4e-4 | 208 (1983 RPM) | 1.31 |

Phase 1 calibration: measure real tops (mass, CoM by balancing, I₃ by bifilar
pendulum) and adjust the TopData dimensions until the integrated values match.

## 4. Dynamics

State per top: tip position `p`, velocity `v`, orientation `q`, angular
velocity `ω`, plus flags (airborne, fallen).

### 4.1 Rigid body

Euler's equations in the body frame (symmetric top, I₁ = I₂):

```
I · dω/dt + ω × (I · ω) = τ
```

The gyroscopic term `ω × (I·ω)` is **mandatory**: it is what produces
precession. With fast spin, the steady precession rate is

```
Ω_p ≈ m · g · l / (I₃ · ω₃)
```

(slow when spinning fast, accelerating as spin decays — the characteristic
"death spiral").

### 4.2 Stability ("sleeping" top)

A top on a fixed pivot is stable upright when

```
I₃² · ω₃² > 4 · I₁ · m · g · l      ⇔      ω₃ > ω_crit = 2·√(I₁·m·g·l) / I₃
```

Implemented as `TopMassProperties.CriticalAngularSpeed(g)` (exposed on `TopDefinition`). Data validation
requires `ω_max ≥ 1.2 · ω_crit`, otherwise a perfect throw could never stand
up. `PhysicsTuningProfile.criticalSpinMultiplier` scales ω_crit for feel.

### 4.3 Tip–ground contact

Contact point `c = p` (tip), normal force `N` from penetration constraint.

* **Pivot friction torque** (spin drain), circular contact patch:
  `τ_pivot = −(2/3) · μ_t · N · r_t · sign(ω₃) · e₃` where μ_t =
  `ArenaDefinition.tipFriction`.
* **Rolling resistance:** `τ_roll = −C_rr · N · r_t · ω̂` with
  `C_rr = ArenaDefinition.rollingResistance`.
* **Sliding friction** of the tip (wandering): contact velocity
  `v_c = v + ω × (c − x_com)`; Coulomb `F = −μ_t · N · v̂_c`, regularised
  below 1 mm/s to avoid chatter.
* **Righting effect:** sliding friction at a tilted, fast-spinning tip
  produces a torque that reduces θ (why a well-thrown top "rises" and
  sleeps). Emerges from the contact model; `rightingGain` scales it for feel.
* **Surface unevenness:** a deterministic noise field of amplitude
  `ArenaDefinition.unevenness` perturbs the contact normal.

### 4.4 Air drag

`τ_air = −k_air · ω₃ · |ω₃| · e₃`, `k_air = airDragCoefficient` (default 2e-8 N·m·s²).
Linear air drag on translation is negligible and omitted.

### 4.5 Falling and stopping

* The top **falls** when the body touches the ground (θ ≥ θ_rim, the
  tilt at which the outline first reaches the ground: θ_rim = min atan(y / r(y)) over the profile; ≈ 54° for all three prototype shapes). It then rolls on its side under
  PhysX-style rigid contact until at rest.
* The top counts as **stopped** for rules when it has fallen *or*
  `|ω₃| < stoppedSpinThreshold` (default 8 rad/s).

## 5. Collisions

### 5.1 Top–top

* Proxy shape: the convex hull of the body outline plus the tip sphere (TOP_MODEL §1 Colliders).
* Normal impulse with restitution `e = √(e₁ · e₂)` of the two materials.
* Tangential impulse from relative surface velocity including spin
  (`v_rel = v₁ + ω₁ × r₁ − v₂ − ω₂ × r₂`), capped by Coulomb `μ_b · J_n`.
  The spin-coupled part is scaled by `collisionSpinTransfer`.
* **Strike (bổ cù):** the attacker's iron tip landing on the defender's
  crown or shoulder. The impulse applied above the defender's CoM tilts it;
  if θ exceeds θ_rim the defender falls. Durability (`TopMaterialDefinition.durability`)
  reduces the chance of the "split" visual effect (cosmetic only, Phase 4).

### 5.2 Top–environment

Arena boundary (ring is a drawn line, not a wall), props and debris use
PhysX colliders. Contacts with static PhysX geometry are queried by the
simulation (raycast/overlap) so tops never depend on PhysX integration.

## 6. Time stepping

| Parameter | Value | Field |
|---|---|---|
| Unity fixed step | 1/120 s | `fixedTimestep` |
| Simulation sub-steps | 2 → Δt = 1/240 s | `simulationSubsteps` |
| Integrator | semi-implicit Euler for translation; quaternion integration of ω with gyroscopic term treated implicitly (stable at high ω) | — |

At ω = 300 rad/s and Δt = 1/240 s the body turns 1.25 rad per step; this
is fine for a symmetric body (spin about e₃ does not change contact
geometry) but explicit integration of the gyroscopic term would gain energy,
hence the implicit treatment.

### 6.1 Engine settings applied at boot

`EngineSettingsApplier.ApplyPhysics` sets `Time.fixedDeltaTime`,
`Physics.gravity`, solver iterations, `Physics.defaultMaxAngularSpeed = 450 rad/s`
(PhysX default is 7 rad/s and would destroy any top that touches PhysX),
`sleepThreshold ≈ 0` (spinning tops must never sleep) and
`bounceThreshold`.

## 7. Launch model (rope throw)

Real throw: rope wound `n` times around the body; the player throws the top
downward while pulling the rope back, unwinding it.

| Input | Meaning |
|---|---|
| wind-up `w ∈ [0,1]` | how many of `ropeWraps` are wound (hold duration / trigger) |
| release power `P ∈ [0,1]` | swipe speed (touch/mouse) or trigger velocity (gamepad) |
| release timing `T ∈ [−1,1]` | error relative to the ideal release moment (0 = perfect) |
| aim `(yaw, distance)` | landing point on the ground |

```
ω₀ = ω_max · w · P · (1 − k_t · T²)
v₀ = ballistic velocity to the aimed landing point, scaled by P
θ₀ = θ_err · |T| + noise(aim error)          (initial tilt)
```

`k_t`, `θ_err` and assist strength live in a throw-tuning asset (Phase 2).
AI uses the same model with error drawn from `AIDifficultyProfile`.

## 8. Determinism & networking

* Within one build/platform the simulation must be **repeatable**: same
  inputs + same seed → same result (no `UnityEngine.Random`, no frame-rate
  dependence, no dictionary-order dependence).
* Across platforms bit-exact results are **not** required; online play is
  server-authoritative (TECHNICAL_DESIGN §8).

## 9. Acceptance criteria (Phase 1 exit)

| # | Criterion | Measure |
|---|---|---|
| P1 | Spin duration | Medium gỗ ổi top at ω_max on packed earth spins 40–90 s before falling (real tops: ~1 min) |
| P2 | Precession direction & rate | Same sense as spin; within 15 % of `m·g·l/(I₃·ω₃)` for θ = 10°, ω₃ = 200 rad/s |
| P3 | Sleeping | Thrown at 10° tilt with ω₃ > 1.3 ω_crit, tilt decreases below 3° within 3 s |
| P4 | Death spiral | Below ω_crit, tilt grows monotonically and the top falls within 10 s |
| P5 | Energy | With no input, total energy never increases by more than 0.1 % over any 1 s window |
| P6 | Timestep independence | Spin duration at 240 Hz vs 480 Hz differs by < 5 % |
| P7 | Repeatability | Identical inputs → identical state after 60 s (same build) |
| P8 | Strike | A centred strike with 2 m/s impact topples a defender spinning at 1.2 ω_crit in ≥ 80 % of seeded trials |
| P9 | Performance | 8 tops: < 0.5 ms per fixed step on the reference mid-range Android phone (IL2CPP) |
| P10 | Feel | Blind playtest: ≥ 7/10 testers familiar with con cù rate motion "realistic" (≥ 4/5) |

## 10. Tuning parameters

All global coefficients are in `PhysicsTuningProfile`; per-top and per-arena
values are on their definitions. Changing a symbol here requires changing the
field (and vice versa) in the same commit.
