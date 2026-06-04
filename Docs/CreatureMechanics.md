# Creature Capture Mechanics — Design Research

Status: **Research / draft for review.** No code changed by this document.

## Background & Goal

We're pivoting creature acquisition away from the current crate/RNG model (which got
pushback) toward a **capture** model. The chosen direction is the **Capture Card**
mechanic. This doc captures the full set of options researched, then details the
chosen Capture Card design.

### Why this fits the existing project

A capture loop reuses almost all current systems:

- **Inventory** (`Code/PlayerData/Inventory.cs`, `InventoryPetSlot`) — captured creatures
  are inventory slots, exactly like today.
- **Equip → spawn → orbit** (`PetFramework.Equip`, `Inventory.EquipPet`) — "deploying" a
  captured creature is the existing equip flow.
- **Reveal animation** (`InteractBuyCrate` phases `Rising` / `Celebrating` / `Collecting`)
  — the capture moment is this pipeline, optionally run in reverse (collapse-into-card).
- **Rarity** (`Code/Pets/PetRarity.cs`, `PetRarityExtensions`) — drives card border/foil.
- **Merge** (`Inventory.TryMergePets`) — becomes card fusion/upgrade.
- **Previews** (`PetPreviewRenderer`) — the card art is the existing preview texture.
- **Trading** (`Code/Trading/PlayerTradeController.cs`) — cards are inherently tradeable.

## IP / Patent Risk Note (not legal advice)

This is design-risk guidance only; get IP counsel before a commercial launch.

Concepts (e.g. "capturing creatures") are not patentable — the asserted Nintendo/Pokémon
patents target **specific mechanical combinations**, notably:

1. Aiming and **throwing a capture item at a field character** to trigger a capture attempt.
2. Using the **same aimed control to switch between throwing a capture item and summoning
   a fighting creature** into the field.
3. Mount/riding-while-fighting mechanics.

Design rules we follow to stay clear of that lane:

1. **Don't replicate** "throw sphere → creature sucked in → wobble → caught/escaped."
2. **Keep capture and deploy as separate verbs / inputs** (capture = throw at world creature;
   deploy = play a card from inventory). This sidesteps the most-cited claim.
3. Push capture toward **inscription / relationship / marking / transformation / essence**
   rather than containment.

---

## Researched Options (7)

| # | Mechanic | Core verb | Capture resolution | Codebase reuse | IP distance |
|---|----------|-----------|--------------------|----------------|-------------|
| 1 | **Capture Cards** (CHOSEN) | Throw card | Inscribe → deploy card | High | High |
| 2 | Spirit Lantern | Aim/channel | Siphon essence | High | High |
| 3 | Resin Figurines | Throw flask | Crystallize statue | High | High |
| 4 | Snare / Net | Throw net | Restrain + claim | Medium | High |
| 5 | Sigil Brand | Throw mark | Bind / follow | Medium | Very high |
| 6 | Bait & Befriend | Throw treat | Trust meter | Medium | Very high |
| 7 | Spore / Egg | Throw spore | Hatch egg | High | High |

### 2. Spirit Lantern (essence siphon)
Aim a lantern; a glowing wisp is drawn out of the creature into the flame. Rekindle at home
to re-form the creature. Differentiator: essence, not enclosure; channeled siphon, not a
thrown-and-contained ball.

### 3. Resin / Amber Figurines
Throw living resin; creature is encased in amber that hardens into a collectible figurine,
then awakened later. Differentiator: preservation/transformation, strong "display shelf"
meta. The amber cracking open rhymes with the hatch theme.

### 4. Snare / Net
Throw a net/bola/web that entangles the creature; a second "claim/tame" hold finishes the
capture. Differentiator: physical restraint + a distinct claim step; no shrinking, no vessel.

### 5. Sigil Branding
Throw a glowing sigil/brand that marks the creature; once marked it's pact-bound and follows
you home. Differentiator: allegiance/marking — the opposite of containment; cleanest IP
distance. Great for a social/multiplayer hub.

### 6. Bait & Befriend
Throw food/treats to raise a per-creature trust meter; max trust and it joins you. Different
creatures want different bait. Differentiator: relationship-based, throws a consumable not a
capture device. Pairs with existing stats tracking.

### 7. Spore / Egg Seeding (on-theme)
Throw a spore that compresses the creature into an egg/seed you collect and hatch. The project
is literally `hatch_simulator`, so this keeps the hatch identity and reuses the reveal pipeline
almost verbatim. Strong candidate to pair with Capture Cards as the *resolution* visual.

---

## CHOSEN: Capture Card Mechanic

### Fantasy
You're an arcanist who **inscribes** creatures onto enchanted cards. Throw a blank Capture
Card at a (weakened) creature; instead of being contained, the creature is **drawn as living
artwork** onto the card. Later you **deploy** the card to summon the creature beside you.

### Two separate verbs (core design + IP safety)
- **Capture** = throw a blank card at a creature in the world.
- **Deploy** = play the card from inventory → routes into existing `Inventory.EquipPet` /
  `PetFramework.Equip`. Equipped = card is "in play."

Keeping these as different actions (and not binding both to the same aimed field input) is
deliberate for IP distance.

### The capture moment (animation)
Reuse the reveal pipeline in reverse: a blank card spins out, hovers before the creature, the
creature is "sketched" into the card frame (line art → color), the card snaps shut and flips
into the player's hand. This maps onto the existing `Rising`/`Celebrating`/`Collecting` phases.

### How it maps to current systems
- **A card = an `InventoryPetSlot` with a face.** Card art = the `PetPreviewRenderer` texture.
- **Rarity = card border / foil** via `PetRarityExtensions.GetCssColor` (holo foils for
  Mythic / Ancestral).
- **Merge = card fusion/upgrade** via existing `Inventory.TryMergePets`.
- **Trade = card trading** via existing trade system.
- **Deploy = equip** (orbit/follow already implemented in `PetFramework`).

### Optional sub-systems to consider later
- **Card charges/durability** — a card deploys N times, or capture quality degrades, giving an
  economy sink that isn't RNG.
- **Capture difficulty** — rarer creatures need weakening first (ties to combat) or special
  card types, making capture a skill/decision rather than a roll.
- **Resolution flavor** — optionally fuse with #7 (egg/hatch) so a captured card "hatches"
  when deployed, preserving the `hatch_simulator` identity.

### Open questions for review
1. Where do wild creatures live/spawn? (Hub roaming vs. dedicated capture zones — affects the
   AreaSpawner / DunGen systems.)
2. Is there a weakening/combat step before capture, or is the throw the whole interaction?
3. Are cards consumable (one creature per card) or reusable vessels?
4. Should capture be aimed/world-thrown, or initiated from a UI when near a creature?
   (UI-initiated is the most IP-conservative.)
5. Does deploying remove the card from inventory, or is the card permanent and "deploy" just
   toggles equip (matching today's equip/unequip)?

### Rough implementation lift (high level — to be specced separately)
- New "wild creature" world entity + spawner integration.
- A capture interaction (`Code/Interactions/InteractCaptureCreature.cs` or similar) that, on
  success, calls `Inventory.AddPetPrefab(...)` — i.e. the grant path already used by crates.
- Reskin/extend the reveal animation for the card inscription.
- Card-styled rendering in the inventory/UI (largely a styling pass over existing pet cards).

> Next step after review: pick answers to the open questions, then I'll write a full
> implementation spec mapped to the actual classes and the wild-creature → capture → inventory
> data flow.
