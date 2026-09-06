# GunQuest

GunQuest is a compact first-person survival campaign built with Unity 6 and URP. One operator fights through three distinct five-wave operations:

- **Outpost** — a bright military relay with open firing lanes and modular cover.
- **Blackwood** — a fogbound ancient village built around a corrupted spirit shrine.
- **Skyline** — a neon midnight district of streets, vehicles and hostile crossfire.

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

Health and ammunition caches are positioned along each arena's riskier routes. Clearing a wave restores 20 health and supplies 60 reserve rounds.

Choose a threat level before deployment. Recruit fields fewer, softer enemies and provides generous resupply. Operator is the balanced default. Veteran increases enemy count, speed, durability and score rewards while reducing between-wave supplies.

Enemy formations evolve across a run: fast green **Runners** enter from wave two, armored red **Juggernauts** from wave three, and long-range blue **Marksmen** from wave four. Their silhouettes, shoulder colors, health, movement, projectile speed, damage and score value are distinct; the tactical HUD reports special threats still alive.

Each victory records a per-map, per-difficulty best score and awards a performance rank from C to S using accuracy, remaining health, completion time and threat level. Cleared operations are marked directly on the campaign selector.

Outpost wind, Blackwood's low forest drone and Skyline's electronic pulse are generated at runtime, so each operation has its own lightweight soundscape. Victory and defeat have dedicated stingers, and both audio level and look sensitivity are adjustable from every menu.

## Visual direction

The PC presentation uses 115% render scale, 4x MSAA plus high-quality SMAA, 4K cascaded shadows, ACES tonemapping, restrained bloom, film grain and per-operation color grading. Outpost, Blackwood and Skyline each use a dedicated 4K HDR environment, PBR ground materials, atmospheric particles and local accent lighting. Enemies use the animated PBR sci-fi warrior, while the first-person GQ-30 is baked from its detailed rifle mesh. The deployment menu includes a custom transparent GunQuest emblem.

The imported asphalt, concrete, dirt and HDR environments under `Assets/ThirdParty/PolyHaven` are CC0 assets from [Poly Haven](https://polyhaven.com/); exact source URLs are recorded in that folder's `LICENSE.txt`.

## Editor tools

The `GunQuest` menu contains the supported project workflows:

- `Outpost / Generate playable outpost` regenerates all three campaign scenes, materials, enemy prefab and baked NavMeshes.
- `Outpost / Build Windows player` creates `Builds/Windows/GunQuest.exe`.
- `Validation / Validate combat rules` verifies ammo conservation and health/death rules.
- `Validation / Run outpost play checks` exercises navigation, combat, cover, pause, all five waves, victory, restart and defeat in Play Mode.
- `Validation / Run campaign scene checks` opens every operation in Play Mode and verifies metadata, player placement, hostile entrances, navigation and wave-one spawning.

Equivalent headless commands from PowerShell:

```powershell
& 'D:/OS/Downloads/unity/6000.4.8f1/Editor/Unity.exe' -batchmode -nographics -quit -projectPath . -executeMethod CoreValidation.Run -logFile Logs/core-validation.log
& 'D:/OS/Downloads/unity/6000.4.8f1/Editor/Unity.exe' -batchmode -projectPath . -executeMethod PlayValidation.Run -logFile Logs/play-validation.log
& 'D:/OS/Downloads/unity/6000.4.8f1/Editor/Unity.exe' -batchmode -projectPath . -executeMethod CampaignValidation.Run -logFile Logs/campaign-validation.log
& 'D:/OS/Downloads/unity/6000.4.8f1/Editor/Unity.exe' -batchmode -nographics -quit -projectPath . -executeMethod OutpostBuilder.BuildWindows -logFile Logs/windows-build.log
```

The project includes third-party sample art under `Assets/DL`; those assets retain their original authorship and licenses.
