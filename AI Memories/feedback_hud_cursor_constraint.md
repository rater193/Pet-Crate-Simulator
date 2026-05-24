---
name: feedback-hud-cursor-constraint
description: "Clickable screen-UI must only be interactive while the cursor is already up, or it breaks mouse-look during gameplay"
metadata: 
  node_type: memory
  type: feedback
  originSessionId: 07ea938f-16d5-4065-9054-31a3a162e75c
---

When adding clickable screen UI (HUD buttons, overlays) to this project, make the interactive elements `pointer-events: all` ONLY while the cursor is already visible (i.e. a menu/inventory is open). Otherwise keep them `pointer-events: none`.

**Why:** This game uses crosshair aiming with `Sandbox.PlayerController.UseLookControls = true`. In s&box, `Mouse.Visibility` defaults to `Auto`, which shows the cursor whenever any screen panel has interactive (`pointer-events: all`) children — and while the cursor is visible, `Input.AnalogLook` is forced to zero, disabling mouse-look. So an always-interactive HUD element would pin the cursor on and break the camera during normal play. (This is why the inventory only renders its clickable content while open.)

**How to apply:** Gate interactivity (or simply only render) on the HUD's menu-open state (e.g. `@if (isPetInventoryOpen)`) rather than on `Input.MouseCursorVisible` directly — gating on the live cursor flag creates a feedback loop that keeps the cursor on forever. Example: the Music/Sound mute buttons in `Code/UI/PlayerHud.razor` are rendered only inside the `@if (isPetInventoryOpen)` block. (Full-screen modals like menus/notices intentionally use `pointer-events: all` so the Auto cursor appears for clicking — that's the correct flip side of this rule.)
