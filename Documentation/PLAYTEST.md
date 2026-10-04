# Operation completion checklist

This checklist describes the current local Windows build. Automated checks complement, but do not replace, human playtesting.

## Stability and high-DPI verification / 2026-09-08

- `Logs/stability-core-final.log`: PASS - scripts compile and core ammo, threat, health and death rules remain valid.
- `Logs/stability-play-final.log`: PASS - full five-wave regression plus pooled combat-effect cleanup and reliable relay contention with 48 nearby scenery colliders.
- `Logs/stability-campaign.log`: PASS - Blackwood, Outpost and Skyline load in sequence with valid metadata, navigation, hostile entries and first-wave spawns.
- `Logs/stability-forest.log`: PASS - Blackwood terrain, all relay routes, bridge crossing and four authored vistas.
- `Logs/stability-audit.log`: Blackwood has 18,799,363 base/LOD0 vertices; Outpost and Skyline retain 157 and 160 shadow casters.
- `Logs/stability-windows-final.log`: PASS - rebuilt Windows player, 376,237,305 bytes reported by Unity.
- `Logs/stability-standalone-final.log`: PASS - deployment, options, preference-range migration, rifle effect, trail, creek, station, ridge and field guide; all captures are nonblank.
- `Logs/stability-performance.log`: clean direction pass - center/edge views held 59.9-60.0 FPS across every map; worst sampled frame was 17.2 ms on the Core Ultra 5 338H / Intel Arc B370 test machine.
- `Logs/stability-performance-final.log`, `Logs/stability-performance-final2.log` and `Logs/stability-performance-final3.log`: repeated final-player passes remained at 60 FPS outside isolated 66-91 ms stalls that moved between Blackwood center and Skyline center/edge. The spikes are not direction-specific or sustained; they remain a cold-start/background-system risk for visible playtesting.

The high-DPI fullscreen guard keeps Balanced on its optimized 1080p-class budget and limits Ultra to roughly a 1440p pixel budget above 1440p, using FSR while retaining Ultra LOD, SMAA and shadow quality. Combat tracers, sparks and decals are cleared during scene changes; relay contention now checks living enemies directly instead of a scenery-limited physics buffer. Stored sensitivity is clamped to the supported 5-60 range on load, while zero remains a valid master-audio setting.

## Visual and weapon upgrade verification / 2026-09-08

- `Logs/visual-upgrade-play.log`: PASS - firing, pooled tracers/impacts, reload, cover, five waves, upgrades, extraction, restart and defeat.
- `Logs/visual-upgrade-forest-final.log`: PASS - upgraded forest shaders, creek, trail, station, ridge and physical bridge traversal.
- `Logs/visual-upgrade-campaign.log`: PASS - all three scenes, navigation, relay routes, hostile entries and wave-one spawns.
- `Logs/visual-upgrade-audit.log`: Blackwood remains at 18,799,363 base/LOD0 vertices; Outpost has 157 shadow casters and Skyline 160 after the added viewmodel detail.
- `Logs/visual-upgrade-windows-final.log`: PASS - Windows build, 376,488,569 bytes.
- `Logs/visual-upgrade-standalone-final.log`: PASS - deployment, options, rifle flash, trail, creek, station, ridge and field guide produced nonblank standalone captures.
- `Logs/visual-upgrade-performance.log`: PASS - center and edge views across all maps held 59.7-60.0 FPS at the 60 FPS cap; worst sampled frame was 21.0 ms on Intel Arc B370.

The visual smoke test writes PNG files between viewpoints, so its following sample can include image-encoding stalls. The campaign performance check performs no captures and is the relevant rendering benchmark.

## Campaign performance verification / 2026-09-08

- `Logs/campaign-performance-audit-final.log`: Blackwood base/LOD0 geometry fell from 248,580,205 to 18,798,787 vertices; only 14 hero meshes remain above 100,000 vertices. Outpost shadow casters fell from 598 to 163, and Skyline from 3,620 to 166.
- `Logs/optimization-play-validation.log`: PASS - combat, cover, projectile sweep, five waves, upgrades, relay interruption, extraction, restart and defeat after the performance rebuild.
- `Logs/optimization-forest-validation.log`: PASS - Blackwood trail paint, relay routes and physical bridge traversal after tree density, LOD, collider and shadow changes.
- `Logs/optimization-campaign-validation.log`: PASS - all three scenes, navigation routes, hostile entries and wave-one spawns.
- `Logs/optimization-windows-build-final2.log`: PASS - Windows build, 376,143,185 bytes.
- `Logs/campaign-performance-player-final.log`: PASS - center and edge views in Blackwood, Outpost and Skyline all held 59.8-60.0 FPS at the 60 FPS cap; worst sampled frame was 20.4 ms on the development Intel Arc B370 GPU.

These automated samples use a hidden 1600x900 Windows player. A visible pass at the monitor's native resolution is still required to judge input feel, presentation and sustained thermals.

## Forest chapter verification / 2026-09-07

- `Logs/forest-views.log`: PASS — Blackwood first, expedition HUD, persistent dirt-trail paint, complete paths to all three authored relays, physical bridge traversal without jumping, and four vista previews.
- `Logs/forest-core.log`: PASS — ammo, health/death and threat-profile rules.
- `Logs/forest-play.log`: PASS — full five-wave Outpost regression, projectile/cover checks, upgrades, interrupted uploads, extraction reset, victory/restart/defeat and options.
- `Logs/forest-campaign.log`: PASS — reordered three-scene campaign, spawn routes, all relay paths, wave spawning and character alignment.
- `Logs/forest-windows.log`: PASS — Windows build with Blackwood first, approximately 584 MB reported build content.
- `Logs/forest-player.log`: PASS — standalone boot, usable deployment/options/guide actions, pause and seven nonblank offscreen renders. No managed gameplay errors in this smoke test. Desktop presentation and physical input still require a visible human playtest.

These checks do not certify a full forest playthrough, controller ergonomics or performance on other hardware. New creature types, boss fights, additional weapons and inventory are still outside this environment milestone.

## Earlier operation-loop verification / 2026-09-07

- `Logs/completion-play.log`: PASS — full five-wave Outpost run, combat/cover/reload/projectile checks, upgrade spending and caps, contested/interrupted uploads, pause during upload, extraction reset, victory/restart/defeat, option bounds/runtime graphics copies, and reload/empty-chamber event counts.
- `Logs/completion-campaign.log`: PASS — all three scenes, spawn routes, complete paths to all nine relay positions, wave-one spawning, animated character scale/alignment.
- `Logs/completion-windows.log`: PASS — Windows build, 292,850,544 reported bytes.
- `Logs/completion-player.log`: standalone Windows player launched and remained responsive; no managed gameplay exceptions were found during startup. This is a boot smoke test, not an automated standalone playthrough.

Unity's editor-only SearchDatabase startup exception remains in the batch logs. The play validator excludes only that known editor stack, not gameplay errors. Unity analytics could not connect to its remote configuration endpoint during the standalone boot check; offline startup still proceeded. Listening quality, controller ergonomics and the aspect-ratio matrix below still need human review.

## Mission loop

- Deploy in each of Outpost, Blackwood and Skyline at the selected difficulty.
- Reach relay one; start its upload with E / controller west button.
- Leave its ring during upload: progress resets and an interruption notice appears.
- Let an enemy approach within five metres: upload is blocked or interrupted.
- Pause during an upload or extraction: simulation and progress must stop.
- Complete the upload; relay two remains locked until wave three.
- Clear a wave. Buy an upgrade using 1–3, or pause and use the field-guide buttons.
- Credits never become negative; no branch exceeds three ranks; combat blocks purchases.
- Press Enter / use the next-wave button to skip the remaining resupply timer.
- Clear five waves and all three relays, in either order.
- Follow the extraction marker back to deployment. Stay inside for five seconds.
- Leaving extraction resets the countdown; victory appears only after a completed extraction.
- Restart or enter another mission: upgrades and relay progress reset, completed-map records remain.
- Clearing Skyline alone must not mark the entire campaign complete.

## Interface and preferences

- Try mouse/keyboard and controller navigation in every menu.
- Check 16:9, 16:10, 4:3 and ultrawide: no inaccessible buttons or clipped panels.
- Adjust FOV, sensitivity, audio, inverted look, reduced motion, frame cap and fullscreen/windowed mode; verify F11 and Alt+Enter.
- Change all three graphics presets; no missing/pink materials or broken shadows.
- Close/reopen the game: settings persist, no Unity project graphics asset is modified.
- Pause an active run; restart/exit asks for a second click. Resume cancels that confirmation.
- Confirm the displayed maximum health increases after a vitality upgrade.

## Known scope

Three single-player operations, one rifle, four enemy roles. No multiplayer, mid-run checkpoint, weapon inventory or fully remappable controls yet. Audio is currently synthesized; this is not a finished AAA production. Windows graphics and input behavior should also be checked on hardware other than the development machine.
