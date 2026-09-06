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
    private Text wave, score, health, ammo, notice, hint, title, subtitle, stats, actionLabel, hitMarker, missionHud, operationLine;
    private Image healthFill, reloadFill, damageOverlay;
    private Button primary;
    private Button recruitButton, operatorButton, veteranButton;
    private Button[] missionButtons;
    private float hitUntil;
    private float damageAlpha;
    private static readonly Color Teal = new Color(0.28f, 0.94f, 0.79f);
    private static readonly Color Muted = new Color(0.58f, 0.68f, 0.71f);
    private static readonly Color Dark = new Color(0.025f, 0.05f, 0.065f, 0.93f);
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
        scaler.matchWidthOrHeight = 0.5f;
        canvas = root.GetComponent<RectTransform>();
        if (Object.FindAnyObjectByType<EventSystem>() == null)
            new GameObject("UI input", typeof(EventSystem), typeof(InputSystemUIInputModule));

        hud = Panel("HUD", canvas, Vector2.zero, new Vector2(1600, 900), Color.clear).gameObject;
        var h = hud.GetComponent<RectTransform>();
        Panel("Mission", h, new Vector2(40, 32), new Vector2(340, 108), Dark);
        missionHud = Label("", h, 60, 48, 300, 24, 16, Accent);
        wave = Label("", h, 60, 78, 300, 38, 28, Color.white);
        Panel("Score", h, new Vector2(1280, 32), new Vector2(280, 108), Dark);
        score = Label("", h, 1300, 47, 240, 75, 23, Color.white, TextAnchor.MiddleRight);
        notice = Label("", h, 400, 165, 800, 42, 23, Accent, TextAnchor.MiddleCenter);
        hint = Label("", h, 500, 208, 600, 32, 18, Muted, TextAnchor.MiddleCenter);
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

        menu = Panel("Menu", canvas, Vector2.zero, new Vector2(680, 900), Dark).gameObject;
        var m = menu.GetComponent<RectTransform>();
        Panel("Accent", m, new Vector2(56, 62), new Vector2(44, 4), Accent);
        Label("G U N Q U E S T", m, 116, 46, 440, 36, 18, Accent);
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
        MakeButton("RESTART OPERATION", m, 56, 614, 265, 48, new Color(0.15f, 0.22f, 0.24f), () => session.Restart());
        MakeButton("EXIT", m, 337, 614, 269, 48, new Color(0.15f, 0.22f, 0.24f), () => session.Quit());
        Label("LOOK SENSITIVITY", m, 56, 694, 290, 26, 14, Muted);
        var sliderRoot = Panel("Sensitivity", m, new Vector2(56, 734), new Vector2(550, 8), new Color(0.2f, 0.28f, 0.3f));
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
        Label("Clear five escalating waves. Recover field supplies.\nSecure every operation to finish the campaign.", m, 56, 785, 550, 58, 16, Muted);

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

    private void OnPrimary()
    {
        if (session.State == SessionState.Menu) session.StartRun();
        else if (session.State == SessionState.Paused) session.TogglePause();
        else if (session.State == SessionState.Victory) session.LoadNextMission();
        else session.Restart();
    }

    private void RefreshMenu()
    {
        bool playing = session.State == SessionState.Playing;
        hud.SetActive(playing);
        menu.SetActive(!playing);
        if (playing) return;
        bool won = session.State == SessionState.Victory;
        bool lost = session.State == SessionState.Defeat;
        missionHud.text = $"OPERATION / {session.missionName}";
        operationLine.text = $"TACTICAL SURVIVAL / {session.missionCode}   //   SELECT OPERATION";
        title.text = won ? "SECURED" : lost ? "SIGNAL LOST" : session.State == SessionState.Paused ? "ON HOLD" : session.missionName;
        title.fontSize = lost ? 62 : 76;
        subtitle.text = won ? session.victoryDescription : lost ? "Your position has been overrun.\nRegroup and try another approach." : session.State == SessionState.Paused ? "Operation paused.\nTake a breath. Choose your next move." : session.missionDescription;
        stats.text = $"BEST  {session.BestScore:000000}" + (session.Wave > 0 ? $"     SCORE  {session.Score:000000}\n{session.Kills} ELIMINATED / WAVE {session.Wave}" : "\n30-ROUND RIFLE / FIELD SUPPLIES AVAILABLE");
        actionLabel.text = session.State == SessionState.Menu ? "DEPLOY  >" : session.State == SessionState.Paused ? "RESUME  >" : won && session.HasNextMission ? "NEXT OPERATION  >" : won ? "REDEPLOY  >" : "TRY AGAIN  >";
        StyleDifficulty(recruitButton, session.Difficulty == Difficulty.Recruit);
        StyleDifficulty(operatorButton, session.Difficulty == Difficulty.Operator);
        StyleDifficulty(veteranButton, session.Difficulty == Difficulty.Veteran);
        recruitButton.interactable = operatorButton.interactable = veteranButton.interactable = session.State == SessionState.Menu;
        for (int i = 0; i < missionButtons.Length; i++)
        {
            missionButtons[i].interactable = session.State == SessionState.Menu && i != session.MissionIndex;
            StyleDifficulty(missionButtons[i], i == session.MissionIndex);
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
        health.text = $"{Mathf.CeilToInt(hp):000} / 100";
        healthFill.rectTransform.sizeDelta = new Vector2(258f * hp / session.player.maxHealth, 7);
        healthFill.color = hp <= 30 ? new Color(1f, 0.3f, 0.2f) : Accent;
        ammo.text = session.weapon.IsReloading ? "RELOADING" : $"{session.weapon.Ammo.Loaded:00} <color=#778E95>/ {session.weapon.Ammo.Reserve:000}</color>";
        reloadFill.rectTransform.sizeDelta = new Vector2(300f * session.weapon.ReloadProgress, 4f);
        notice.text = session.Notice;
        hint.text = session.EnemiesRemaining == 0 ? $"NEXT WAVE IN {Mathf.CeilToInt(session.NextWaveIn)}s / resupply and reposition" : session.weapon.Ammo.Loaded == 0 ? "R / RELOAD" : "";
        if (Time.unscaledTime > hitUntil) hitMarker.text = "";
        damageAlpha = Mathf.MoveTowards(damageAlpha, 0f, Time.deltaTime * 0.7f);
        damageOverlay.color = new Color(0.65f, 0.05f, 0.02f, damageAlpha);
    }

    private void OnHit(bool kill)
    {
        hitMarker.text = "X";
        hitMarker.color = kill ? new Color(1f, 0.55f, 0.2f) : Accent;
        hitUntil = Time.unscaledTime + 0.15f;
    }
    private void OnDamage(float amount) => damageAlpha = 0.24f;
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
