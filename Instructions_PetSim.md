\# AI Development Guide for Pet Crate Simulator



You are working on a s\&box game project. Be a careful, practical s\&box developer: read the existing code before changing it, use the engine APIs correctly, and verify your work before handing it back.



\## First Steps



Before editing code, inspect the local project. Learn the current component patterns, prefab layout, networking style, and naming conventions from the files already in the repo.



Use these references when you are unsure about s\&box APIs:



&#x20; \* Local s\&box documentation root: `E:\\AI\\ReferencedFiles\\Docs`

&#x20; \* Local s\&box docs index: `E:\\AI\\ReferencedFiles\\Docs\\llms.txt`

&#x20; \* Local s\&box API reference: `E:\\AI\\ReferencedFiles\\Docs\\api`

&#x20; \* Local s\&box developer docs: `E:\\AI\\ReferencedFiles\\Docs\\dev\\doc`

&#x20; \* Local s\&box engine/public source: `E:\\AI\\ReferencedFiles\\Repos\\sbox-public-master`

&#x20; \* Facepunch sandbox project examples: `E:\\AI\\ReferencedFiles\\Repos\\sandbox-main`

&#x20; \* Local API reference files when you need exact method names or component behavior.

&#x20; \* If you are unable to find the information you require to achieve a tasks, Check the sbox_explorer mcp for what you require.

&#x20; \* If you create a framework that is not located on MCP, Contribute the framework to the MCP under sbox_contribute.

&#x20; \* When contributing to MCP, provide the user a public link to the contribution when a slug is available. Use this URL pattern:

&#x20;   - `https://sbox.grimtech.co.uk/mcp-knowledge?slug={slug}`



Do not guess engine APIs when the answer can be checked locally, in local source, or in official docs. s\&box APIs change, and small naming mistakes can waste a lot of time.



\## Local Documentation, Source, and Session Memory Bootstrap



This project uses a local-first documentation and source workflow.



The local s\&box documentation folder is:



`E:\\AI\\ReferencedFiles\\Docs`



The local s\&box engine/public source folder is:



`E:\\AI\\ReferencedFiles\\Repos\\sbox-public-master`



At the start of each new development session, before answering project-specific questions or modifying code, build a working session memory from the local project files, the local s\&box documentation, and the local s\&box engine/public source.



Session memory means temporary working knowledge for the current AI session. Do not assume this knowledge persists after the session restarts unless it exists in a local file.



\### Required Startup Behavior



Before beginning development work:



1\. Read this project bootstrap document.

2\. Scan the current project folder.

3\. Read the highest-priority local project files first:

&#x20;  - `README.md`

&#x20;  - `AGENTS.md`

&#x20;  - project `.sbproj` files

&#x20;  - important `.cs` files

&#x20;  - important `.razor` files

&#x20;  - important `.prefab` files when prefab work is involved

&#x20;  - project configuration files

4\. Read the local s\&box documentation index:

&#x20;  - `E:\\AI\\ReferencedFiles\\Docs\\llms.txt`

5\. Scan the local s\&box engine/public source folder:

&#x20;  - `E:\\AI\\ReferencedFiles\\Repos\\sbox-public-master`

6\. Build a working session map of:

&#x20;  - project folder structure

&#x20;  - important components and systems

&#x20;  - existing coding patterns

&#x20;  - prefab layout

&#x20;  - networking style

&#x20;  - UI/Razor style

&#x20;  - asset paths

&#x20;  - known conventions

&#x20;  - relevant local documentation files

&#x20;  - relevant local API reference files

&#x20;  - relevant local engine/source files

&#x20;  - engine API examples and implementation details

7\. Use this session map as working memory for the current session.

8\. Refresh the session memory when:

&#x20;  - new files are created

&#x20;  - existing files are modified

&#x20;  - the user points to a new folder

&#x20;  - the user asks about a system that has not been inspected yet

&#x20;  - the answer depends on documentation, source, or code that has not been read yet



\### Local-First Documentation and Source Rule



Always inspect local files before using internet sources.



Use this priority order:



1\. Project-specific files in the current repo.

2\. Local s\&box documentation under `E:\\AI\\ReferencedFiles\\Docs`.

3\. Local s\&box API reference under `E:\\AI\\ReferencedFiles\\Docs\\api`.

4\. Local s\&box engine/public source under `E:\\AI\\ReferencedFiles\\Repos\\sbox-public-master`.

5\. Existing source code examples in the project.

6\. Public internet sources only when the local files are missing, incomplete, or insufficient.



Do not search the internet unless:



\- The relevant local file is missing.

\- The local documentation/source is incomplete.

\- The user explicitly asks for the latest online documentation.

\- The local documentation/source conflicts with observed engine behavior and verification is required.



When a topic is covered by a local file, prefer that local file as the primary source of truth.



\### Local s\&box Documentation and Source Paths



When working on s\&box topics, use these local folders:



\- General docs: `E:\\AI\\ReferencedFiles\\Docs\\dev\\doc`

\- API reference: `E:\\AI\\ReferencedFiles\\Docs\\api`

\- UI/Razor docs: `E:\\AI\\ReferencedFiles\\Docs\\dev\\doc\\ui`

\- Networking docs: `E:\\AI\\ReferencedFiles\\Docs\\dev\\doc\\networking`

\- Scene/component docs: `E:\\AI\\ReferencedFiles\\Docs\\dev\\doc\\scene`

\- Physics/tracing docs: `E:\\AI\\ReferencedFiles\\Docs\\dev\\doc\\physics`

\- Rendering/shader docs: `E:\\AI\\ReferencedFiles\\Docs\\dev\\doc\\rendering`

\- Asset/resource docs: `E:\\AI\\ReferencedFiles\\Docs\\dev\\doc\\assets`

\- Gameplay/input/navigation docs: `E:\\AI\\ReferencedFiles\\Docs\\dev\\doc\\gameplay`

\- Editor tooling docs: `E:\\AI\\ReferencedFiles\\Docs\\dev\\doc\\editor`

\- Dedicated server docs: `E:\\AI\\ReferencedFiles\\Docs\\dev\\doc\\networking\\dedicated-servers`

\- Engine/public source: `E:\\AI\\ReferencedFiles\\Repos\\sbox-public-master`



Before generating or modifying s\&box code, identify which local docs/source files are relevant, read them, then apply the answer to the project.



\### Engine Source Usage Rules



Use the local engine/public source folder when:



\- Documentation does not clearly explain an API.

\- You need exact method names, overloads, attributes, or property behavior.

\- You need to understand how a component, system, or helper works internally.

\- You need examples of expected usage.

\- You need to verify whether an API exists.

\- You need to resolve conflicts between memory, docs, and actual source.



When using the local engine/public source:



\- Prefer source definitions over assumptions.

\- Prefer official source examples over guessed code.

\- Check method signatures before calling APIs.

\- Check attributes and access modifiers before using members.

\- Check namespaces before adding imports.

\- Do not copy large engine source sections into project code unless explicitly required.

\- Use source behavior to guide small, project-appropriate code.



\### File Analysis Expectations



When reading files into working session memory, extract and remember:



\- important classes

\- important components

\- public APIs

\- method signatures

\- property names

\- `\[Property]` usage

\- `\[Sync]` usage

\- RPC usage

\- component lifecycle methods

\- networking ownership rules

\- UI/Razor conventions

\- prefab structure

\- asset paths

\- naming conventions

\- TODOs

\- warnings

\- known issues

\- build or runtime errors

\- project-specific helper methods

\- project-specific base classes

\- relevant engine/source implementation details



Do not invent APIs, component names, asset paths, or method signatures that were not found in the local docs, local source, or the project files.



\### Answering Rules



When answering development questions:



\- Read relevant local files first.

\- Mention which files were checked when useful.

\- Prefer project code over generic examples.

\- Prefer local docs and local source over internet results.

\- If documentation and project code disagree, prefer the project code and mention the conflict.

\- If documentation and engine source disagree, prefer the local engine/source behavior and mention the conflict.

\- If a file is missing, say so clearly.

\- If the answer depends on a file that has not been read, read it before answering.

\- If unsure, search the local files and local source again before guessing.



\### Code Modification Rules



Before editing or creating code:



1\. Scan the project structure.

2\. Locate similar existing files.

3\. Match the existing style and patterns.

4\. Check local docs/ references for correct usage.

5\. Check local engine/public source when exact behavior or signatures matter.

6\. Apply the change.

7\. Re-read the affected file after editing.

8\. Verify imports, namespaces, method names, lifecycle hooks, and file paths.

9\. Validate the change with a build or the closest available validation command.



Do not make broad unrelated refactors unless they are necessary to complete the requested task safely.



\## Working Style



&#x20; \* Explain what you are checking and why while you work, especially when reading files, touching prefabs, or changing networking.

&#x20; \* Be proactive. If the user asks for a fix or feature, implement it instead of only proposing a plan.

&#x20; \* Keep changes scoped to the request. Do not refactor unrelated systems unless the refactor is necessary to finish safely.

&#x20; \* Preserve the user's edits. The worktree may be dirty; never reset, revert, or overwrite changes you did not make unless the user explicitly asks.

&#x20; \* Prefer simple, readable component code over clever abstractions.

&#x20; \* If a design choice is ambiguous, follow the patterns already used in this project.



\## s\&box Project Notes



&#x20; \* Components usually inherit from `Component`; interaction components may inherit from project-specific base classes such as `Interactable`.

&#x20; \* Use `\[Property]` for editor-configurable values.

&#x20; \* Use `\[Sync]` for state that needs to replicate.

&#x20; \* Use RPCs deliberately:

&#x20;   \* `\[Rpc.Host]` for work that must execute on the host.

&#x20;   \* `\[Rpc.Owner]` for work that must execute only on the owning client.

&#x20;   \* `\[Rpc.Broadcast]` for cosmetic or shared events that all clients should see.

&#x20; \* Avoid hiding gameplay side effects in UI update methods. UI methods like healthbar refreshes should update UI, not award money or mutate gameplay state.

&#x20; \* For local-only player data, check the existing singleton/reference pattern before adding a new one.



\## Networking Guidance



Think carefully about who owns the object and who is allowed to mutate state.



&#x20; \* Player money is local-player-owned data. Award money through `PlayerData` using the existing RPC flow instead of directly changing another client's local value.

&#x20; \* If a player interaction should reward the interacting player, get the `PlayerData` from the interacting player's controller/component path and call the appropriate `PlayerData` method/RPC.

&#x20; \* Keep authoritative gameplay state separate from cosmetic animation.

&#x20; \* For smooth network feel, drive important replicated transforms from the owner/host, but broadcast cosmetic events when remote clients should play the same hit, flash, lunge, or bob animation.

&#x20; \* Do not rely on every client being allowed to set another player's local data.



\## Prefab Safety



Be extremely careful editing `.prefab` files. They are JSON-like, but s\&box relies on metadata fields such as `\_\_type`, `\_\_guid`, `\_\_version`, and component structure.



&#x20; \* Do not round-trip prefab files through generic JSON serializers unless you have verified they preserve all s\&box metadata exactly.

&#x20; \* In particular, never strip component `\_\_type` fields. If those are missing, s\&box will show missing components and models may stop rendering.

&#x20; \* Prefer small text patches or carefully targeted edits for prefab changes.

&#x20; \* After editing prefabs, validate that:

&#x20;   \* The file still parses.

&#x20;   \* Each component still has the correct `\_\_type`.

&#x20;   \* Model renderers still use `Sandbox.ModelRenderer`.

&#x20;   \* Model paths still point to valid `.vmdl` assets.

&#x20;   \* Child object transforms, especially rotations and scales, are intentional.



\## Assets and UI



&#x20; \* When rendering images from `.razor` UI using project assets, use `.vtex` textures. A plain `.png` may work in editor but fail for clients outside the editor.

&#x20; \* The user can create `.vtex` assets in s\&box by right-clicking an image in the asset browser and choosing the texture creation option.

&#x20; \* Reference actual project assets where possible. Do not invent asset paths.



\## Current Project Knowledge



These patterns are important in this project:



&#x20; \* `InteractGivePlayerCoin` handles destructable-style interact targets that lose health when the player presses Use.

&#x20; \* Money rewards should go through `PlayerData`, not through UI refresh code.

&#x20; \* `PlayerData.AddMoney(...)` applies pet coin multipliers through `PetFramework`.

&#x20; \* `PetFramework.EquipPet(GameObject prefab)` should clone the prefab before equipping it.

&#x20; \* Equipped pets are stored under the player prefab/player object, and the player prefab owns `PetFramework`.

&#x20; \* Pet prefabs live under `Assets/Prefabs/Pets`.

&#x20; \* Pet model source files live under `Assets/3rdparty/kenny.nl`.

&#x20; \* Pet prefabs should have:

&#x20;   \* A root object with `PetComponent`.

&#x20;   \* An `AnimalModel` child.

&#x20;   \* A `Sandbox.ModelRenderer` on the model child.

&#x20;   \* The model child rotation set intentionally, usually identity: `0,0,0,1`.

&#x20; \* Pet coin multipliers are configured on `PetComponent`.

&#x20; \* Pets should orbit the player evenly, move toward their positions instead of teleporting, touch the ground when possible, bob while moving, and lunge/bob toward the destructable the player attacks.



\## Animation and Feel



For simple game-feel animation:



&#x20; \* Prefer time-based interpolation and easing over instant jumps.

&#x20; \* Keep the gameplay object stable when possible and animate child visual objects for bounce, squash, lunge, flash, or bob effects.

&#x20; \* Separate the base/rest transform from the visual offset so effects can cleanly return to zero.

&#x20; \* Use configurable `\[Property]` values for distances, durations, speeds, colors, and multipliers so the user can tune them in the editor.

&#x20; \* For hit feedback, broadcast cosmetic effects when everyone should see them.

&#x20; \* For pets or followers, a simple "move toward target position" behavior is often enough; do not add pathfinding unless the user asks for it.




## Current Project Addendum: JSON and Pet Inventory

These notes reflect the current project state and should be treated as project-specific conventions.

### JSON Data Framework

- `JSONObject` lives at `Code/Core/JSONObject.cs`.
- Use `JSONObject` directly, with no namespace import.
- Expected usage:
  - `JSONObject.ToJson(JSONObjectInstance)`
  - `JSONObject.FromJson(JSONString)`
  - `JSONObject.Set("Key", value)`
  - `JSONObject.Exists("Key")`
  - `JSONObject.Remove("Key")`
- `JSONObject` supports nested `JSONObject` values and array/list values.
- For persistence of engine objects or prefab references, prefer stable string paths over serializing live `GameObject` references directly.
- When adding save/load data, keep JSON keys explicit and stable so existing player saves remain compatible.

### Player Pet Inventory

- `PlayerData` exposes the player inventory through `playerdata.inventory`.
- `Inventory` is the player's pet inventory API.
- Pet inventory slots are stored as `List<InventoryPetSlot>` on `Inventory.Slots`.
- `Inventory.Count` returns the number of valid used pet slots.
- Supported pet inventory API:
  - `playerdata.inventory.EquipPet(int slotNumber)`
  - `playerdata.inventory.GetPet(int slotNumber)`
  - `playerdata.inventory.RemovePet(int slotNumber)`
  - `playerdata.inventory.ToJson()`
  - `playerdata.inventory.LoadJson(string jsonData)`
- `InventoryPetSlot` is a component used as prefab-configurable inventory data.
- `InventoryPetSlot` stores:
  - `DisplayName`
  - `PetPrefab`
  - `PetPrefabPath`
  - `Rarity`
- `PetPrefabPath` is the preferred save/load value.
- `Inventory.EquipPet(...)` resolves the selected inventory slot and calls the existing `PetFramework.Equip(...)` flow.
- Do not bypass `PetFramework` when equipping pets.

### Pet Inventory Data Prefabs

- Pet inventory data prefabs live under:
  - `Assets/Prefabs/Inventory/PetInventoryData`
- Each pet inventory data prefab should have:
  - A root object.
  - An `InventoryPetSlot` component.
  - A `DisplayName` matching the pet.
  - A `PetPrefabPath` pointing to the matching pet prefab under `Prefabs/Pets`.
- Existing player pet prefabs live under:
  - `Assets/Prefabs/Pets`
- When new pet prefabs are added, also create a matching inventory data prefab.
- Keep inventory data prefabs small and data-only unless gameplay requires otherwise.

### Build Validation

- Preferred validation command:
  - `dotnet build E:\Git\Pet-Crate-Simulator\Code\hatch_simulator.csproj -v:minimal`
- Existing warnings may appear in unrelated files. Treat new errors or new warnings in touched files as blockers.
- If build access fails because of s&box or Steam install permissions, report the exact access-denied path and run the closest available validation.




\## Validation Checklist



Before finishing a coding task:



&#x20; \* Read the final diff.

&#x20; \* Check for accidental unrelated edits.

&#x20; \* Validate prefab/component metadata if any assets were touched.

&#x20; \* When adding new `PetComponent` properties, update existing pet prefabs so the fields are visible/configurable in the editor. Do this with targeted edits and verify the new fields only land on `PetComponent`, not on root objects or `Sandbox.ModelRenderer`.

&#x20; \* Run a build or the closest available validation command.

&#x20; \* If the build cannot run because of local permissions or environment issues, say that clearly and report what validation did run.

&#x20; \* Mention changed files and the important behavior changes in the final response.



\## Pet System Notes



&#x20; \* `PetComponent` is the per-pet content configuration source. Put future pet tuning values there when they belong to the pet prefab, such as display name, coin multiplier, damage, attack interval, attack range multiplier, movement multiplier, bob multiplier, and lunge multiplier.

&#x20; \* Pet display names are stored on `PetComponent.DisplayName`, not on prefab root objects or renderer components.

&#x20; \* Pet coin multipliers are additive bonuses, not sequential multipliers. Example: `1.1`, `1.8`, and `1.3` should become `1 + 0.1 + 0.8 + 0.3 = 2.2x`.

&#x20; \* Equipped pets should move in world space. They should walk toward assigned world positions rather than relying on the playerâ€™s local transform.

&#x20; \* Pets orbit the player when idle and swarm around the playerâ€™s active destructable target when attacking.

&#x20; \* Pet attacks should use `InteractGivePlayerCoin.ApplyPetDamage(...)` so pet damage follows the same health, reward, hit feedback, and destroy flow as player attacks.

&#x20; \* Pet attack visuals should face the target and lunge in the target direction.

&#x20; \* Pet rotation should be interpolated with `PetTurnSpeed`; avoid directly snapping `WorldRotation` every frame unless explicitly desired.



When you are ready to begin after reading this file and the project context, say:



`Ok, lets get started with development! Tell me what to do!`


## Current Project Addendum: Pet Inventory UI and Preview Rendering

These notes reflect the current pet inventory UI implementation and should be treated as project-specific conventions.

### Pet Inventory UI

- `PetInventoryPanel` lives at `Code/UI/PetInventoryPanel.razor`.
- `PetInventoryPanel` is a child Razor `Panel`, not a `PanelComponent` scene component.
- Do not add `PetInventoryPanel` directly to a scene object. It is hosted by `PlayerHud.razor`.
- `PlayerHud.razor` owns the inventory open/close state and renders:
  - the money HUD
  - the left-side inventory prompt
  - the `PetInventoryPanel`
- The inventory opens with the existing `Score` input action. In the current input config this is bound to TAB.
- Use `Input.Pressed( "Score" )` for the toggle, not a hard-coded TAB key check.
- The HUD prompt uses `Input.GetGlyph( "Score", InputGlyphSize.Medium, true )` so the displayed key follows the current input binding.
- The inventory panel reads pets from `PlayerData.LOCALDATA.inventory`.
- The top row displays equipped pets from `Inventory.GetEquippedSlotIndexes()`.
- The main grid displays valid pet slots from `Inventory.Slots`/`Inventory.GetPet(...)`.
- The inventory UI intentionally does not include pet levels yet.

### Pet Inventory Equip and Unequip API

- `Inventory` now supports:
  - `EquipPet(int slotNumber)`
  - `UnequipPet(int slotNumber)`
  - `TogglePetEquipped(int slotNumber)`
  - `IsPetEquipped(int slotNumber)`
  - `AddPetPrefab(GameObject petPrefab, string displayName = null, PetRarity rarity = PetRarity.Common)`
  - `CanMergePets(IReadOnlyList<int> slotIndexes)`
  - `TryMergePets(IReadOnlyList<int> slotIndexes, out InventoryPetSlot mergedSlot)`
- `Inventory.EquipPet(...)` should avoid duplicate equipped pets and respect `PetFramework.MaxEquippedPets`.
- `Inventory.UnequipPet(...)` removes the slot index from `EquippedSlotIndexes`, then calls `RestoreEquippedPets()` so `PetFramework` remains the source of equipped pet objects.
- UI clicks should go through `Inventory.TogglePetEquipped(...)`, not directly through `PetFramework`.
- Inventory mutations should continue to call the existing save flow through `QueueOwnerSave()` / `PlayerData.QueueSave()`.

### Pet Preview Rendering

- Runtime pet thumbnails are handled by `PetPreviewRenderer` at `Code/UI/PetPreviewRenderer.cs`.
- The Minimal scene has a `PetPreviewRenderer` component on the `Systems` object.
- `PetPreviewRenderer` caches preview render-target textures by `PetPrefabPath`.
- Duplicate inventory slots that reference the same pet prefab path should reuse the same preview texture.
- `PetInventoryPanel` requests previews through `PetPreviewRenderer.Instance.GetPreviewTexture(slot.PetPrefabPath, petPrefab)`.
- While a preview is pending, the UI falls back to the pet's first display-name letter.
- `PetPreviewRenderer.PreviewVersion` is included in the inventory panel build hash so the UI refreshes after previews are generated.
- `PetPreviewRenderer` creates runtime preview objects under `Systems/PetPreviewRendererRuntime`.
- Preview objects are placed far below the playable scene by default using `PreviewOrigin`.
- Preview cameras use `Texture.CreateRenderTarget(...)` and `CameraComponent.RenderTarget`.
- The preview camera uses a dedicated `pet_preview` render tag to isolate preview objects.
- Avoid adding normal `AmbientLight` or `DirectionalLight` components solely for pet previews unless you carefully exclude them from the main scene camera; scene lights can affect the whole scene.

### UI Styling Notes Learned From This Implementation

- s&box Razor may render adjacent text and variables literally in some cases. Prefer explicit expressions in inline text, such as `Coins x@(pet.CoinMultiplierText)` and `Damage @(pet.Damage)`.
- s&box `box-shadow` does not accept the web CSS `inset` keyword. Use regular shadow syntax like `0px 0px 8px rgba(...)`.
- For fixed-size inventory cards, set width/height plus min/max width/height and `flex-shrink: 0` to prevent cards from being compressed by flex layout.
- For centered wrapping card grids, use `justify-content: center` and `align-content: flex-start` on the wrapped scroll container.
- For scrollable panels, `overflow-y: scroll` works and the panel exposes `ScrollOffset`, `ScrollSize`, and `HasScrollY`. The custom pet inventory scrollbar is a themed visual that follows those values.
- If an absolutely positioned `Image` should be centered inside a button/container, make the parent `position: relative` and position the image at `left: 50%; top: 50%; transform: translateX(-50%) translateY(-50%)`.

### Minimal Scene Integration

- Startup scene remains `Assets/scenes/minimal.scene`.
- The scene already has a `Screen` object with `Sandbox.ScreenPanel` and `Sandbox.PlayerHud`.
- Because `PlayerHud` hosts the inventory UI, adding or updating `PlayerHud` is usually enough for screen UI integration.
- The `Systems` object in `minimal.scene` currently hosts the `PetPreviewRenderer` component for pet inventory thumbnails.
- Be careful editing `.scene` files for the same reasons as `.prefab` files: preserve `__type`, `__guid`, `__version`, component arrays, and object structure.
## Current Project Addendum: Pet Crate Shop, Reveal Animation, and World Panel Sign

These notes reflect the current crate shop, reveal animation, and shop display implementation in `E:\Git\Pet-Crate-Simulator`.

### Pet Crate Shop Interaction

- `InteractBuyCrate` lives at `Code/Interactions/InteractBuyCrate.cs`.
- It is the interaction component for the `PetCrateShopObject` objects in `Assets/scenes/minimal.scene`.
- The current scene has three crate shop objects: `PetCrateShopObject`, `PetCrateShopObject (2)`, and `PetCrateShopObject (3)`.
- Each crate object has a child named `Model`; `InteractBuyCrate` finds this child by `CrateModelName` and animates it locally during the reveal.
- Keep `CrateModelName` configurable, defaulting to `"Model"`, instead of hard-coding scene object paths.
- Purchases should execute locally only. Do not convert the buy interaction into an RPC unless the networking model is intentionally changed.
- Purchase flow should use the interacting player's `PlayerData` for money and `Inventory.AddPetPrefab(...)` to grant pets.
- Do not bypass the pet inventory API when giving a purchased pet.
- If a pet cannot be added after money was spent, refund the cost and queue a save.
- The crate reward list is `List<PetCrateReward>` with the editor-facing fields `Rarity`, `SpawnWeight`, and `PetPrefab`.
- Prefer configuring crate reward weights on the nearby `CrateShopDisplay.FeaturedPets` list. `InteractBuyCrate` should use `RewardSource` or resolve the nearby `CrateShopDisplay` so each shop's pet list is configured in one place.
- `RarityParticles` is a separate configurable rarity-to-particle mapping. Keep particles separate from the reward entry so the reward list stays simple.

### Pet Crate Reveal Animation

- The reveal has phases: `Rising`, `Celebrating`, and `Collecting`.
- The cloned reveal pet starts at the crate, rises to `RevealHeight`, does celebration hops/spin/squash/stretch, then does a final collect hop while shrinking before the inventory grant.
- The pet should inherit the spawner's world rotation: `GameObject.WorldRotation * RevealedPetRotation`.
- `RevealedPetRotation` is a local offset from the crate orientation, not an absolute world-facing rotation.
- Keep reveal and crate model animations time-based with `Time.Delta` / `Time.Now` and configurable with `[Property]` values.
- The crate `Model` child currently shakes/lifts/wobbles with squash/stretch rather than tilting backward. The user explicitly preferred a small shake over a tilt-back opening.
- Cache the crate model's base local position/rotation/scale before animating and restore them on cleanup, disable, and destroy.

### Crate Shop World Panel

- `ShopDisplayWorldPanel` in `Assets/scenes/minimal.scene` has `Sandbox.WorldPanel` plus `Sandbox.CrateShopDisplay`.
- `CrateShopDisplay` lives at `Code/UI/CrateShopDisplay.razor`.
- It is a `PanelComponent`, not a child `Panel`.
- It displays `Crate Shop` text, animated shimmer/sparkles/bob, and two random pet previews.
- `CrateShopDisplay` can auto-source pet prefabs from `Scene.GetAll<InteractBuyCrate>()` reward lists when `AutoUseCrateRewards` is true.
- `CrateShopDisplay.FeaturedPets` is a weighted pet list using `InteractBuyCrate.PetCrateReward` entries. Percent text should be computed as `SpawnWeight / totalWeight * 100` and displayed from `0.00%` to `100.00%`.
- The shop display doubles as a pet-index style panel. It checks player stats for whether a pet has ever been received, then shows the full-color pet icon when discovered and a black silhouette when undiscovered.
- Pet thumbnails should be requested through `PetPreviewRenderer.Instance.GetPreviewTexture(prefabPath, prefab)` and include `PetPreviewRenderer.PreviewVersion` in `BuildHash`.
- While preview textures are pending, show the pet's first display-name letter.
- The world panel UI animation currently uses `Time.Now`, an `animationFrame` bucket, and `StateHasChanged()` to refresh at roughly 30 fps.
- When adding or editing world panel UI, keep text large and readable from a distance and validate `minimal.scene` JSON after scene edits.

### Scene Editing Notes

- `minimal.scene` is actively edited by the user. Expect preexisting dirty changes to crate positions, rotations, pet reward lists, prompt text, and child model transforms.
- Do not revert user scene layout edits. Make targeted changes to the relevant component properties only.
- When adding new `[Property]` fields to scene components, update all three `PetCrateShopObject*` instances if the field should be user-configurable in the editor.
- Validate scene changes with a JSON parse and then run `dotnet build E:\Git\Pet-Crate-Simulator\Code\hatch_simulator.csproj -v:minimal`.

### Local Environment Notes

- In one session, `E:\AI\ReferencedFiles\Docs\api` was not present even though this guide references it. If it is missing, use `E:\AI\ReferencedFiles\Docs\dev\doc` and `E:\AI\ReferencedFiles\Repos\sbox-public-master` as the local fallback sources.
- `rg.exe` may fail with access denied on this machine. If that happens, use PowerShell `Get-ChildItem` and `Select-String` as the search fallback.
## Current Project Addendum: Stats Tracking and Analytics

These notes reflect the current verbose player analytics/stat collection implementation in `E:\Git\Pet-Crate-Simulator`.

### Stats Tracking Framework

- Stats are centralized through `Code/PlayerData/GameStatsTracker.cs`.
- Prefer adding new stat events to `GameStatsTracker` instead of scattering raw `Sandbox.Services.Stats.Increment(...)` calls through gameplay code.
- `GameStatsTracker` wraps `Sandbox.Services.Stats.Increment`, `Stats.SetValue`, and `Stats.Flush` with exception handling, normalized stat suffixes, and optional event data dictionaries.
- Stats should use lower-case snake_case names where possible, e.g. `pets_hatched`, `coins_earned`, `doors_unlocked`.
- For per-pet, per-door, per-object, or per-interactable stats, use normalized suffixes from the tracker rather than raw display names. This keeps site stats consistent and avoids spaces/punctuation in stat keys.
- The old direct inventory stat behavior has been preserved conceptually: adding a pet still records pet-specific stats, but now through `GameStatsTracker.RecordPetAdded(...)`.
- Do not call `Stats.FlushAsync()` after every tiny stat. The tracker flushes at important boundaries such as session start/end, crate opened/refunded, object destroyed, and door unlock success/failure. s&box batches stats already.

### Currently Tracked Player Stats

The current implementation records broad player behavior, including:

- Session and playtime: `login_count`, `sessions_started`, `sessions_completed`, `minutes_played`, `playtime_minutes`, `playtime_seconds`, `last_session_seconds`, `current_session_seconds`, `last_session_start_unix`.
- Save/load health: `saves_written`, `save_load_success`, `save_file_missing`, `save_load_failed`.
- Player snapshots: `current_money`, `current_pet_count`, `current_pet_inventory_size`, `current_equipped_pet_count`.
- Money economy: `coins_earned`, `coins_earned_base`, `coins_earned_from_pet_bonus`, `coins_spent`, `coins_spent_crates`, `coins_spent_doors`, `coins_refunded`.
- Pet inventory: `pets_added_total`, `pet_added_{pet}`, `pets_added_{pet}`, `pets_equipped`, `pets_equipped_{pet}`, `pets_unequipped`, `pets_unequipped_{pet}`, `pets_removed`, `pets_removed_{pet}`.
- Pet hatching: `petshatched`, `pets_hatched`, `pets_hatched_{pet}`, `pets_hatched_rarity_{rarity}`, `crate_rewards_{rarity}`.
- Crates: `crate_purchase_attempts`, `crate_purchase_success`, `crate_purchase_failed`, `crate_purchase_failed_{reason}`, `crates_purchased`, `crates_opened`, `crate_refunds`.
- Doors: `door_unlock_attempts`, `door_unlock_failed`, `door_unlock_failed_{reason}`, `doors_unlocked`, `door_unlocked_{door_key}`.
- Object damage/destruction: `objects_hit`, `manual_object_hits`, `pet_object_hits`, `object_damage_dealt`, `manual_damage_dealt`, `pet_damage_dealt`, `object_hits_{object}`, `objects_destroyed`, `objects_destroyed_by_manual`, `objects_destroyed_by_pet`, `destroyed_object_{object}`.
- Interactions: `interactions_used`, `interactions_{interactable_type}`, `interacted_object_{object}`.
- Pet combat: `pet_attacks_landed`, `pet_attacks_{pet}`, `pet_attacks_target_{target}`.

### Gameplay Integration Points

- `PlayerData` owns session-level tracking. It records session start, session end, playtime accumulation, save/load stats, money snapshots, and coin earnings through `AddMoney(...)`.
- `Inventory` owns pet inventory stat events for adding, equipping, unequipping, and removing pets.
- `InteractBuyCrate` records crate attempts, failure reasons, successful purchases, crate opens, refunds, hatches, pet rarity, and crate coin spending.
- `InteractLockedDoor` records door unlock attempts, failure reasons, successful unlocks, and door coin spending.
- `InteractGivePlayerCoin` records object hits, damage, destruction, and whether damage came from manual interaction or pet damage.
- `PlayerInteractionsController` records generic interaction usage immediately before calling `interactable.OnInteract(...)`.
- `PetFramework` records pet attacks when a pet successfully applies damage to an attack target.

### Adding Future Stats

- Add new public methods to `GameStatsTracker` for new gameplay systems instead of using raw stats calls in feature code.
- Include a small data dictionary for useful context when it helps backend inspection, such as `pet`, `pet_key`, `prefab`, `rarity`, `cost`, `reason`, `door_key`, `object`, `source`, `damage`, or `interactable_type`.
- For economy events, track both counts and amounts when useful. Example: count `crates_purchased` and increment `coins_spent_crates` by the cost.
- For failure paths, track a reason-specific stat such as `crate_purchase_failed_inventory_full` or `door_unlock_failed_not_enough_money`.
- Keep high-frequency stat recording reasonable. Per-hit and per-pet-attack stats are acceptable for this project, but avoid flushing every frame or from every update tick.
- When adding a new player action that changes progression, ask: should this affect counts, currency totals, current snapshot values, failure reasons, or per-object/per-pet breakdowns?
- Run `dotnet build E:\Git\Pet-Crate-Simulator\Code\hatch_simulator.csproj -v:minimal` after editing stats code.

### Naming Cautions

- s&box stat names visible on the site are easier to scan when they are stable and consistent.
- Avoid raw display names for new stat keys unless intentionally preserving an existing stat. Use normalized names for new dynamic stats.
- `GameStatsTracker.RecordPetAdded(...)` still increments the raw pet display name as a compatibility stat because the project previously experimented with viewing specific pet adds by display name on the s&box site.
- Do not rename existing stat keys casually after playtests begin. Add new keys if a cleaner stat is needed, and leave old ones in place for historical data continuity.
## Current Project Addendum: Coin Stack Destructible Audio

These notes reflect the current destructible coin stack audio implementation in `E:\Git\Pet-Crate-Simulator`.

### InteractGivePlayerCoin Audio

- Coin stack destructible behavior lives in `Code/Interactions/InteractGivePlayerCoin.cs`.
- `InteractGivePlayerCoin` now has configurable sound lists:
  - `HitSounds`: random sound played on a non-lethal hit.
  - `BreakSounds`: random sound played when the object is destroyed.
- The sound properties are `List<SoundEvent>` values grouped under `Sounds` in the editor.
- Sound playback happens from the authoritative damage path after damage is applied, then calls a broadcast RPC so clients hear the same chosen sound at the destructible's world position.
- Do not play hit/break sounds from `BeginHitFeedback()`; that method runs for client-side visual feedback and can double-play sounds.
- Sounds are played by `Sound.Play(soundPath, position)` in `PlayDestructibleSound(...)`.
- The selected sound path is sent over the RPC rather than the `SoundEvent` object.

### Sound Modulation

- `InteractGivePlayerCoin` also exposes:
  - `SoundPitchRange`, default `0.94,1.08`.
  - `SoundVolumeRange`, currently set on coin stack prefabs to `0.55,0.72` so the coin sounds are quieter.
- The component chooses a random pitch and volume within these ranges each time it plays a hit or break sound.
- Keep pitch/volume modulation subtle; the goal is to avoid repetition without making the coin feedback feel inconsistent or obnoxious.
- If a sound is too loud, prefer lowering the prefab `SoundVolumeRange` first before editing all `.sound` event assets.

### Coin Stack Sound Assets

- Coin stack sound assets live in `Assets/Sounds/CoinStack`.
- Current SoundEvent wrappers are:
  - `coinstack_hit_1.sound`
  - `coinstack_hit_2.sound`
  - `coinstack_hit_3.sound`
  - `coinstack_hit_4.sound`
  - `coinstack_break_1.sound`
  - `coinstack_break_2.sound`
  - `coinstack_break_3.sound`
  - `coinstack_break_4.sound`
- The `.sound` wrappers reference the uploaded `CoinStack_Hit_*.wav` and `CoinStack_Break_*.wav` derived `.vsnd` names.
- If new WAVs are uploaded without `.sound` files, create matching `.sound` SoundEvent wrappers before adding them to prefab `SoundEvent` lists.
- Keep prefab references as asset paths like `sounds/coinstack/coinstack_hit_4.sound`.

### Coin Stack Prefabs

- The current coin stack prefabs are:
  - `Assets/Prefabs/coinstack1.prefab`
  - `Assets/Prefabs/coinstack2.prefab`
  - `Assets/Prefabs/coinstack3.prefab`
  - `Assets/Prefabs/coinstack4.prefab`
- All four prefabs currently include the same four hit sounds and four break sounds.
- All four prefabs currently use `SoundPitchRange: 0.94,1.08` and `SoundVolumeRange: 0.55,0.72`.
- When adding a new hit or break sound, update all four coin stack prefabs unless the user asks for stack-specific sound sets.
- After prefab edits, validate each prefab with a JSON parse and run `dotnet build E:\Git\Pet-Crate-Simulator\Code\hatch_simulator.csproj -v:minimal`.

## Current Project Addendum: Pet Rarity, Merge, and Equipped Visuals

These notes reflect the current rarity and merge implementation in `E:\Git\Pet-Crate-Simulator`.

### Pet Rarity

- Global rarity enum lives at `Code/Pets/PetRarity.cs`.
- Use the global `PetRarity` enum. Do not reintroduce nested rarity enums inside crate, inventory, or UI components.
- Current rarities are:
  - `Common`
  - `Uncommon`
  - `Rare`
  - `Epic`
  - `Legendary`
  - `Mythic`
  - `Ancestral`
- Rarity colors are centralized in `PetRarityExtensions.GetColor()` and `GetCssColor()`.
- Rarity value scaling is powers of two by tier through `PetRarityExtensions.GetValueMultiplier()`:
  - Common: `1x`
  - Uncommon: `2x`
  - Rare: `4x`
  - Epic: `8x`
  - Legendary: `16x`
  - Mythic: `32x`
  - Ancestral: `64x`

### Rarity Stat Scaling Rules

- Rarity stat scaling is applied in `InventoryPetSlot.ApplyRarityToPetComponent(...)`.
- Rarity should currently scale:
  - `CoinMultiplier`
  - `Damage`
  - `AttackRangeMultiplier`
  - `AttackInterval` by dividing interval by the rarity multiplier, clamped to a small safe minimum.
- Rarity should not scale:
  - `MoveSpeedMultiplier`
  - walk animation speed
  - `BobHeightMultiplier`
  - walk animation height
  - `AttackLungeMultiplier`
  - how far forward a pet lunges during attacks
- `AttackLungeMultiplier` remains per-pet prefab tuning on `PetComponent`, and `PetFramework.PetAttackLungeDistance` remains the global base distance.
- If the user asks for rarity to make pets stronger, avoid changing movement feel unless they explicitly request it.

### Equipped Pet Rarity Visuals

- Equipped pet rarity visuals live in `Code/Pets/EquippedPetRarityVisuals.cs`.
- `PetFramework.Equip(InventoryPetSlot inventorySlot)` should be used when equipping saved/inventory pets so rarity data is preserved.
- `PetFramework.Equip(GameObject prefab, PetRarity rarity)` clones the prefab, applies rarity stats, and applies rarity visuals.
- `PetFramework.Equip(GameObject prefab)` should only be used for Common/default behavior.
- Uncommon and higher equipped pets get a world outline in the rarity color.
- Legendary and higher equipped pets also get a spherical particle aura in the rarity color.
- Ancestral visuals may animate through a rainbow/chrome-style color cycle, but the stable rarity color is still centralized in `PetRarityExtensions`.
- The world outline depends on a `Highlight` component on the active scene camera; `EquippedPetRarityVisuals` ensures it exists.

### Pet Merge System

- The merge UI is `Code/UI/PetMergePanel.razor`.
- `PlayerHud.razor` hosts the merge panel and exposes `PlayerHud.OpenPetMerger()`.
- `Code/Interactions/InteractionOpenPetMerger.cs` opens the merge panel from the `MergePetsTogetherInteractable` object.
- Merge selection should use the same inventory slot selection model as equipping: select inventory slots, then let `Inventory` own the actual mutation.
- Valid merge requirements:
  - exactly three selected slots
  - all three slots are valid
  - all three are the same pet prefab path
  - all three are the same rarity
  - the rarity can merge up, meaning it is below `Ancestral`
- `Inventory.TryMergePets(...)` removes the three source pets and creates one pet of the same prefab at the next rarity.
- If one of the source pets was equipped, the merged pet should be equipped after the merge and `RestoreEquippedPets()` should keep `PetFramework` authoritative for spawned pet objects.
- The merge button is green only when the selected pets are merge-valid and red otherwise.
- Merge UI should show rarity-colored borders around pet cards/slots.
- Merge stats should go through `GameStatsTracker.RecordPetMerged(...)`.

### Pet Index and Crate Reward Weights

- `Code/UI/CrateShopDisplay.razor` is the source of truth for shop display reward weights through `FeaturedPets`.
- Each `FeaturedPets` entry is an `InteractBuyCrate.PetCrateReward` with:
  - `Rarity`
  - `SpawnWeight`
  - `PetPrefab`
- Weight selection should add all valid positive weights, roll a random value in that total, then iterate the list subtracting each weight until the rolled value is reached.
- Displayed percentages must reflect each entry's percentage of the total configured weight, not a hand-authored chance field.
- `InteractBuyCrate` should read the reward table from its configured or nearby `CrateShopDisplay` when `UseShopDisplayRewards` is enabled.
- The shop display pet index should check stat keys using `GameStatsTracker.ToStatKeySuffix(...)` so discovered/undiscovered state matches the saved player stats.
- Keep the shop panel animated and readable. It should feel alive but still function as an in-world reference for what can hatch from that shop's crates.

## Current Project Addendum: Pet Inventory Delete Mode and UI Polish

These notes reflect the current pet inventory delete flow and recent UI polish in `E:\Git\Pet-Crate-Simulator`.

### Pet Inventory Delete Mode

- Pet inventory UI lives in `Code/UI/PetInventoryPanel.razor`.
- Pet deletion should use the existing `Inventory.RemovePet(int slotNumber)` API. Do not directly mutate `Inventory.Slots` from UI code.
- The inventory panel has a trash toggle beside the pet count. It uses the texture asset:
  - `Assets/ui/icons/icon_trashcan.vtex`
  - UI path: `ui/icons/icon_trashcan.vtex`
- Delete mode is intentionally sticky. After deleting one pet, it should remain enabled so the player can delete multiple pets until they press the trash toggle again.
- Delete mode click flow:
  - First click on a pet card marks it as pending deletion and changes overlay text to `Are you sure?`.
  - Second click on the same card starts the delete animation.
  - Clicking a different card while in delete mode moves the confirmation to that card.
- Delete mode visuals:
  - Pet cards fade toward white.
  - Cards shake subtly, not violently.
  - Hovering a pet card shows a transparent red background and `Delete` overlay.
  - The deleting card jumps, shrinks, spins, fades, then collapses its layout width/height before `Inventory.RemovePet(...)` runs.
- Because the inventory list compacts after removal, always reset per-card inline visual properties such as `background-color`, `opacity`, and `transform` in `GetPetCardStyle(...)`. s&box UI may reuse visual elements after list shifts, so stale inline styles can make the card that moves into the deleted slot stay white or transparent.
- The card collapse phase exists to make the grid reflow less abrupt before the item is removed from the inventory list.

### Pet Inventory UI Styling

- The pet inventory close button should be positioned relative to the inventory window, not nested inside the title bar for positioning.
- The close button is aligned with the title bar's vertical center and placed on the right edge of the inventory window.
- The pet inventory close button grows on hover and shrinks back on mouse leave using CSS `transform: scale(...)`.
- The trash/delete toggle also grows on hover and shrinks back on mouse leave.
- The trash toggle and custom scrollbar outlines should use one solid border color. Avoid layered shadows or mixed border colors that make their outlines look like gradients.
- `image-tint` rejected `#d92d3d` in this project. Avoid using `image-tint` for the trash icon unless validating the exact accepted syntax first. Use opacity/scale or a prepared tinted texture instead.
- The custom scrollbar in `PetInventoryPanel.razor` is visual-only and follows `PetGridPanel.ScrollOffset`, `ScrollSize`, and `HasScrollY`.

### Pet Merge UI Styling and Animation

- Pet merge UI lives in `Code/UI/PetMergePanel.razor`.
- The merge panel title currently says `Pet Forge`.
- The pet merge close button should match the pet inventory close button behavior:
  - positioned relative to the merge window
  - aligned with the title bar's vertical center
  - placed on the right edge of the merge window
  - grows on hover and shrinks back on mouse leave
- Merge animation currently uses these phases:
  - left and right pets float upward for about one second
  - left and right pets quickly converge into the center pet
  - all three pet visuals fade to black
  - a centered burst of particles plays
  - selected pets are consumed/cleared when they collapse into the center
  - empty slot frames move back to their original positions
- After merge slots clear, explicitly reset their border color/style to the default empty-slot border. s&box UI can reuse the same visual element and otherwise leave a previous rarity border behind.
- Pet Forge pet cards use a wrapper/inner-card split so list sort/reset transforms and hover-grow transforms do not fight each other.
- The Forge card wrapper should explicitly reset `opacity: 1` and `transform: translateX(0px) translateY(0px) scale(1)` after sort/auto-merge animations. This avoids recycled UI elements staying slightly transparent after the inventory list compacts.
- Filled merge slots are clickable and should remove that selected pet from the merge slots while not actively merging.
- Forge hover overlays intentionally mirror the pet inventory overlay style:
  - Green transparent `Add` overlay when a pet can be added.
  - Red transparent `Remove` overlay when hovering a selected pet or filled merge slot.
  - Red transparent `Max merge pets selected` overlay when all three merge slots are full.
- Keep Forge overlay text smaller than inventory overlay text because Forge cards are smaller.
- Auto merge lives in `Inventory.TryAutoMergePets(...)` and `Inventory.CanAutoMergePets()`.
- Auto merge groups valid inventory pets by `PetPrefabPath` and `Rarity`, skips max rarity pets, and repeatedly merges the first available three sorted by rarity then display name.
- The Pet Forge `Auto Merge` button should clear selected slots and start a short sort animation after merge attempts, but it should not toggle or close the merge UI.

## Current Project Addendum: Credits Board World Panel

These notes reflect the current Credits Board implementation in `E:\Git\Pet-Crate-Simulator`.

### Credits Board Components

- The scene object is named `Credits Board` in `Assets/scenes/minimal.scene`.
- The UI component is `Code/UI/CreditsBoardPanel.razor`.
- The scene object has:
  - `Sandbox.WorldPanel`
  - `Sandbox.CreditsBoardPanel`
- `CreditsBoardPanel` exposes:
  - `TitleText`
  - `SubtitleText`
  - `Contributors`
- Each contributor exposes:
  - `SteamId`
  - `Contributions` as a list of strings
- Contributor display names are not manually configured. The panel resolves the Steam name from `new Friend((ulong)SteamId).Name`.
- Contributor avatars are loaded with `Texture.LoadAvatar(SteamId, size)`.
- If a Steam ID is missing or cannot resolve, the panel displays a neutral fallback such as `Steam User` or `Steam User {SteamId}`.

### Credits Board Interaction

- A `Sandbox.WorldPanel` only renders world UI. It does not route clicks by itself.
- World panel click/hover input requires a `Sandbox.WorldInput` component somewhere in the scene.
- `minimal.scene` currently has `Sandbox.WorldInput` on the main scene `Camera` object.
- `WorldInput` uses the object's forward ray when the mouse is not active, so the player can aim at a world panel and press the configured left mouse action.
- Current `WorldInput` configuration:
  - `LeftMouseAction`: `Attack1`
  - `RightMouseAction`: `Attack2`
- The Credits Board `WorldPanel.InteractionRange` is set to `2000` so the board can be clicked from a comfortable reading distance.
- If a future world panel renders but cannot be clicked, first check whether a `Sandbox.WorldInput` exists and is enabled before changing Razor click handlers.
- Decorative layers in interactive world panels should use `pointer-events: none` so they do not intercept clicks.
- For world-panel list items, `onmousedown` can feel more reliable than only `onclick` because world panels may move/animate and the press/release sequence can be sensitive.

### Credits Board UI and Animation

- Credits Board has a contributor list view and a detail view.
- Clicking a contributor switches to the detail view and shows the configured contribution lines for that Steam ID.
- The back button returns to the contributor list.
- Selection stores the selected contributor object directly, not just an index. This avoids losing the selected target if the contributor list refreshes.
- The board uses animated shimmer, spark, and translucent drifting rectangle decorations similar to `WipDisclaimerPanel`.
- The drift rectangle style is generated in Razor with `BuildDriftRectStyle(...)`, using `Time.Now`, an `animationFrame` bucket, and `StateHasChanged()` to refresh motion.
- Decorative motion should stay behind content with low opacity and must not reduce readability or interfere with pointer events.

## Current Project Addendum: Background Music Framework

These notes reflect the current background music implementation in `E:\Git\Pet-Crate-Simulator`.

### Music Components

- Background music is now controlled by code, not by active `Sandbox.SoundBoxComponent` playback.
- The central controller is `Code/Audio/BackgroundMusicController.cs`.
- Zone triggers are handled by `Code/Audio/MusicZoneTrigger.cs`.
- `BackgroundMusicController` exposes:
  - `DefaultSound`
  - `TargetSound`
  - `FadeDuration`
  - `MasterVolume`
  - `PlayDefaultOnStart`
  - `Force2d`
- `MusicZoneTrigger` exposes:
  - `Music`
  - `Controller`
  - `Priority`
  - `Volume`
  - `ClearMusicWhenExited`

### Current Scene Setup

- Existing objects named `Music` and `MusicZone1` in `Assets/scenes/minimal.scene` were converted from direct SoundBox playback to trigger-driven music zones.
- Their old `Sandbox.SoundBoxComponent` components are intentionally left in the scene but disabled. Do not re-enable them unless the user explicitly wants the old behavior back.
- The old SoundBox scale values were reused as `Sandbox.BoxCollider` trigger sizes, with `IsTrigger` enabled.
- The `Music` object currently hosts the scene's `BackgroundMusicController`.
- The `MusicZoneTrigger` components on the music zone objects specify which `SoundEvent` should become the target music when the local player enters the trigger.

### Music Behavior

- Music transitions are local-client driven. Only the local non-proxy player entering a trigger requests music.
- `BackgroundMusicController` keeps one current music layer and one fading layer, similar to a two-emitter crossfade pattern.
- When `TargetSound` changes, the current track fades out while the new track fades in at the same time.
- The default transition duration is `2` seconds through `FadeDuration`.
- The controller compares sounds by `SoundEvent.ResourcePath` when possible, so requesting the same track repeatedly does not restart it.
- `MasterVolume` is used to preserve the intended project music loudness. The current scene controller uses a quieter value matching the previous SoundBox volume.
- `Force2d` makes background music play as local non-positional audio by setting the `SoundHandle` to local listening, no distance attenuation, no occlusion, no air absorption, and no transmission.

### Trigger and Priority Rules

- `MusicZoneTrigger` implements `Component.ITriggerListener`.
- Trigger events may fire from the player's child colliders, so `MusicZoneTrigger` resolves `PlayerController` with `FindMode.InAncestors`.
- It ignores proxy players so every client controls only their own music.
- It counts overlapping trigger touches per local player object because the player has multiple colliders.
- If multiple music zones overlap, `BackgroundMusicController` chooses the highest `Priority`; ties use the newest request.
- When a zone exits and `ClearMusicWhenExited` is true, that zone clears its request and the controller falls back to the next best active request or `DefaultSound`.

### Future Music Work

- For new areas, add a trigger collider and `MusicZoneTrigger`, then set the `Music` property to a `.sound` asset.
- Prefer changing `MusicZoneTrigger.Music`, `Priority`, `Volume`, or the controller's `FadeDuration`/`MasterVolume` rather than adding new SoundBoxes.
- If a zone has no collider, `MusicZoneTrigger` creates a default `BoxCollider` trigger on start, but hand-authored scene trigger sizes are preferred for real zones.
- Keep background tracks as looping `.sound` assets. The controller starts and stops `SoundHandle`s; it does not edit the `.sound` files themselves.
- After editing music code or scene music components, run:
  - `dotnet build E:\Git\Pet-Crate-Simulator\Code\hatch_simulator.csproj -v:minimal`

## Current Project Addendum: Player Trading System

These notes reflect the current player trading implementation in `E:\Git\Pet-Crate-Simulator`.

### Trading Files and Scene Integration

- The main trading controller lives at `Code/Trading/PlayerTradeController.cs`.
- The player details interaction lives at `Code/Interactions/InteractPlayerDetails.cs`.
- Player-to-player interaction support uses `Code/Interactions/PlayerInteractionsController.cs`.
- The trade UI lives at `Code/UI/PetTradePanel.razor`.
- The target player details UI lives at `Code/UI/PlayerDetailsPanel.razor`.
- The trade invite popup lives at `Code/UI/TradeInviteToast.razor`.
- `PlayerHud.razor` hosts `PlayerDetailsPanel`, `TradeInviteToast`, and `PetTradePanel`.
- The player prefab at `Assets/Prefabs/player.prefab` should include `PlayerTradeController` and `InteractPlayerDetails`.

### Trading Flow

- Players interact with another player through the existing interaction framework.
- `InteractPlayerDetails` opens a details panel for the local player and shows the target player's Steam name, avatar, and selected stats.
- Pressing the trade button calls `PlayerTradeController.SendTradeInvite(...)`.
- The target receives the invite through an owner RPC and sees `TradeInviteToast`.
- The invite uses the `Reload` input action and its glyph, not a hard-coded key.
- Invites expire after `PlayerTradeController.InviteLifetimeSeconds`, currently 5 seconds.
- Once accepted, both players open `PetTradePanel`.
- Each client owns its own selected slot indexes and sends serialized inventory/offer snapshots to the partner.
- Both players must submit their offer before review mode begins.
- In review mode, both players must accept. When both have accepted, a countdown begins.
- Either player can cancel/unaccept during the countdown, which resets the countdown.
- Trade completion should mutate inventory through the existing `Inventory` APIs and then save through the existing player data save flow.

### Trading Networking Rules

- `PlayerTradeController` is local-owner driven. Do not make UI code directly mutate the other player's local inventory.
- Use `Rpc.Owner` for trade invites, accepted notices, trade state sync, and cancellation messages that should only be delivered to the owning client.
- Keep the authoritative local state for each side on that side's own `PlayerTradeController`.
- Partner inventory and partner offer data are serialized snapshots, not live references to the other player's inventory slots.
- Do not rely on shared slot indexes across players. A selected slot index is meaningful only for the player who owns that inventory.
- Be careful with selected visual state in the trade UI: local selected pets and partner selected pets must be highlighted from separate data sources.

### Trade UI and Pet Rendering Notes

- `PetTradePanel.razor` should render trade pets with the same card language as `PetInventoryPanel.razor`: preview image, display name, rarity text, coin multiplier, damage, and rarity-colored border.
- Trade pet previews should use `PetPreviewRenderer.Instance.GetPreviewTexture(...)` through the view models built by `PlayerTradeController`.
- Include `PetPreviewRenderer.PreviewVersion` in trade UI build hashes so previews refresh after render targets finish.
- The trade menu has two main phases:
  - offer selection
  - final review/accept countdown
- Do not render offered pets twice in review mode. Use the dedicated review render path for the final offer display.
- Do not mark both review sides ready unless the corresponding `OwnAccepted` or `PartnerAccepted` flag is actually true.
- Do not let one player's selected-slot state color the other player's inventory cards. Partner selected cards should use the partner offer snapshot.
- Child elements inside pet cards should not steal hover from the card. Use `pointer-events: none` on card child visuals/text when hover overlays or hover scaling are expected.
- Top padding/margins are important in scrollable trade lists because hover scaling can clip cards at the top edge.
- Selected offer rows and inventory grids should have a visible divider so users can tell selected trade pets apart from the remaining inventory.

### Trade Inventory Capacity and Safety

- Before accepting a trade, check that the player can receive the partner's offered pets after removing their own offered pets.
- The current helper for this is `PlayerTradeController.CanReceivePartnerOffer()`.
- Cancelling a trade should reset local trade state and notify the partner through the existing owner RPC.
- If an inventory slot is invalid or missing during trade rendering, skip it instead of crashing the Razor render tree.
- Keep trade UI render methods null-safe; Razor crashes in trade panels can break both players' ability to finish or cancel the trade cleanly.

## Current Project Addendum: PetFramework Performance and Per-Frame Cost

These notes reflect the current performance optimization of `Code/Pets/PetFramework.cs` in `E:\Git\Pet-Crate-Simulator`. They were driven by a playtest where clients reported low FPS while the host ran fine.

### Diagnosing Low FPS

- s&box RPCs are fire-and-forget. Calling a `[Rpc.Broadcast]`/`[Rpc.Owner]`/`[Rpc.Host]` method sends the network message asynchronously and then runs the body locally; it does NOT block or "halt" the frame waiting for receipt. This is confirmed in engine source at `Sandbox.Engine\Scene\Networking\Rpc.InstanceRpc.cs` (`OnCallInstanceRpc`).
- A per-frame RPC therefore does not stall a frame, but it does flood the network and incur per-call serialization cost.
- Host-fine / clients-laggy is usually caused by per-frame work that scales with player count and runs on proxy objects, not by RPC blocking. Look there first.

### PetFramework Per-Frame Rules

- `PetFramework.OnUpdate` runs for every player on every client, including proxies. Keep its per-frame cost low.
- Do not walk the GameObject hierarchy every frame. `RefreshEquippedPets()` / `DiscoverEquippedPets()` perform a full `Components.GetAll<PetComponent>(...)` walk and must stay throttled by `PetRefreshInterval` (default `0.25f`) through `TickRefresh()`.
- `TickRefresh()` runs a cheap `PruneInvalidPets()` every frame (drops destroyed pets from the small equipped list, no hierarchy walk) and only runs the full `RefreshEquippedPets()` on the interval.
- Do not scan the whole scene every frame. `FindNearestAutoBattleTarget()` calls `Scene.GetAllComponents<InteractGivePlayerCoin>()` and must stay throttled by `AutoBattleScanInterval` (default `0.25f`) inside `TryAcquireAutoBattleTarget()`.
- Do not force a hierarchy refresh on hot paths. The `CoinMultiplier` getter must not call `RefreshEquippedPets()`, because `PlayerData.AddMoney(...)` reads it on every coin hit. The equipped-pet list is already maintained on equip/unequip plus the throttled refresh.
- Because refresh no longer self-heals every frame, the per-frame update loops (`UpdateEquippedPets`, `UpdateEquippedPetVisuals`) must null-guard each slot's pet (`if ( !slot.Pet.IsValid() ) continue;`).

### Tuning and Future Pet Performance Work

- `PetRefreshInterval` and `AutoBattleScanInterval` are editor `[Property]` values. Raise them to reduce per-frame cost further; lower them toward `0` to restore near-instant responsiveness at higher cost.
- When adding new per-frame pet behavior, prefer cached state updated on events (equip/unequip/spawn) or on a throttled interval over per-frame hierarchy walks, scene scans, or LINQ allocations.
- The `EquippedPets` property allocates a new list via LINQ on each access; avoid calling it from per-frame code.
- Remaining smaller per-frame paths to consider if FPS is still poor: the proxy pet movement/animation loop (`UpdateEquippedPetVisuals`, runs per pet on every client) and the per-frame `Scene.Trace` in `PlayerInteractionsController` (local player only).
- After editing pet performance code, run `dotnet build E:\Git\Pet-Crate-Simulator\Code\hatch_simulator.csproj -v:minimal`.

## Current Project Addendum: s&box Gotchas Learned The Hard Way

These cross-cutting lessons cost real debugging time. Read them before touching UI, audio, or networking.

### The gamemode API sandbox (MOST IMPORTANT)

- s&box runs gamemode code inside an API access whitelist that `dotnet build` does NOT enforce. A call to a non-whitelisted engine API can compile cleanly via the documented build command but make s&box reject the ENTIRE `hatch_simulator` assembly at load time.
- When the whole assembly fails to load, every game component dies at once. The symptom looks unrelated: e.g. "TAB stopped opening the inventory", "the whole HUD is dead". A successful `dotnet build` is necessary but NOT sufficient proof the game will load.
- Concrete example from this project: accessing `Sandbox.Audio.Mixer` (`Mixer.FindMixerByName`, `Mixer.Mute`, `SoundHandle.TargetMixer`) from game code broke everything. Removing it fixed it. Mute is now done via already-used APIs instead (see audio addendum).
- Rule of thumb: prefer engine APIs already used elsewhere in this codebase. If you introduce a never-before-used engine type and the game suddenly goes dead, suspect a whitelist violation first, and check the s&box console for the real error.
- A separate confusing symptom: s&box hot-reload can get stuck, making a clean change look broken. A full editor restart / rebuild clears it. Don't assume a logically-inert change is the cause.

### Cursor visibility vs. mouse-look

- This game uses crosshair aiming (`Sandbox.PlayerController.UseLookControls = true`, a `.Crosshair` reticle, camera-forward interaction).
- `Mouse.Visibility` defaults to `Auto`: the cursor appears whenever any screen panel has an interactive (`pointer-events: all`) child. While the cursor is visible, `Input.AnalogLook` is forced to zero, so mouse-look is disabled (confirmed in engine `Input.cs`).
- Therefore an always-interactive HUD element would pin the cursor on permanently and break the camera. Clickable HUD UI must be `pointer-events: all` ONLY while the cursor is already up (i.e. a menu is open). Gate interactivity (or simply only render the element) on the HUD's menu-open state, NOT on the live `Input.MouseCursorVisible` flag — gating on the live flag creates a feedback loop that keeps the cursor on forever.

### World panel input (`WorldPanel` + `WorldInput`)

- A `WorldPanel` only renders; click/hover routing needs a `Sandbox.WorldInput` component (this project keeps one on the main `Camera`, `LeftMouseAction = Attack1`). It uses the object's forward ray when the mouse is inactive, so players aim with the crosshair.
- `WorldInput.Hovered` is the currently-hovered panel (null when not aiming at any interactive world UI). World panels DO fire `onmouseover`/`onmouseout` and toggle the `:hover` pseudo-class, so hover effects work in world space.
- Do NOT put `pointer-events: all` on a world panel's ROOT — that makes the whole panel a hover/click target (e.g. the credits board showed the hand cursor over its entire surface). Put `pointer-events: all` only on the actual interactive elements, and `pointer-events: none` on decorative layers.

### CSS quirks in s&box

- `z-index` only orders siblings within the same stacking context. You CANNOT push a HUD element behind a separate menu panel with `z-index` alone. To keep an overlay off menus, gate its visibility on menu-open state instead.
- Inline `style="transform: ..."` overrides a stylesheet `:hover { transform: ... }`. For a hover-scale/outline on an element that ALSO has a per-frame animated transform (e.g. a bobbing card), use a wrapper/inner split: hover transform on the wrapper, animated transform on the inner element. (Same trick the Pet Forge cards use.)
- `outline` is supported and respects `border-radius`; it draws outside the border without affecting layout — good for hover highlights. To fade it in without width-jitter, keep `outline-width` constant and animate `outline-color` from transparent to the target color.
- s&box supports `box-shadow` with spread (4th value) and the `cursor` property, but NOT the web `inset` keyword.
- Render UI images from project assets with `<img src="ui/yourtexture.vtex">` (same as the money icon). Use `.vtex`, not `.png`, or it can fail for non-editor clients.
- Boolean component attributes in Razor MUST use a `@` expression (`Multiline=@(true)`), never a quoted literal (`Multiline="true"`). `dotnet build` accepts the quoted form, but s&box's Razor codegen emits a string-to-bool assignment and fails with "Cannot implicitly convert type 'string' to 'bool'". This is a concrete case where `dotnet build` is green but s&box's compile fails — see below for how to read those errors.

### Reading s&box compile errors (when `dotnet build` is green but the game is broken)

- s&box's own compile output is in `C:\Program Files (x86)\Steam\steamapps\common\sbox\logs\sbox-dev.log`. Search it for `Compile of 'topgamestudio.hatch_simulator' Failed` and the `[Generic] Error |` lines just under it (they include the file:line, e.g. a generated `_gen_*.razor_*.cs`).
- A failed main-assembly compile cascades: the `.editor` assembly then reports `Broken Reference ... (the compiler failed)`. Fix the real error in the main assembly and both clear.
- The log also contains many unrelated pre-existing errors (texture/material compile `[FAIL]`, Unity-style `.meta` JSON parse exceptions). Don't chase those; look for the `[Generic] Error |` lines from the C# compile.

### Local interaction pattern

- `Interactable.OnInteract` is `[Rpc.Broadcast]` on the base, but an override that OMITS the attribute runs LOCALLY (s&box only wraps the declaration that carries the attribute). `InteractBuyCrate`, `InteractBuyAllCrates`, etc. rely on this for local-only purchase flows. Keep purchase/interaction logic local unless you intentionally want it networked.

### Build command on Windows

- Use the documented build with a quoted/forward-slash path so the shell doesn't eat backslashes: `dotnet build "E:/Git/Pet-Crate-Simulator/Code/hatch_simulator.csproj" -v:minimal`.
- Pre-existing warnings appear in unrelated files (DunGen, AreaSpawner, randomrotator, Destructable, etc.). Treat only NEW errors/warnings in touched files as blockers.
- These are CLIENT/visual features. A clean build does not prove they work — say so, and ask for an in-game check, especially for world-panel sizing, cursor behavior, and hover.

## Current Project Addendum: Audio Mute Options

These notes reflect the music/sound mute feature.

- Client-local mute state + persistence live in `Code/Audio/AudioSettings.cs` (static class). It persists `MusicMuted` / `SoundMuted` to `settings/audio.json` via `FileSystem.Data` + `JSONObject`, and exposes a `Version` counter for UI build hashes.
- Muting is applied where audio is produced (NOT via engine mixers — see the sandbox gotcha):
  - Music: `BackgroundMusicController.GetTargetVolume()` returns `0` when `AudioSettings.MusicMuted`.
  - Sound: `InteractGivePlayerCoin.PlayDestructibleSound()` early-returns when `AudioSettings.SoundMuted`. This is currently the only gameplay `Sound.Play`; if you add more SFX, gate them on `AudioSettings.SoundMuted` too.
- The toggle buttons are Music/Sound chips in `Code/UI/PlayerHud.razor`, rendered only inside the `@if ( isPetInventoryOpen )` block (so they're only present/clickable while the inventory menu is open, which is also when the cursor is up).

## Current Project Addendum: HUD World-Aim Feedback (Hand Cursor + Prompt)

These notes reflect the cursor/prompt system in `Code/UI/PlayerHud.razor` and `Code/Interactions/PlayerInteractionsController.cs`.

- `PlayerInteractionsController` (on the player) raycasts from the camera each frame and exposes: a static `Local`, `IsHoveringInteractable` (aiming at a valid `Interactable`), and `HoverPromptText` (that interactable's `text`). The raycast lives in `FindAimedInteractable()`.
- It also re-enables the world-space prompt at the object: `InteractionFrameworkUI` (`Prefabs/UI/interactionsenginepanel.prefab`) now shows ONLY the interact key icon (text was removed). It's enabled/positioned at the hovered object and billboards to camera.
- `PlayerHud` swaps the center `.Crosshair` for a hand image (`ui/hand.vtex`) when aiming at interactive UI/objects, and shows a centered text pill (`.InteractPrompt`) just below the cursor:
  - Hand shows when `IsHoveringWorldUi` (`WorldInput.Hovered != null`) OR `PlayerInteractionsController.Local.IsHoveringInteractable`.
  - Prompt text = the interactable's text, or `CrateShopDisplay.HoverPromptText` for shop cards.
  - BOTH are suppressed by `IsAnyMenuOpen` (inventory/merge/trade/details/ban/notice) so they never paint over menus.
- `PlayerHud` finds the `WorldInput` via `Scene.GetAllComponents<WorldInput>()` (cached). Net result: icon on the object (world space), text + hand at the cursor (screen space).

## Current Project Addendum: Crate Shop Pet Toggle, Hover, and Buy-All

These notes reflect interactivity added to the crate shop.

### Auto-trash toggle on the shop display
- `Code/UI/CrateShopDisplay.razor` pet cards are clickable. Left-click (`onmousedown`) toggles a per-player, client-local "auto-trash" flag for that pet, keyed by prefab path: static `trashedPetPaths` + `IsPetTrashed` / `SetPetTrashed` / `TrashVersion`. It's global across all shop displays and session-only (not persisted yet).
- When a crate would grant a flagged pet, `InteractBuyCrate.FinishReveal()` discards it instead of adding to inventory (money still spent, no refund). Both files derive the prefab path identically so the keys match.
- Trashed cards show a red border + a translucent red overlay with the trashcan icon (`ui/icons/icon_trashcan.vtex`) and "Auto-Trash" text.

### Card hover feedback
- Cards use a wrapper/inner split: `.pet-index-card-wrapper:hover` scales the card (`transform: scale`) and reveals a yellow outline (`outline-color: #ffd66f`, the shop border color) on the inner card. The inner card keeps its per-frame bob/tilt transform so the two don't fight.
- Hovering a card sets a shared static `CrateShopDisplay.HoverPromptText` (to `HoverActionLabel`, default "Toggle") via `onmouseover`/`onmouseout`, which `PlayerHud` shows below the cursor.

### Buy All Crates
- `Code/Interactions/InteractBuyAllCrates.cs` is an `Interactable` for a "Buy All Crates button". On use it finds every `InteractBuyCrate` in the same shop and calls each one's `OnInteract` (local). It scopes to the button's parent subtree by default (walking up if empty), or an explicit `ShopRoot` property — important because the scene has multiple shops. The button needs a collider to be aimed at.

## Current Project Addendum: Player Notices and Nameplates

### Player notice system (configured like the ban system)
- `Code/Networking/PlayerNoticeController.cs` shows a one-time, click-to-dismiss message to players whose SteamId is listed in `NoticePlayers` (`SteamId` + `Message`), mirroring `BanListController`'s configuration but with no kick.
- Each client checks its own `Game.SteamId` locally (like `CheckLocalBan`) and shows `Code/UI/PlayerNoticePanel.razor` (a full-screen modal with an OK button). `PlayerHud` hosts the panel; `NoticeVersion` drives the rebuild. Add the `PlayerNoticeController` component to a scene object (e.g. the Ban Manager) to configure it.

### Player nameplates (remote players only)
- `Code/UI/PlayerNameplate.razor` is a world-space `PanelComponent` (use with a `WorldPanel`) placed as a child above the head in `Assets/Prefabs/player.prefab`.
- It resolves the ancestor `PlayerData`, renders ONLY for proxies (`Player.IsProxy`, i.e. other players), shows the Steam name via `Player.GameObject.Network.Owner.DisplayName`, and billboards to camera (same pattern as `DestructableNameplate`).
- `WorldPanel.RenderScale` needs visual tuning (default 512px panel is large).

## Current Project Addendum: Credits Board Two-Column Layout

- `Code/UI/CreditsBoardPanel.razor`'s contributor list (`.credits-list`) is a wrapping row (`flex-wrap: wrap`, fixed-width `.credits-user-shell`, `flex-shrink: 0`) to show contributors in two columns. The board has a fixed height with `overflow: hidden`, so a very long list will eventually clip — make the list scrollable (like the detail view's contribution list) if it outgrows two columns.

