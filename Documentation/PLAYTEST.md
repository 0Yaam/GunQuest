# Operation completion checklist

This checklist describes the current local Windows build. Automated checks complement, but do not replace, human playtesting.

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
- Adjust FOV, sensitivity, audio, inverted look, reduced motion and frame cap.
- Change all three graphics presets; no missing/pink materials or broken shadows.
- Close/reopen the game: settings persist, no Unity project graphics asset is modified.
- Pause an active run; restart/exit asks for a second click. Resume cancels that confirmation.
- Confirm the displayed maximum health increases after a vitality upgrade.

## Known scope

Three single-player operations, one rifle, four enemy roles. No multiplayer, mid-run checkpoint, weapon inventory or fully remappable controls yet. Audio is currently synthesized; this is not a finished AAA production. Windows graphics and input behavior should also be checked on hardware other than the development machine.
