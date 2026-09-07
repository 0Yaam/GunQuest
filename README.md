# GunQuest

GunQuest is a first-person survival campaign built with Unity 6 and URP. One operator links three relays, fights through five waves and extracts alive in each operation:

- **Outpost** — a desert processing station with overhead pipe racks, pressure vessels and a raised service deck.
- **Blackwood** — an abandoned field laboratory surrounded by a modeled forest and continuous highland terrain.
- **Skyline** — a midnight industrial district with recessed windows, warm facade lighting and cool street lights.

## Play

Open the project with Unity `6000.4.8f1`, load any scene in `Assets/Scenes`, and enter Play Mode. The deployment screen can switch between all three operations, and victory advances to the next mission. You can also launch the prebuilt Windows version at `Builds/Windows/GunQuest.exe` after running the build command below.

| Input | Action |
| --- | --- |
| WASD / left stick | Move |
| Mouse / right stick | Look |
| Left mouse / right trigger | Fire |
| Right mouse / left trigger | Aim |
| R / right shoulder | Reload |
| Left Shift / stick press | Sprint |
| Left Control / east button | Crouch |
| Space / south button | Jump |
| Escape / Start | Pause |
| E / west button (X on Xbox) | Start a relay upload |
| 1 / 2 / 3 | Buy damage / reload / vitality upgrades between waves |
| Enter | Call the next wave early |

Health and ammunition caches are positioned along each arena's riskier routes. Clearing a wave restores 20 health and supplies 60 reserve rounds.

## Operation objectives

Follow the HUD marker to the three physical relay consoles. Relays unlock sequentially in waves 1, 3 and 5. Press E (or the controller's west button) within 3.2 metres to start a five-second upload. Stay near the console: leaving the ring or allowing a living hostile within five metres cancels the upload and resets its progress. Completed relays stay secured for the rest of the run; unfinished relays can still be linked after wave five.

Clearing all hostiles does **not** immediately win the mission. After all three relays are linked and all five waves are cleared, return to the green extraction ring at deployment and remain inside for five seconds. Leaving resets the extraction countdown. Pause freezes both timers. Mission completion, best score and rank are saved only according to the result; campaign completion requires all three maps to be cleared, not just Skyline.

Between waves, there is a 20-second resupply window. Each of the first four clears awards one upgrade credit. Buy +5 rifle damage, -0.25 seconds reload time, or +15 maximum health with 30 immediate healing. Each branch supports three ranks. Upgrades cannot be purchased during combat and reset when restarting or moving to another operation. Press Escape to make the same purchases with the mouse/controller while paused; the field guide also offers a next-wave button.

## Preferences and accessibility

Menus include a field guide, persistent sensitivity and audio sliders, a 65–100 degree field-of-view slider, inverted vertical look, reduced weapon motion and a 60 FPS/unlimited frame cap. Performance, Balanced and Ultra presets adjust internal resolution, MSAA and shadow distance without changing project assets. The safe-area layout keeps controls visible at narrower aspect ratios. Restarting or exiting an active paused run requires a second confirmation click; there is no mid-run checkpoint save.

Choose a threat level before deployment. Recruit fields fewer, softer enemies and provides generous resupply. Operator is the balanced default. Veteran increases enemy count, speed, durability and score rewards while reducing between-wave supplies.

Enemy formations evolve across a run: fast green **Runners** enter from wave two, armored red **Juggernauts** from wave three, and long-range blue **Marksmen** from wave four. Their silhouettes, shoulder colors, health, movement, projectile speed, damage and score value are distinct; the tactical HUD reports special threats still alive.

Each victory records a per-map, per-difficulty best score and awards a performance rank from C to S using accuracy, remaining health, completion time and threat level. Cleared operations are marked directly on the campaign selector.

Outpost wind, Blackwood's low forest drone and Skyline's electronic pulse are generated at runtime, so each operation has its own lightweight soundscape. Victory and defeat have dedicated stingers, and both audio level and look sensitivity are adjustable from every menu.

## Visual direction

The rebuilt environments use a consistent industrial material palette, beveled architectural meshes with world-scaled UVs, actual window reveals, layered ventilation panels, modeled pipes, tank reinforcement bands, accessible stairs and elevated service decks. A continuous sculpted landscape replaces the old box-shaped mountains. Blackwood's canopy consists of 44 textured tree models, not a forest photograph.

The PC presentation uses 115% render scale, 4x MSAA plus high-quality SMAA, 4K cascaded shadows, ACES tonemapping, restrained bloom, per-operation exposure, local reflection probes and broad sky fill. The GQ-30 mesh is centered and angled to show the receiver, with support/trigger gloves and sleeves. The deployment menu hides the first-person weapon and preserves text contrast over the live environment.

The imported asphalt, concrete, dirt and HDR environments under `Assets/ThirdParty/PolyHaven` are CC0 assets from [Poly Haven](https://polyhaven.com/); exact source URLs are recorded in that folder's `LICENSE.txt`.

Current gameplay captures: [Outpost](Documentation/Visuals/outpost.png), [Blackwood](Documentation/Visuals/blackwood.png), [Skyline](Documentation/Visuals/skyline.png). These are rendered Play Mode captures, not concept art.

## Editor tools

The `GunQuest` menu contains the supported project workflows:

- `World / Rebuild industrial campaign` regenerates all three current scenes, beveled meshes, materials, lights and baked NavMeshes. `Outpost / Generate playable outpost` invokes the same builder. These commands replace the generated campaign scenes, so save custom scene work separately before regenerating.
- `Outpost / Build Windows player` creates `Builds/Windows/GunQuest.exe`.
- `Validation / Validate combat rules` verifies ammo conservation and health/death rules.
- `Validation / Run outpost play checks` exercises navigation, combat, cover, pause, all five waves, upgrade credit/cap rules, interrupted relay uploads, extraction reset, victory, restart and defeat in Play Mode.
- `Validation / Run campaign scene checks` verifies every scene, complete navigation routes from all hostile entries and to every relay, wave spawning, animated enemy scale and alignment. It captures menus, gameplay and separate posed character checks in `Logs/Screenshots`.

Equivalent headless commands from PowerShell:

```powershell
& 'D:/OS/Downloads/unity/6000.4.8f1/Editor/Unity.exe' -batchmode -nographics -quit -projectPath . -executeMethod CoreValidation.Run -logFile Logs/core-validation.log
& 'D:/OS/Downloads/unity/6000.4.8f1/Editor/Unity.exe' -batchmode -projectPath . -executeMethod PlayValidation.Run -logFile Logs/play-validation.log
& 'D:/OS/Downloads/unity/6000.4.8f1/Editor/Unity.exe' -batchmode -projectPath . -executeMethod CampaignValidation.Run -logFile Logs/campaign-validation.log
& 'D:/OS/Downloads/unity/6000.4.8f1/Editor/Unity.exe' -batchmode -nographics -quit -projectPath . -executeMethod OutpostBuilder.BuildWindows -logFile Logs/windows-build.log
```

The project includes third-party sample art under `Assets/DL`; those assets retain their original authorship and licenses.
