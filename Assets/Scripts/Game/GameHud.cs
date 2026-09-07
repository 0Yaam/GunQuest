using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using GunQuest.Game;

public sealed class GameHud : MonoBehaviour
{
    public GameSession session;
    private Font font;
    private RectTransform canvas;
    private GameObject menu;
    private GameObject hud;
    private GameObject noticeBacking;
    private GameObject operationsPanel, upgradeStrip;
    private Text objectiveTitle, objectiveHint, waypoint, upgradeStatus, fovLabel;
    private Image objectiveFill;
    private readonly Button[] upgrades = new Button[3];
    private readonly Button[] qualityButtons = new Button[3];
    private Text motionLabel, invertLabel, frameLabel;
    private Button readyButton;
    private bool confirmingRestart, confirmingQuit;
    private Button restartButton, quitButton;
    private Text wave, score, health, ammo, notice, hint, title, subtitle, stats, actionLabel, hitMarker, missionHud, operationLine;
    private Image healthFill, reloadFill, damageOverlay;
    private Button primary;
    private Button recruitButton, operatorButton, veteranButton;
    private Button[] missionButtons;
    private float hitUntil;
    private float damageAlpha;
    private static readonly Color Teal = new Color(0.28f, 0.94f, 0.79f);
    private static readonly Color Muted = new Color(0.58f, 0.68f, 0.71f);
    private static readonly Color Dark = new Color(0.035f, 0.055f, 0.075f, 0.82f);
    private Color Accent => session != null ? session.missionAccent : Teal;

    private void Start()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var root = new GameObject("GunQuest UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform);
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var stage = new GameObject("Safe layout / 1600 x 900", typeof(RectTransform));
        canvas = stage.GetComponent<RectTransform>();
        canvas.SetParent(root.transform, false);
        canvas.anchorMin = canvas.anchorMax = canvas.pivot = new Vector2(0.5f, 0.5f);
        canvas.sizeDelta = new Vector2(1600, 900);
        if (Object.FindAnyObjectByType<EventSystem>() == null)
            new GameObject("UI input", typeof(EventSystem), typeof(InputSystemUIInputModule));

        hud = Panel("HUD", canvas, Vector2.zero, new Vector2(1600, 900), Color.clear).gameObject;
        var h = hud.GetComponent<RectTransform>();
        Panel("Mission", h, new Vector2(40, 32), new Vector2(340, 108), Dark);
        missionHud = Label("", h, 60, 48, 300, 24, 16, Accent);
        wave = Label("", h, 60, 78, 300, 38, 28, Color.white);
        Panel("Score", h, new Vector2(1280, 32), new Vector2(280, 108), Dark);
        score = Label("", h, 1300, 47, 240, 75, 23, Color.white, TextAnchor.MiddleRight);
        noticeBacking = Panel("Transmission backing", h, new Vector2(485, 169), new Vector2(630, 64), new Color(0.015f, 0.025f, 0.035f, 0.72f)).gameObject;
        notice = Label("", h, 400, 165, 800, 42, 23, Accent, TextAnchor.MiddleCenter);
        hint = Label("", h, 500, 208, 600, 32, 18, Muted, TextAnchor.MiddleCenter);
        var objectivePanel = Panel("Mission objective", h, new Vector2(40, 157), new Vector2(350, 147), Dark);
        Label("PRIMARY OBJECTIVE", objectivePanel, 20, 12, 310, 22, 13, Muted);
        objectiveTitle = Label("", objectivePanel, 20, 38, 310, 30, 22, Accent);
        objectiveHint = Label("", objectivePanel, 20, 73, 310, 54, 16, Color.white);
        Panel("Objective track", objectivePanel, new Vector2(20, 132), new Vector2(310, 4), new Color(0.2f, 0.25f, 0.27f));
        objectiveFill = Panel("Objective progress", objectivePanel, new Vector2(20, 132), new Vector2(0, 4), Accent).GetComponent<Image>();
        waypoint = Label("", h, 690, 310, 220, 52, 18, Accent, TextAnchor.MiddleCenter);
        Panel("Vitals", h, new Vector2(40, 772), new Vector2(300, 90), Dark);
        Label("VITALS", h, 60, 785, 100, 22, 14, Muted);
        health = Label("", h, 172, 779, 145, 37, 27, Color.white, TextAnchor.MiddleRight);
        Panel("Health track", h, new Vector2(60, 829), new Vector2(258, 7), new Color(0.2f, 0.25f, 0.27f));
        healthFill = Panel("Health fill", h, new Vector2(60, 829), new Vector2(258, 7), Accent).GetComponent<Image>();
        Panel("Weapon", h, new Vector2(1220, 755), new Vector2(340, 107), Dark);
        Label("GQ-30 / AUTOMATIC", h, 1240, 768, 290, 22, 14, Accent);
        ammo = Label("", h, 1240, 795, 290, 44, 32, Color.white, TextAnchor.MiddleRight);
        reloadFill = Panel("Reload", h, new Vector2(1240, 846), new Vector2(0, 4), Accent).GetComponent<Image>();
        Label("WASD move   SHIFT sprint   CTRL crouch   SPACE jump   RMB aim   R reload   ESC pause", h, 370, 850, 820, 24, 14, Muted, TextAnchor.MiddleCenter);
        Panel("Crosshair horizontal", h, new Vector2(792, 449), new Vector2(16, 2), new Color(1, 1, 1, 0.75f));
        Panel("Crosshair vertical", h, new Vector2(799, 442), new Vector2(2, 16), new Color(1, 1, 1, 0.75f));
        hitMarker = Label("", h, 775, 425, 50, 50, 38, Accent, TextAnchor.MiddleCenter);
        damageOverlay = Panel("Damage feedback", h, Vector2.zero, new Vector2(1600, 900), Color.clear).GetComponent<Image>();
        upgradeStrip = Panel("Intermission upgrades", h, new Vector2(430, 697), new Vector2(740, 135), Dark).gameObject;
        var u = upgradeStrip.transform;
        upgradeStatus = Label("", u, 20, 12, 700, 23, 17, Accent, TextAnchor.MiddleCenter);
        Label("1  DAMAGE +5       2  RELOAD -0.25s       3  MAX HEALTH +15", u, 20, 44, 700, 24, 18, Color.white, TextAnchor.MiddleCenter);
        Label("Spend 1 credit per upgrade / max 3 per branch\nENTER: next wave   |   ESC: upgrade with mouse or controller", u, 20, 78, 700, 44, 15, Muted, TextAnchor.MiddleCenter);

        menu = Panel("Menu", canvas, Vector2.zero, new Vector2(680, 900), new Color(0.018f, 0.028f, 0.038f, 0.96f)).gameObject;
        var m = menu.GetComponent<RectTransform>();
        Panel("Accent", m, new Vector2(56, 62), new Vector2(44, 4), Accent);
        Label("G U N Q U E S T", m, 116, 46, 390, 36, 18, Accent);
        var emblem = Resources.Load<Texture2D>("Brand/GunQuestEmblem");
        if (emblem != null) RawTexture("GunQuest emblem", m, emblem, new Vector2(532, 22), new Vector2(92, 92), Color.white);
        operationLine = Label("", m, 56, 99, 560, 25, 14, Muted);
        missionButtons = new Button[GameSession.MissionNames.Length];
        for (int i = 0; i < missionButtons.Length; i++)
        {
            int mission = i;
            missionButtons[i] = MakeButton(GameSession.MissionNames[i], m, 56 + i * 184, 130, 170, 38, Dark, () => session.LoadMission(mission));
        }
        title = Label("", m, 50, 184, 580, 92, 70, Color.white);
        subtitle = Label("", m, 56, 282, 552, 86, 21, Muted);
        stats = Label("", m, 56, 374, 552, 62, 19, Accent);
        Label("THREAT LEVEL", m, 56, 449, 290, 22, 14, Muted);
        recruitButton = MakeButton("RECRUIT", m, 56, 478, 174, 42, Dark, () => session.SetDifficulty(Difficulty.Recruit));
        operatorButton = MakeButton("OPERATOR", m, 244, 478, 174, 42, Dark, () => session.SetDifficulty(Difficulty.Operator));
        veteranButton = MakeButton("VETERAN", m, 432, 478, 174, 42, Dark, () => session.SetDifficulty(Difficulty.Veteran));
        primary = MakeButton("Deploy", m, 56, 540, 550, 60, Accent, OnPrimary);
        actionLabel = primary.GetComponentInChildren<Text>();
        restartButton = MakeButton("RESTART OPERATION", m, 56, 614, 265, 48, new Color(0.15f, 0.22f, 0.24f), ConfirmRestart);
        quitButton = MakeButton("EXIT", m, 337, 614, 269, 48, new Color(0.15f, 0.22f, 0.24f), ConfirmQuit);
        Label("LOOK SENSITIVITY", m, 56, 694, 260, 26, 14, Muted);
        Label("AUDIO LEVEL", m, 346, 694, 260, 26, 14, Muted);
        var sliderRoot = Panel("Sensitivity", m, new Vector2(56, 734), new Vector2(260, 8), new Color(0.2f, 0.28f, 0.3f));
        var slider = sliderRoot.gameObject.AddComponent<Slider>();
        sliderRoot.GetComponent<Image>().raycastTarget = true;
        var handle = Panel("Handle", sliderRoot, new Vector2(0, -7), new Vector2(16, 22), Accent);
        slider.handleRect = handle;
        slider.targetGraphic = handle.GetComponent<Image>();
        handle.GetComponent<Image>().raycastTarget = true;
        slider.minValue = 5f;
        slider.maxValue = 60f;
        slider.value = PlayerPrefs.GetFloat("GunQuest.Sensitivity", 20f);
        ApplySensitivity(slider.value);
        slider.onValueChanged.AddListener(ApplySensitivity);
        var audioRoot = Panel("Audio", m, new Vector2(346, 734), new Vector2(260, 8), new Color(0.2f, 0.28f, 0.3f));
        var audioSlider = audioRoot.gameObject.AddComponent<Slider>();
        audioRoot.GetComponent<Image>().raycastTarget = true;
        var audioHandle = Panel("Handle", audioRoot, new Vector2(0, -7), new Vector2(16, 22), Accent);
        audioSlider.handleRect = audioHandle;
        audioSlider.targetGraphic = audioHandle.GetComponent<Image>();
        audioHandle.GetComponent<Image>().raycastTarget = true;
        audioSlider.minValue = 0f;
        audioSlider.maxValue = 1f;
        audioSlider.value = PlayerPrefs.GetFloat("GunQuest.Audio", 0.75f);
        ApplyAudio(audioSlider.value);
        audioSlider.onValueChanged.AddListener(ApplyAudio);
        Label("Link three relays. Survive five waves. Extract alive.\nUpgrades reset on redeployment; best scores are saved.", m, 56, 785, 550, 58, 16, Muted);
        BuildOperationsPanel();

        session.StateChanged += RefreshMenu;
        session.weapon.Hit += OnHit;
        session.player.Damaged += OnDamage;
        RefreshMenu();
    }

    private void ApplySensitivity(float value)
    {
        var look = session.player.GetComponent<PlayerLook>();
        look.xSensitivity = look.ySensitivity = value;
        PlayerPrefs.SetFloat("GunQuest.Sensitivity", value);
    }

    private static void ApplyAudio(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("GunQuest.Audio", value);
    }

    private void OnPrimary()
    {
        if (session.State == SessionState.Menu) session.StartRun();
        else if (session.State == SessionState.Paused) session.TogglePause();
        else if (session.State == SessionState.Victory) session.LoadNextMission();
        else session.Restart();
    }

    private void ConfirmRestart()
    {
        if (session.State == SessionState.Paused && !confirmingRestart)
        {
            confirmingRestart = true;
            restartButton.GetComponentInChildren<Text>().text = "CONFIRM RESTART?";
            return;
        }
        session.Restart();
    }
    private void ConfirmQuit()
    {
        if (session.State == SessionState.Paused && !confirmingQuit)
        {
            confirmingQuit = true;
            quitButton.GetComponentInChildren<Text>().text = "CONFIRM EXIT?";
            return;
        }
        session.Quit();
    }

    private void BuildOperationsPanel()
    {
        operationsPanel = Panel("Field guide and options", canvas, new Vector2(920, 32), new Vector2(640, 830), new Color(0.018f, 0.028f, 0.038f, 0.94f)).gameObject;
        var p = operationsPanel.transform;
        Label("OPERATOR FIELD GUIDE", p, 28, 24, 584, 32, 22, Accent);
        Label("01  LINK THE RELAYS", p, 28, 75, 584, 26, 18, Color.white);
        Label("Follow the diamond. Press E / X near the console, then\nstay inside its ring for 5s. Nearby enemies interrupt uploads.\nRelays unlock during waves 1, 3 and 5.", p, 28, 109, 584, 69, 17, Muted);
        Label("02  HOLD THE SECTOR", p, 28, 193, 584, 26, 18, Color.white);
        Label("Clear five waves. Collect medical and ammunition caches.\nEach cleared wave grants a credit for an in-run upgrade.", p, 28, 227, 584, 50, 17, Muted);
        Label("03  EXTRACT ALIVE", p, 28, 288, 584, 26, 18, Color.white);
        Label("Once all relays and hostiles are cleared, return to the\ngreen beacon at deployment. Stay inside for 5s to win.", p, 28, 322, 584, 50, 17, Muted);
        Label("FIELD UPGRADES / BETWEEN WAVES", p, 28, 394, 584, 24, 15, Accent);
        string[] names = { "DAMAGE", "RELOAD", "VITALITY" };
        for (int i = 0; i < 3; i++)
        {
            int slot = i;
            upgrades[i] = MakeButton(names[i], p, 28 + i * 199, 428, 186, 42, Dark, () => session.PurchaseUpgrade(slot));
            upgrades[i].GetComponentInChildren<Text>().fontSize = 15;
        }
        readyButton = MakeButton("RESUME + CALL NEXT WAVE", p, 28, 482, 584, 38, Dark, () =>
        {
            if (session.State == SessionState.Paused) session.TogglePause();
            session.CallNextWave();
        });
        readyButton.GetComponentInChildren<Text>().fontSize = 16;
        Label("GRAPHICS", p, 28, 546, 584, 24, 15, Accent);
        string[] quality = { "PERFORMANCE", "BALANCED", "ULTRA" };
        for (int i = 0; i < 3; i++)
        {
            int preset = i;
            qualityButtons[i] = MakeButton(quality[i], p, 28 + i * 199, 580, 186, 38, Dark, () => session.Options.SetGraphics(preset));
            qualityButtons[i].GetComponentInChildren<Text>().fontSize = 15;
        }
        fovLabel = Label("", p, 28, 642, 220, 24, 15, Muted);
        var track = Panel("Field of view", p, new Vector2(256, 651), new Vector2(356, 8), new Color(0.2f, 0.28f, 0.3f));
        track.GetComponent<Image>().raycastTarget = true;
        var slider = track.gameObject.AddComponent<Slider>();
        var handle = Panel("Handle", track, new Vector2(0, -7), new Vector2(16, 22), Accent);
        handle.GetComponent<Image>().raycastTarget = true;
        slider.handleRect = handle;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.minValue = 65f; slider.maxValue = 100f; slider.wholeNumbers = true;
        slider.value = session.Options.FieldOfView;
        slider.onValueChanged.AddListener(session.Options.SetFieldOfView);
        motionLabel = MakeButton("", p, 28, 691, 286, 38, Dark, session.Options.ToggleMotion).GetComponentInChildren<Text>();
        invertLabel = MakeButton("", p, 326, 691, 286, 38, Dark, session.Options.ToggleInvert).GetComponentInChildren<Text>();
        frameLabel = MakeButton("", p, 28, 742, 286, 38, Dark, session.Options.ToggleFrameLimit).GetComponentInChildren<Text>();
        motionLabel.fontSize = invertLabel.fontSize = frameLabel.fontSize = 15;
        Label("Preferences save automatically", p, 326, 750, 286, 25, 15, Muted);
    }

    private void RefreshMenu()
    {
        bool playing = session.State == SessionState.Playing;
        hud.SetActive(playing);
        menu.SetActive(!playing);
        operationsPanel.SetActive(!playing);
        confirmingRestart = confirmingQuit = false;
        restartButton.GetComponentInChildren<Text>().text = "RESTART OPERATION";
        quitButton.GetComponentInChildren<Text>().text = "EXIT";
        var presentation = session.weapon.GetComponent<WeaponPresentation>();
        if (presentation != null && presentation.viewModel != null) presentation.viewModel.gameObject.SetActive(playing);
        if (playing) return;
        bool won = session.State == SessionState.Victory;
        bool lost = session.State == SessionState.Defeat;
        missionHud.text = $"OPERATION / {session.missionName}";
        operationLine.text = $"CAMPAIGN / {GameSession.ClearedMissionCount} OF 3 SECURED   //   OPERATION {session.missionCode}";
        title.text = won ? "EXTRACTED" : lost ? "SIGNAL LOST" : session.State == SessionState.Paused ? "ON HOLD" : session.missionName;
        title.fontSize = won || lost ? 62 : 76;
        subtitle.text = won ? (session.HasNextMission ? "Uplink secured. Operator extracted.\nThe next operation is ready." : "All relays secured. Operator extracted.\nFinal operation complete.") : lost ? "Your position has been overrun.\nRegroup and try another approach." : session.State == SessionState.Paused ? "Operation paused.\nTake a breath. Choose your next move." : session.missionDescription;
        stats.text = won
            ? $"RANK  {session.PerformanceRank}     SCORE  {session.Score:000000}     BEST  {session.BestScore:000000}\n{session.Accuracy:0}% ACCURACY  /  {FormatTime(session.Elapsed)}  /  {session.Kills} ELIMINATED"
            : $"BEST  {session.BestScore:000000}" + (session.Wave > 0 ? $"     SCORE  {session.Score:000000}\n{session.Kills} ELIMINATED / WAVE {session.Wave} / {session.Accuracy:0}% ACC" : "\n30-ROUND RIFLE / FIELD SUPPLIES AVAILABLE");
        actionLabel.text = session.State == SessionState.Menu ? "DEPLOY  >" : session.State == SessionState.Paused ? "RESUME  >" : won && session.HasNextMission ? "NEXT OPERATION  >" : won ? "REDEPLOY  >" : "TRY AGAIN  >";
        StyleDifficulty(recruitButton, session.Difficulty == Difficulty.Recruit);
        StyleDifficulty(operatorButton, session.Difficulty == Difficulty.Operator);
        StyleDifficulty(veteranButton, session.Difficulty == Difficulty.Veteran);
        recruitButton.interactable = operatorButton.interactable = veteranButton.interactable = session.State == SessionState.Menu;
        for (int i = 0; i < missionButtons.Length; i++)
        {
            missionButtons[i].interactable = session.State == SessionState.Menu && i != session.MissionIndex;
            StyleDifficulty(missionButtons[i], i == session.MissionIndex);
            missionButtons[i].GetComponentInChildren<Text>().text = GameSession.MissionNames[i] + (GameSession.IsMissionCleared(i) ? "  [OK]" : "");
        }
        EventSystem.current?.SetSelectedGameObject(primary.gameObject);
    }

    private void StyleDifficulty(Button button, bool selected)
    {
        button.targetGraphic.color = selected ? Accent : new Color(0.11f, 0.17f, 0.19f);
        button.GetComponentInChildren<Text>().color = selected ? Dark : Color.white;
    }

    private void Update()
    {
        if (wave == null) return;
        wave.text = $"WAVE {session.Wave:00} / {session.totalWaves:00}";
        score.text = $"{session.Score:000000}\n{session.EnemiesRemaining:00} HOSTILES" + (session.ThreatSummary.Length > 0 ? $"\n<size=13>{session.ThreatSummary}</size>" : "");
        float hp = session.player.GetCurrentHealth();
        health.text = $"{Mathf.CeilToInt(hp):000} / {session.player.maxHealth:0}";
        healthFill.rectTransform.sizeDelta = new Vector2(258f * hp / session.player.maxHealth, 7);
        healthFill.color = hp <= 30 ? new Color(1f, 0.3f, 0.2f) : Accent;
        ammo.text = session.weapon.IsReloading ? "RELOADING" : $"{session.weapon.Ammo.Loaded:00} <color=#778E95>/ {session.weapon.Ammo.Reserve:000}</color>";
        reloadFill.rectTransform.sizeDelta = new Vector2(300f * session.weapon.ReloadProgress, 4f);
        notice.text = session.Notice;
        noticeBacking.SetActive(!string.IsNullOrEmpty(session.Notice));
        hint.text = session.EnemiesRemaining == 0 && session.Wave < session.totalWaves ? $"NEXT WAVE IN {Mathf.CeilToInt(session.NextWaveIn)}s / resupply and reposition" : session.weapon.Ammo.Loaded == 0 ? (session.weapon.Ammo.Reserve == 0 ? "OUT OF AMMO / find a field cache" : "R / RELOAD") : "";
        RefreshObjectives();
        RefreshOptions();
        if (Time.unscaledTime > hitUntil) hitMarker.text = "";
        damageAlpha = Mathf.MoveTowards(damageAlpha, 0f, Time.deltaTime * 0.7f);
        damageOverlay.color = new Color(0.65f, 0.05f, 0.02f, damageAlpha);
    }

    private void RefreshOptions()
    {
        upgradeStrip.SetActive(session.CanUpgrade && session.State == SessionState.Playing);
        upgradeStatus.text = $"FIELD REQUISITIONS / {session.UpgradeCredits} CREDIT(S) AVAILABLE";
        string[] names = { "DAMAGE", "RELOAD", "VITALITY" };
        for (int i = 0; i < 3; i++)
        {
            upgrades[i].interactable = session.CanUpgrade && session.UpgradeCredits > 0 && session.UpgradeLevel(i) < 3;
            upgrades[i].GetComponentInChildren<Text>().text = $"{names[i]} {session.UpgradeLevel(i)}/3";
        }
        readyButton.interactable = session.CanUpgrade;
        for (int i = 0; i < 3; i++) StyleDifficulty(qualityButtons[i], session.Options.GraphicsPreset == i);
        fovLabel.text = $"FIELD OF VIEW / {session.Options.FieldOfView:0}";
        motionLabel.text = "REDUCED MOTION / " + (session.Options.ReducedMotion ? "ON" : "OFF");
        invertLabel.text = "INVERT Y / " + (session.Options.InvertY ? "ON" : "OFF");
        frameLabel.text = "FRAME LIMIT / " + (session.Options.FrameLimit ? "60 FPS" : "UNLIMITED");
    }

    private void RefreshObjectives()
    {
        var objectives = session.Objectives;
        if (objectives == null) return;
        objectiveTitle.text = objectives.TargetName + $"   {objectives.Distance:0}m";
        objectiveHint.text = objectives.Instruction;
        objectiveFill.rectTransform.sizeDelta = new Vector2(310f * objectives.Progress, 4);
        Vector3 target = objectives.TargetPosition + Vector3.up * 2.9f;
        var camera = session.weapon.aimCamera;
        Vector3 screen = camera.WorldToScreenPoint(target);
        var uiCanvas = canvas.GetComponentInParent<Canvas>();
        Camera uiCamera = uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : camera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, screen, uiCamera, out var point);
        float x = point.x + 800, y = 450 - point.y;
        bool outside = screen.z <= 0 || x < 430 || x > 1170 || y < 270 || y > 650;
        if (screen.z <= 0)
        {
            float side = Vector3.Dot(camera.transform.right, target - camera.transform.position);
            x = side >= 0 ? 1160 : 440; y = 390;
        }
        x = Mathf.Clamp(x, 440, 1160);
        y = Mathf.Clamp(y, 275, 640);
        waypoint.rectTransform.anchoredPosition = new Vector2(x - 110, -y);
        waypoint.text = (outside ? (x < 800 ? "< " : "> ") : "◇ ") + objectives.TargetName + $"\n{objectives.Distance:0}m";
    }

    private void OnHit(bool kill)
    {
        hitMarker.text = "X";
        hitMarker.color = kill ? new Color(1f, 0.55f, 0.2f) : Accent;
        hitUntil = Time.unscaledTime + 0.15f;
    }
    private void OnDamage(float amount) => damageAlpha = 0.24f;
    private static string FormatTime(float seconds) => $"{Mathf.FloorToInt(seconds / 60f):00}:{Mathf.FloorToInt(seconds % 60f):00}";
    private void OnDestroy()
    {
        if (session == null) return;
        session.StateChanged -= RefreshMenu;
        if (session.weapon != null) session.weapon.Hit -= OnHit;
        if (session.player != null) session.player.Damaged -= OnDamage;
    }

    private RectTransform Panel(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(position.x, -position.y);
        rect.sizeDelta = size;
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private Text Label(string text, Transform parent, float x, float y, float w, float h, int size, Color color, TextAnchor alignment = TextAnchor.UpperLeft)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(w, h);
        var label = go.GetComponent<Text>();
        label.font = font;
        label.fontSize = size;
        label.color = color;
        label.text = text;
        label.alignment = alignment;
        label.raycastTarget = false;
        return label;
    }

    private static RectTransform RawTexture(string name, Transform parent, Texture texture, Vector2 position, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(position.x, -position.y);
        rect.sizeDelta = size;
        var image = go.GetComponent<RawImage>();
        image.texture = texture;
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private Button MakeButton(string text, Transform parent, float x, float y, float w, float h, Color color, UnityEngine.Events.UnityAction action)
    {
        var rect = Panel(text, parent, new Vector2(x, y), new Vector2(w, h), color);
        rect.GetComponent<Image>().raycastTarget = true;
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        button.onClick.AddListener(action);
        Label(text, rect, 18, 0, w - 36, h, 19, color == Teal || color == Accent ? Dark : Color.white, TextAnchor.MiddleLeft);
        return button;
    }
}
