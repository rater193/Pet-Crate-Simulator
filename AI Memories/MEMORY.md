# Memory Index

- [Working style](feedback_working_style.md) — narrate reasoning as you work; keep Instructions_PetSim.md updated; can't run the client, so build + ask user to test
- [s&box build gotchas](feedback_sbox_build_gotchas.md) — dotnet build green ≠ s&box compiles; check sbox-dev.log; known razor/sandbox divergences
- [HUD cursor constraint](feedback_hud_cursor_constraint.md) — clickable screen UI must be interactive only when the cursor is up, or it breaks mouse-look
- [Perf debugging heuristic](project_playtest_perf.md) — host-fine/clients-laggy is per-frame proxy work that scales with players, not RPC blocking
