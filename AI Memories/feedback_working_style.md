---
name: feedback-working-style
description: "User wants running commentary on your reasoning while you work, not just a final summary"
metadata: 
  node_type: memory
  type: feedback
  originSessionId: 07ea938f-16d5-4065-9054-31a3a162e75c
---

When working on a task, narrate your thoughts and decisions as you go — what you're checking, why, and what you conclude at each step.

**Why:** The user explicitly asked to "keep me updated with your thoughts as you work on this process." This also matches the project's own `Instructions_PetSim.md` guidance ("Explain what you are checking and why while you work, especially when reading files, touching prefabs, or changing networking").

**How to apply:** Give short progress updates at each meaningful step (investigation findings, direction changes, before each edit). Don't batch everything into one end-of-task summary. Still keep individual updates tight — one or two sentences each.

**Docs workflow:** The user treats `Instructions_PetSim.md` (repo root) as the canonical, per-system documentation and regularly asks to update it. When you build a feature or discover a gotcha, add/update a "Current Project Addendum" section there — they expect it kept current. Sessions also start by being told to follow that file.

**Verification honesty:** This is an s&box game; you can't run the client. After UI/feature work, run the build for code validation but say plainly that you can't verify in-game and ask the user to reload/test. They're fine reloading and reporting back — see [[feedback-sbox-build-gotchas]].
