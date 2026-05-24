---
name: feedback-sbox-build-gotchas
description: A clean dotnet build does NOT prove s&box compiles/loads; check sbox-dev.log; known dotnet-green/s&box-fail divergences
metadata: 
  node_type: memory
  type: feedback
  originSessionId: 07ea938f-16d5-4065-9054-31a3a162e75c
---

`dotnet build E:\Git\Pet-Crate-Simulator\Code\hatch_simulator.csproj -v:minimal` being green is NECESSARY but NOT SUFFICIENT. s&box has its own Razor codegen + a gamemode API sandbox that `dotnet build` does not enforce, so code can build clean here yet fail to compile/load in the editor.

**Where the real errors are:** `C:\Program Files (x86)\Steam\steamapps\common\sbox\logs\sbox-dev.log`. Search for `Compile of 'topgamestudio.hatch_simulator' Failed` and the `[Generic] Error |` lines under it (they cite the generated `_gen_*.razor_*.cs` file + line). Ignore the many unrelated asset/texture `[FAIL]` and Unity-style `.meta` JSON errors.

**Tell-tale symptom:** if the WHOLE HUD/input goes dead (e.g. TAB stops opening the inventory, can't interact with anything), it's almost always a failed assembly compile/load — not a logic bug. The whole game DLL didn't load.

**Known dotnet-green / s&box-fail divergences (all hit this project):**
- Non-whitelisted engine API from gamemode code breaks the assembly load. Confirmed with `Sandbox.Audio.Mixer` (FindMixerByName/Mute/SoundHandle.TargetMixer). Prefer APIs already used in the codebase; e.g. mute audio via `SoundHandle.Volume`/`BackgroundMusicController.GetTargetVolume`, not mixers.
- A `record` declared in a Razor `@code` block → cascading "type or namespace could not be found" for nested types. Use a `class`/`enum`.
- A literal char immediately before `@Member.Access` renders LITERALLY: write `x@(Info.CoinText)`, not `x@Info.CoinText`.
- A quoted bool attribute (`Multiline="true"`) → "cannot implicitly convert string to bool". Use `Multiline=@(true)`.

**Mid-edit transients:** s&box recompiles on EVERY file save, so a multi-step restructure (open a container in one edit, close it in the next) can leave a transient `} expected` failure in the log. After finishing edits, reload the editor so the FINAL state recompiles; a balanced div/brace count + clean `dotnet build` means the current file is fine even if the log shows an older failure. If hot-reload seems stuck, do a full editor restart.

This project documents systems + these gotchas in `Instructions_PetSim.md` (see [[feedback-working-style]]).
