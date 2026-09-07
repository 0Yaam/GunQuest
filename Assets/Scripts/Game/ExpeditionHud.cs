using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using GunQuest.Game;

/// <summary>Quiet expedition interface: the environment is the menu's principal visual.</summary>
public sealed class ExpeditionHud : MonoBehaviour
{
    public GameSession session;
    private Font body, display;
    private RectTransform stage;
    private GameObject menu, hud, sheet, requisitions;
    private Text heading, chapter, description, record, primaryLabel, wave, vitals, rounds, objective, instruction, transmission, waypoint, hit;
    private Text optionValues, upgradeText, intermission;
    private Image healthBar, objectiveBar, reloadBar, damageLeft, damageRight;
    private Button primary, restart, quit;
    private readonly Button[] chapters=new Button[3], difficulties=new Button[3], upgrades=new Button[3];
    private GameObject optionsPage, guidePage;
    private bool confirmRestart, confirmQuit;
    private float hitUntil, damageFade;
    private static readonly Color Ink=new Color(.025f,.034f,.033f,.93f);
    private static readonly Color Paper=new Color(.86f,.88f,.82f);
    private static readonly Color Muted=new Color(.56f,.62f,.59f);
    private Color Accent => Color.Lerp(session.missionAccent,new Color(.72f,.76f,.65f),.65f);

    private void Start()
    {
        body=Resources.Load<Font>("Fonts/Interface")??Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        display=Resources.Load<Font>("Fonts/Display")??body;
        var root=new GameObject("Expedition interface",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(transform);
        root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        stage=new GameObject("Safe area",typeof(RectTransform)).GetComponent<RectTransform>();stage.SetParent(root.transform,false);stage.anchorMin=stage.anchorMax=stage.pivot=Vector2.one*.5f;stage.sizeDelta=new Vector2(1600,900);
        if(Object.FindAnyObjectByType<EventSystem>()==null)new GameObject("UI input",typeof(EventSystem),typeof(InputSystemUIInputModule));
        BuildMenu();BuildHud();BuildSheet();
        session.StateChanged+=RefreshState;session.player.Damaged+=OnDamage;session.weapon.Hit+=OnHit;
        RefreshState();
    }

    private void BuildMenu()
    {
        menu=Panel("Deployment",stage,0,0,1600,900,Color.clear).gameObject;
        var m=menu.transform;
        // A soft native-UI gradient leaves most of the forest unobscured.
        var shade=new GameObject("Left cinematic scrim",typeof(RectTransform),typeof(CanvasRenderer),typeof(ExpeditionScrim));
        var rect=shade.GetComponent<RectTransform>();rect.SetParent(m,false);Place(rect,0,0,850,900);shade.GetComponent<ExpeditionScrim>().raycastTarget=false;
        Label("G U N Q U E S T",m,64,50,420,35,22,Paper,true);
        Label("FIELD OPERATIONS  /  CAMPAIGN",m,65,98,480,22,12,Muted);
        for(int i=0;i<3;i++)
        {
            int index=i;
            chapters[i]=Button($"0{i+1}  {GameSession.MissionNames[i]}",m,65+i*177,149,167,35,()=>session.LoadMission(index),13);
        }
        chapter=Label("",m,65,238,600,25,14,Accent);
        heading=Label("",m,59,274,700,106,84,Paper,true);
        description=Label("",m,65,397,494,114,19,new Color(.70f,.75f,.71f));description.lineSpacing=1.2f;
        primary=Button("BEGIN OPERATION",m,65,552,340,57,OnPrimary,19);primaryLabel=primary.GetComponentInChildren<Text>();
        Panel("Deploy accent",primary.transform,0,0,3,57,Accent);
        Button("OPTIONS",m,65,628,154,38,()=>OpenSheet(true),14);
        Button("FIELD GUIDE",m,230,628,175,38,()=>OpenSheet(false),14);
        Label("THREAT LEVEL",m,65,697,170,20,11,Muted);
        for(int i=0;i<3;i++)
        {
            int level=i;difficulties[i]=Button(((Difficulty)i).ToString().ToUpperInvariant(),m,65+i*150,725,140,33,()=>session.SetDifficulty((Difficulty)level),13);
        }
        record=Label("",m,65,804,620,47,13,Muted);
        restart=Button("RESTART",m,1260,796,125,34,Restart,13);
        quit=Button("EXIT",m,1400,796,125,34,Quit,13);
        Label("WASD / MOVE     MOUSE / LOOK     E / INTERACT",m,970,851,557,22,12,Muted, false,TextAnchor.MiddleRight);
    }

    private void BuildHud()
    {
        hud=Panel("In-field HUD",stage,0,0,1600,900,Color.clear).gameObject;var h=hud.transform;
        Label("GQ / FIELD LINK",h,44,34,280,25,13,Accent);
        wave=Label("",h,1230,34,325,49,16,Paper,false,TextAnchor.UpperRight);
        var obj=Panel("Current objective",h,44,92,345,118,new Color(.025f,.034f,.033f,.68f));
        Panel("Objective keyline",obj,0,0,2,118,Accent);
        objective=Label("",obj,17,12,315,31,19,Paper,true);
        instruction=Label("",obj,17,50,315,47,14,new Color(.73f,.77f,.73f));
        Panel("Objective track",obj,17,105,308,2,new Color(1,1,1,.13f));objectiveBar=Panel("Upload progress",obj,17,105,0,2,Accent).GetComponent<Image>();
        transmission=Label("",h,471,66,685,64,17,Paper,false,TextAnchor.MiddleCenter);
        var noticeShadow=transmission.gameObject.AddComponent<Shadow>();noticeShadow.effectColor=new Color(0,0,0,.85f);noticeShadow.effectDistance=new Vector2(1,-1);
        Label("VITALS",h,44,785,100,21,11,Muted);vitals=Label("",h,44,812,230,42,27,Paper,true);
        Panel("Vitals track",h,44,862,205,3,new Color(1,1,1,.15f));healthBar=Panel("Health",h,44,862,205,3,Accent).GetComponent<Image>();
        Label("GQ-30 / SERVICE RIFLE",h,1230,785,325,22,12,Muted,false,TextAnchor.MiddleRight);
        rounds=Label("",h,1230,811,325,43,32,Paper,true,TextAnchor.MiddleRight);reloadBar=Panel("Reload progress",h,1350,862,205,3,Accent).GetComponent<Image>();
        Label("R  RELOAD     E  UPLINK     ESC  PAUSE",h,480,859,640,21,11,Muted,false,TextAnchor.MiddleCenter);
        foreach(var offset in new[]{new Vector2(-7,0),new Vector2(6,0),new Vector2(0,-7),new Vector2(0,6)})
            Panel("Aim mark",h,800+offset.x,450+offset.y,offset.x==0?1:3,offset.x==0?3:1,new Color(1,1,1,.65f));
        hit=Label("",h,775,428,50,45,26,Paper,false,TextAnchor.MiddleCenter);
        waypoint=Label("",h,690,350,220,46,13,Accent,false,TextAnchor.MiddleCenter);var shadow=waypoint.gameObject.AddComponent<Shadow>();shadow.effectColor=Color.black;
        damageLeft=Panel("Left damage edge",h,0,0,12,900,Color.clear).GetComponent<Image>();damageRight=Panel("Right damage edge",h,1588,0,12,900,Color.clear).GetComponent<Image>();
        requisitions=Panel("Resupply strip",h,460,738,680,80,new Color(.025f,.034f,.033f,.8f)).gameObject;
        intermission=Label("",requisitions.transform,18,10,644,60,15,Paper,false,TextAnchor.MiddleCenter);
    }

    private void BuildSheet()
    {
        sheet=Panel("Field notebook",stage,906,156,630,578,Ink).gameObject;
        var p=sheet.transform;
        Button("OPTIONS",p,27,24,178,38,()=>OpenSheet(true),15);
        Button("FIELD GUIDE",p,214,24,205,38,()=>OpenSheet(false),15);
        Button("CLOSE",p,479,24,124,38,()=>sheet.SetActive(false),13);
        Panel("Notebook rule",p,27,77,576,1,new Color(1,1,1,.15f));
        optionsPage=Panel("Options page",p,27,100,576,455,Color.clear).gameObject;
        var o=optionsPage.transform;
        Slider("LOOK SENSITIVITY",o,0,0,5,60,PlayerPrefs.GetFloat("GunQuest.Sensitivity",20),value=>
        { var look=session.player.GetComponent<PlayerLook>();look.xSensitivity=look.ySensitivity=value;PlayerPrefs.SetFloat("GunQuest.Sensitivity",value); });
        var playerLook=session.player.GetComponent<PlayerLook>();playerLook.xSensitivity=playerLook.ySensitivity=PlayerPrefs.GetFloat("GunQuest.Sensitivity",20);
        Slider("MASTER AUDIO",o,0,66,0,1,PlayerPrefs.GetFloat("GunQuest.Audio",.75f),value=>{AudioListener.volume=value;PlayerPrefs.SetFloat("GunQuest.Audio",value);});
        Slider("FIELD OF VIEW",o,0,132,65,100,session.Options.FieldOfView,session.Options.SetFieldOfView);
        Label("RENDERING QUALITY",o,0,205,300,22,12,Muted);
        for(int i=0;i<3;i++) { int preset=i;Button(new[]{"PERFORMANCE","BALANCED","ULTRA"}[i],o,i*194,233,184,35,()=>session.Options.SetGraphics(preset),12); }
        Button("REDUCE MOTION",o,0,293,278,35,session.Options.ToggleMotion,12);
        Button("INVERT Y",o,295,293,278,35,session.Options.ToggleInvert,12);
        Button("60 FPS / UNLIMITED",o,0,340,278,35,session.Options.ToggleFrameLimit,12);
        optionValues=Label("",o,0,399,576,44,12,Muted);
        guidePage=Panel("Guide page",p,27,100,576,455,Color.clear).gameObject;var g=guidePage.transform;
        Label("RESTORE. SURVIVE. EXTRACT.",g,0,0,576,36,24,Paper,true);
        Label("Follow the site marker. Press E / controller X near a relay.\nStay inside its perimeter for five seconds; nearby hostiles\nor leaving the site interrupt the upload.\n\nRelays unlock in waves 1, 3 and 5. Secure all three, clear\nthe five waves, then return to the extraction beacon.\nStay there for five seconds to complete the operation.",g,0,55,576,195,16,new Color(.70f,.75f,.71f));
        upgradeText=Label("",g,0,269,576,32,14,Accent);
        for(int i=0;i<3;i++) { int type=i;upgrades[i]=Button(new[]{"DAMAGE","RELOAD","VITALITY"}[i],g,i*194,312,184,38,()=>session.PurchaseUpgrade(type),13); }
        Button("RESUME + NEXT WAVE",g,0,365,576,40,()=>{if(session.State==SessionState.Paused)session.TogglePause();session.CallNextWave();},14);
        Label("Upgrades reset each run. Progress records save on completion.",g,0,423,576,25,12,Muted);
        sheet.SetActive(false);
    }

    private void OpenSheet(bool options)
    { sheet.SetActive(true);optionsPage.SetActive(options);guidePage.SetActive(!options);EventSystem.current?.SetSelectedGameObject(sheet.GetComponentInChildren<Button>().gameObject); }
    private void OnPrimary()
    {
        if(session.State==SessionState.Menu)session.StartRun();
        else if(session.State==SessionState.Paused)session.TogglePause();
        else if(session.State==SessionState.Victory)session.LoadNextMission();else session.Restart();
    }
    private void Restart()
    { if(session.State==SessionState.Paused&&!confirmRestart){confirmRestart=true;restart.GetComponentInChildren<Text>().text="CONFIRM?";return;}session.Restart(); }
    private void Quit()
    { if(session.State==SessionState.Paused&&!confirmQuit){confirmQuit=true;quit.GetComponentInChildren<Text>().text="CONFIRM?";return;}session.Quit(); }
    private void RefreshState()
    {
        bool playing=session.State==SessionState.Playing;
        menu.SetActive(!playing);hud.SetActive(playing);sheet.SetActive(false);
        var view=session.weapon.GetComponent<WeaponPresentation>();if(view!=null&&view.viewModel!=null)view.viewModel.gameObject.SetActive(playing);
        confirmRestart=confirmQuit=false;restart.GetComponentInChildren<Text>().text="RESTART";quit.GetComponentInChildren<Text>().text="EXIT";
        if(playing)return;
        bool won=session.State==SessionState.Victory,lost=session.State==SessionState.Defeat,paused=session.State==SessionState.Paused;
        heading.text=won?"EXTRACTED":lost?"SIGNAL LOST":paused?"ON HOLD":session.missionName;
        chapter.text=$"CHAPTER {session.missionCode}  /  "+(won?"TRANSMISSION SECURED":lost?"OPERATOR OFFLINE":paused?"OPERATION PAUSED":"A GUNQUEST OPERATION");
        description.text=won?session.victoryDescription:lost?"The signal is lost, but the trail remains.\nRegroup at deployment and try again.":paused?"Your operation is paused. Review the field guide,\nadjust your equipment, or return to the trail.":session.missionDescription;
        primaryLabel.text=won?(session.HasNextMission?"NEXT CHAPTER  >":"REDEPLOY  >"):lost?"TRY AGAIN  >":paused?"RETURN TO THE FIELD  >":"BEGIN OPERATION  >";
        record.text=$"{GameSession.ClearedMissionCount} / 3 CHAPTERS SECURED     BEST {session.BestScore:000000}"+(won?$"\nRANK {session.PerformanceRank}   /   {session.Accuracy:0}% ACCURACY   /   {session.Kills} ELIMINATED":"");
        for(int i=0;i<3;i++)
        {
            chapters[i].interactable=session.State==SessionState.Menu&&i!=session.MissionIndex;
            chapters[i].GetComponent<Image>().color=i==session.MissionIndex?new Color(.35f,.42f,.32f,.45f):new Color(.12f,.15f,.14f,.45f);
            difficulties[i].interactable=session.State==SessionState.Menu;
            difficulties[i].GetComponentInChildren<Text>().color=(int)session.Difficulty==i?Paper:Muted;
        }
        EventSystem.current?.SetSelectedGameObject(primary.gameObject);
    }

    private void Update()
    {
        if(wave==null)return;
        wave.text=$"WAVE {session.Wave:00} / {session.totalWaves:00}     {session.EnemiesRemaining:00} HOSTILES\n<size=11>{session.ThreatSummary}</size>";
        float health=session.player.GetCurrentHealth();vitals.text=$"{health:0} <size=15>/ {session.player.maxHealth:0}</size>";
        healthBar.rectTransform.sizeDelta=new Vector2(205*health/session.player.maxHealth,3);healthBar.color=health<=30?new Color(.8f,.36f,.28f):Accent;
        rounds.text=session.weapon.IsReloading?"<size=23>RELOADING</size>":$"{session.weapon.Ammo.Loaded:00} <size=17>/ {session.weapon.Ammo.Reserve:000}</size>";
        reloadBar.rectTransform.sizeDelta=new Vector2(205*session.weapon.ReloadProgress,3);
        transmission.text=session.Notice;
        requisitions.SetActive(session.CanUpgrade&&session.State==SessionState.Playing);
        intermission.text=$"RESUPPLY / {session.UpgradeCredits} UPGRADE CREDIT(S) / {session.NextWaveIn:0}s\n1 DAMAGE     2 RELOAD     3 VITALITY     ENTER NEXT WAVE";
        upgradeText.text=$"FIELD REQUISITIONS / {session.UpgradeCredits} CREDIT(S)";
        for(int i=0;i<3;i++) { upgrades[i].interactable=session.CanUpgrade&&session.UpgradeCredits>0&&session.UpgradeLevel(i)<3;upgrades[i].GetComponentInChildren<Text>().text=new[]{"DAMAGE","RELOAD","VITALITY"}[i]+$" {session.UpgradeLevel(i)}/3"; }
        optionValues.text=$"FOV {session.Options.FieldOfView:0}  /  {new[]{"PERFORMANCE","BALANCED","ULTRA"}[session.Options.GraphicsPreset]}  /  "+(session.Options.FrameLimit?"60 FPS":"UNLIMITED")+"\nREDUCED MOTION "+(session.Options.ReducedMotion?"ON":"OFF")+"  /  INVERT Y "+(session.Options.InvertY?"ON":"OFF");
        if(Time.unscaledTime>hitUntil)hit.text="";
        damageFade=Mathf.MoveTowards(damageFade,0,Time.deltaTime*1.4f);damageLeft.color=damageRight.color=new Color(.64f,.15f,.09f,damageFade);
        UpdateObjective();
    }
    private void UpdateObjective()
    {
        var o=session.Objectives;if(o==null)return;
        objective.text=o.TargetName;instruction.text=o.Instruction+$"\n{o.Distance:0}m  /  {o.RelaysSecured} OF 3 LINKED";
        objectiveBar.rectTransform.sizeDelta=new Vector2(308*o.Progress,2);
        Vector3 target=o.TargetPosition+Vector3.up*2.4f;var cam=session.weapon.aimCamera;Vector3 screen=cam.WorldToScreenPoint(target);
        var c=stage.GetComponentInParent<Canvas>();RectTransformUtility.ScreenPointToLocalPointInRectangle(stage,screen,c.renderMode==RenderMode.ScreenSpaceOverlay?null:cam,out var local);
        float x=local.x+800,y=450-local.y;bool off=screen.z<=0||x<420||x>1180||y<240||y>700;
        if(screen.z<=0){x=Vector3.Dot(cam.transform.right,target-cam.transform.position)<0?430:1170;y=415;}
        x=Mathf.Clamp(x,430,1170);y=Mathf.Clamp(y,240,690);waypoint.rectTransform.anchoredPosition=new Vector2(x-110,-y);
        waypoint.text=(off?(x<800?"<  ":">  "):"+  ")+$"{o.Distance:0}m";
    }
    private void OnHit(bool kill){hit.text="×";hit.color=kill?new Color(.85f,.63f,.40f):Paper;hitUntil=Time.unscaledTime+.15f;}
    private void OnDamage(float amount)=>damageFade=.75f;
    private void OnDestroy(){if(session==null)return;session.StateChanged-=RefreshState;if(session.player!=null)session.player.Damaged-=OnDamage;if(session.weapon!=null)session.weapon.Hit-=OnHit;}

    private static void Place(RectTransform r,float x,float y,float w,float h)
    {r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
    private RectTransform Panel(string name,Transform parent,float x,float y,float w,float h,Color colour)
    {var go=new GameObject(name,typeof(RectTransform),typeof(Image));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);Place(r,x,y,w,h);go.GetComponent<Image>().color=colour;go.GetComponent<Image>().raycastTarget=false;return r;}
    private Text Label(string value,Transform parent,float x,float y,float w,float h,int size,Color colour,bool condensed=false,TextAnchor align=TextAnchor.UpperLeft)
    {var go=new GameObject("Text",typeof(RectTransform),typeof(Text));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);Place(r,x,y,w,h);var t=go.GetComponent<Text>();t.font=condensed?display:body;t.fontSize=size;t.text=value;t.color=colour;t.alignment=align;t.raycastTarget=false;t.verticalOverflow=VerticalWrapMode.Overflow;return t;}
    private Button Button(string title,Transform parent,float x,float y,float w,float h,UnityEngine.Events.UnityAction click,int size)
    {var r=Panel(title,parent,x,y,w,h,new Color(.12f,.15f,.14f,.6f));var image=r.GetComponent<Image>();image.raycastTarget=true;var button=r.gameObject.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(click);var colors=button.colors;colors.highlightedColor=new Color(1.3f,1.3f,1.3f);colors.selectedColor=new Color(1.2f,1.2f,1.2f);colors.disabledColor=new Color(.75f,.75f,.75f,.6f);button.colors=colors;Label(title,r,15,0,w-25,h,size,Paper,false,TextAnchor.MiddleLeft);return button;}
    private void Slider(string label,Transform parent,float x,float y,float min,float max,float value,UnityEngine.Events.UnityAction<float> change)
    {Label(label,parent,x,y,270,20,12,Muted);var r=Panel(label,parent,x+280,y+8,290,5,new Color(.3f,.36f,.32f));r.GetComponent<Image>().raycastTarget=true;var slider=r.gameObject.AddComponent<Slider>();var handle=Panel("Handle",r,0,-7,10,19,Paper);handle.GetComponent<Image>().raycastTarget=true;slider.handleRect=handle;slider.targetGraphic=handle.GetComponent<Image>();slider.minValue=min;slider.maxValue=max;slider.value=value;slider.onValueChanged.AddListener(change);}
}

public sealed class ExpeditionScrim : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();Rect r=rectTransform.rect;
        float[] stops={0,.54f,1};float[] alpha={.94f,.80f,0};
        for(int i=0;i<3;i++) {Color c=new Color(.014f,.021f,.018f,alpha[i]);vh.AddVert(new Vector3(r.xMin+r.width*stops[i],r.yMin),c,Vector2.zero);vh.AddVert(new Vector3(r.xMin+r.width*stops[i],r.yMax),c,Vector2.one);}
        for(int i=0;i<2;i++){int a=i*2;vh.AddTriangle(a,a+1,a+2);vh.AddTriangle(a+1,a+3,a+2);}
    }
}
