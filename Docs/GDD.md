# Đánh Cù Việt Nam — Game Design Document

| | |
|---|---|
| Status | Draft v0.1 (Phase 0) — vision and core loop; details evolve with playtests |
| Related | [SRS](SRS.md) · [PHYSICS_SPEC](PHYSICS_SPEC.md) · [TECHNICAL_DESIGN](TECHNICAL_DESIGN.md) |

## 1. Vision

> *The feel of a real con cù in your hand — the rope, the throw, the hum of
> wood on packed earth — in a sunlit village courtyard at Tết.*

A realistic, physics-first game of the Vietnamese spinning top. Easy to
pick up (throw and watch it spin), deep to master (throw quality, reading the
opponent's spin, the perfect *bổ*). Cultural authenticity is a pillar, not a
skin.

### Pillars

1. **Authentic physics** — every top behaves like the real object (PHYSICS_SPEC).
2. **The throw is the game** — winding, aiming and releasing the rope is the skill.
3. **Vietnamese soul** — places, sounds, words and rules from real village play.
4. **Fair competition** — same physics for player, AI and online; no pay-to-win.

### Audience

* Vietnamese adults who played *đánh cù* as children (nostalgia, Tết).
* Vietnamese diaspora families sharing the tradition with children.
* Casual mobile players worldwide who enjoy physics toys and short matches.

## 2. Cultural background

*Con cù* (Northern), also *con quay* (Southern), is a wooden top carved
from hard, dense wood — traditionally **guava (gỗ ổi)**, also *gỗ sến*,
*gỗ nghiến*, *gỗ mít* — with an **iron nail tip (chân đinh)**. A cotton or
hemp **rope (dây cù)** is wound from the tip upward; the player throws the
top down and back with a snap that unwinds the rope and sets it spinning.

Popular games include **quay lâu** (who spins longest), **bổ cù** (throwing
your top onto your opponent's spinning top to knock it down or split it), and
**ring games** where tops are knocked out of a drawn circle. It is played in
communal-house courtyards (*sân đình*), brick yards, village lanes, and
especially during **Tết** and village festivals.

Authenticity review by native players/artisans is required before content lock (SRS Q1, risk R8).

## 3. Core loop

```
Choose top ─▶ Wind rope ─▶ Aim ─▶ Throw ─▶ Watch / react ─▶ Round result ─▶ Rewards ─▶ ↺
```

* **Match length:** 1–4 minutes. Rounds 20–90 s (spin duration).
* **Session:** 3–5 matches (mobile commute length).

## 4. The throw (core interaction)

| Step | Touch / mouse | Gamepad | Feedback |
|---|---|---|---|
| Wind | Press & hold — rope wraps around the top (up to `ropeWraps`) | Hold RT | Rope animation, creak sound, wrap counter |
| Aim | Drag to place the landing marker on the ground | Left stick | Ground marker + trajectory hint (assist-dependent) |
| Throw | Swipe/flick toward the ground and release | Release RT with a flick of the stick | Whoosh, rope snap |
| Release timing | Release at the top of the swing arc (timing window) | same | "Perfect / Good / Late" + haptics |

Mapping to physics: PHYSICS_SPEC §7. Input actions: `PointerPress`,
`PointerPosition`, `Aim`, `Charge` (see `DanhCuInputActions`).

**Assist levels** (setting + difficulty): trajectory preview, timing window
width, auto-tilt correction. Assist never changes the physics, only the
throw-quality inputs.

## 5. Modes

| Mode | Vietnamese | Rulesets | Notes |
|---|---|---|---|
| Practice | Luyện tập | LongestSpin | Solo; personal best spin time; tutorial lives here |
| Versus AI | Đấu với máy | Strike, LongestSpin, RingOut | 3 difficulties: Dễ, Vừa, Khó |
| Online | Trực tuyến | Strike, LongestSpin, RingOut | 1v1 quick match + room code (Phase 5) |

### 5.1 Rulesets

* **Quay lâu — LongestSpin:** both players throw (simultaneously or in turn);
  last top still spinning wins the round. Tie if both stop within 0.25 s.
* **Bổ cù — Strike:** defender throws first and their top spins in the ring.
  Attacker throws onto it. If the defender's top falls or stops before the
  attacker's, attacker scores; otherwise defender scores. Roles alternate.
* **Đẩy vòng — RingOut:** both tops in the ring; a top whose tip leaves the
  ring loses. Second-chance throw allowed once per round.

Rounds to win: 1–5 (`GameModeDefinition.roundsToWin`).

## 6. Content

### 6.1 Tops (starter set in the project)

| Id | Name | Wood | Character |
|---|---|---|---|
| `cu_go_oi` | Cù gỗ ổi | Guava | Balanced classic |
| `cu_go_sen` | Cù gỗ sến | Sen ironwood | Heavy, hard to knock down, slower to spin up |
| `cu_go_mit` | Cù gỗ mít | Jackfruit | Light, fast, fragile |

Planned for v1.0: ≥ 8 tops, including *gỗ nghiến* (ironwood), lacquered
festival tops, and a master-artisan top. Stats shown to players (Stability,
Power, Endurance) are **derived** from physical properties, never authored
independently, so what you see is what the physics does.

### 6.2 Arenas

| Id | Name | Surface |
|---|---|---|
| `san_dinh` | Sân đình (communal-house yard) | Packed earth |
| *planned* | Sân gạch (brick yard) | Brick with grout seams |
| *planned* | Ngõ làng (village lane) | Uneven earth & stones |
| *planned* | Chợ Tết (Tết market) | Polished tile, crowd, decorations |

### 6.3 Progression (draft)

Earn *đồng xu* (coins) from matches → unlock tops and cosmetic ropes/paint.
Achievements for perfect throws, long spins, strikes. No energy systems, no
pay-to-win (SRS FR-CONT-5).

## 7. Camera

Low, close camera (tops are ~7 cm): orbit around the ring at ~1 m,
auto-framing both tops; slow-motion close-up on strikes and on the final
wobble of a round. Player-controlled orbit with `CameraOrbit`.

## 8. Art direction

* **Photoreal, warm, nostalgic.** Late-afternoon Northern-Vietnam light, dust in the air.
* Tops: PBR wood with visible grain, oil sheen, iron tip with rust; wear and chips accumulate.
* Environments: banyan tree, communal house roof (*mái đình*), red Tết lanterns, peach blossoms (*hoa đào*).
* UI: minimal, paper/lacquer textures, Vietnamese typography with full diacritics.

## 9. Audio

Wood hum whose pitch tracks spin rate; tip scratch on earth vs brick;
rope snap; crack on strike; village ambience (roosters, children,
distant Tết drums); traditional instrument motifs (*đàn bầu*, *sáo trúc*) in the menu.

## 10. Tutorial

Practice mode, 4 steps: wind → aim → throw → read the spin. Contextual tips
appear after failed throws (e.g. "Thả dây sớm quá! / Released too early!").

## 11. Open design questions

1. Simultaneous vs turn-based throws in LongestSpin online?
2. Should tops carry permanent damage between matches?
3. Regional rule variants as separate modes or ruleset options?
