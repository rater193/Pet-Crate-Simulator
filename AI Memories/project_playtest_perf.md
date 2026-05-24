---
name: project-playtest-perf
description: A spring-2026 playtest surfaced client-side low-FPS complaints; drove PetFramework per-frame optimization work
metadata: 
  node_type: memory
  type: project
  originSessionId: 07ea938f-16d5-4065-9054-31a3a162e75c
---

A multiplayer playtest a few weeks before 2026-05-23 generated complaints about low FPS from players (clients), while the host (the user) ran it fine. The user suspected per-frame/frequent RPCs blocking frames.

**Why:** Classic host-fine / clients-laggy symptom. Root cause was not RPC blocking (s&box RPCs are fire-and-forget). The real driver was per-frame work in `PetFramework.OnUpdate` that scales with player count on every client: a full child-hierarchy walk (`DiscoverEquippedPets`) every frame for every player including proxies, a full `Scene.GetAllComponents<InteractGivePlayerCoin>()` scan every idle frame, and a forced hierarchy refresh on the `AddMoney` coin-hit path.

**How to apply:** When the user reports performance issues, look first at per-frame loops that scale with players/objects and run on proxies — not just RPC frequency. For the fix applied here, see git history on `Code/Pets/PetFramework.cs` (throttled refresh via `PetRefreshInterval`, throttled auto-battle scan via `AutoBattleScanInterval`). Related: [[feedback-working-style]].
