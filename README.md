# GunQuest: Outpost

GunQuest is a compact first-person survival shooter built with Unity 6 and URP. The playable **Outpost** operation asks one operator to hold an arena against five increasingly aggressive waves.

## Play

Open the project with Unity `6000.4.8f1`, load `Assets/Scenes/Outpost.unity`, and enter Play Mode. You can also launch the prebuilt Windows version at `Builds/Windows/GunQuest.exe` after running the build command below.

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

Health and ammunition caches respawn around the arena. Clearing a wave restores 20 health and supplies 60 reserve rounds.

Choose a threat level before deployment. Recruit fields fewer, softer enemies and provides generous resupply. Operator is the balanced default. Veteran increases enemy count, speed, durability and score rewards while reducing between-wave supplies.

## Editor tools

The `GunQuest` menu contains the supported project workflows:

- `Outpost / Generate playable outpost` regenerates the scene, materials, enemy prefab and baked NavMesh.
- `Outpost / Build Windows player` creates `Builds/Windows/GunQuest.exe`.
- `Validation / Validate combat rules` verifies ammo conservation and health/death rules.
- `Validation / Run outpost play checks` exercises navigation, combat, cover, pause, all five waves, victory, restart and defeat in Play Mode.

Equivalent headless commands from PowerShell:

```powershell
& 'D:/OS/Downloads/unity/6000.4.8f1/Editor/Unity.exe' -batchmode -nographics -quit -projectPath . -executeMethod CoreValidation.Run -logFile Logs/core-validation.log
& 'D:/OS/Downloads/unity/6000.4.8f1/Editor/Unity.exe' -batchmode -projectPath . -executeMethod PlayValidation.Run -logFile Logs/play-validation.log
& 'D:/OS/Downloads/unity/6000.4.8f1/Editor/Unity.exe' -batchmode -nographics -quit -projectPath . -executeMethod OutpostBuilder.BuildWindows -logFile Logs/windows-build.log
```

The project includes third-party sample art under `Assets/DL`; those assets retain their original authorship and licenses.
