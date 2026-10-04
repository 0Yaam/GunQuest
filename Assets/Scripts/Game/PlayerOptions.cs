using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Persistent player preferences; quality is applied to a runtime copy, never the project asset.</summary>
public sealed class PlayerOptions : MonoBehaviour
{
    public float FieldOfView { get; private set; }
    public bool ReducedMotion { get; private set; }
    public bool InvertY { get; private set; }
    public int GraphicsPreset { get; private set; }
    public bool FrameLimit { get; private set; }
    public bool Fullscreen { get; private set; }
    private RenderPipelineAsset original;
    private UniversalRenderPipelineAsset runtimePipeline;
    private int originalTarget, originalVSync;
    private float originalLodBias;
    private int originalMipmapLimit;
    private bool originalRealtimeReflections;

    private void Awake()
    {
        float storedFov = PlayerPrefs.GetFloat("GunQuest.FOV", 75f);
        FieldOfView = float.IsFinite(storedFov) ? Mathf.Clamp(storedFov, 65f, 100f) : 75f;
        ReducedMotion = PlayerPrefs.GetInt("GunQuest.ReducedMotion", 0) == 1;
        InvertY = PlayerPrefs.GetInt("GunQuest.InvertY", 0) == 1;
        FrameLimit = PlayerPrefs.GetInt("GunQuest.FrameLimit", 1) == 1;
        Fullscreen = PlayerPrefs.GetInt("GunQuest.Fullscreen", 1) == 1;
        if (PlayerPrefs.GetInt("GunQuest.GraphicsRevision", 0) < 1)
        {
            GraphicsPreset = 1;
            PlayerPrefs.SetInt("GunQuest.Graphics", GraphicsPreset);
            PlayerPrefs.SetInt("GunQuest.GraphicsRevision", 1);
        }
        else GraphicsPreset = Mathf.Clamp(PlayerPrefs.GetInt("GunQuest.Graphics", 1), 0, 2);
        original = QualitySettings.renderPipeline;
        originalTarget = Application.targetFrameRate;
        originalVSync = QualitySettings.vSyncCount;
        originalLodBias = QualitySettings.lodBias;
        originalMipmapLimit = QualitySettings.globalTextureMipmapLimit;
        originalRealtimeReflections = QualitySettings.realtimeReflectionProbes;
        var source = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (source != null)
        {
            runtimePipeline = Instantiate(source);
            runtimePipeline.name = "GunQuest runtime graphics";
            QualitySettings.renderPipeline = runtimePipeline;
        }
        ApplyGraphics();
        ApplyFrameLimit();
        ApplyDisplay();
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        bool altEnter = keyboard.enterKey.wasPressedThisFrame &&
            (keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed);
        if (keyboard.f11Key.wasPressedThisFrame || altEnter) ToggleFullscreen();
    }

    public void SetFieldOfView(float value)
    {
        if (!float.IsFinite(value)) return;
        FieldOfView = Mathf.Clamp(value, 65f, 100f);
        PlayerPrefs.SetFloat("GunQuest.FOV", FieldOfView);
    }
    public void ToggleMotion() { ReducedMotion = !ReducedMotion; PlayerPrefs.SetInt("GunQuest.ReducedMotion", ReducedMotion ? 1 : 0); }
    public void ToggleInvert() { InvertY = !InvertY; PlayerPrefs.SetInt("GunQuest.InvertY", InvertY ? 1 : 0); }
    public void ToggleFrameLimit()
    {
        FrameLimit = !FrameLimit;
        PlayerPrefs.SetInt("GunQuest.FrameLimit", FrameLimit ? 1 : 0);
        ApplyFrameLimit();
    }
    public void ToggleFullscreen() => SetFullscreen(!Fullscreen);
    public void SetFullscreen(bool value)
    {
        Fullscreen = value;
        PlayerPrefs.SetInt("GunQuest.Fullscreen", Fullscreen ? 1 : 0);
        ApplyDisplay();
        ApplyGraphics();
    }
    public void SetGraphics(int value)
    {
        GraphicsPreset = Mathf.Clamp(value, 0, 2);
        PlayerPrefs.SetInt("GunQuest.Graphics", GraphicsPreset);
        ApplyGraphics();
    }
    private void ApplyFrameLimit()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = FrameLimit ? 60 : -1;
    }
    private void ApplyGraphics()
    {
        if (runtimePipeline == null) return;
        float baseScale = GraphicsPreset == 0 ? 0.65f : GraphicsPreset == 1 ? 0.85f : 1f;
        float displayScale = DisplayScaleCompensation(GraphicsPreset == 2 ? 2560f * 1440f : 1920f * 1080f);
        runtimePipeline.renderScale = baseScale * displayScale;
        bool upscaling = runtimePipeline.renderScale < 0.999f;
        runtimePipeline.upscalingFilter = upscaling ? UpscalingFilterSelection.FSR : UpscalingFilterSelection.Auto;
        runtimePipeline.fsrOverrideSharpness = upscaling;
        runtimePipeline.fsrSharpness = GraphicsPreset == 2 ? 0.55f : 0.75f;
        // Post-process AA already handles edges; stacking MSAA doubled geometry cost in dense views.
        runtimePipeline.msaaSampleCount = 1;
        runtimePipeline.shadowDistance = GraphicsPreset == 0 ? 28f : GraphicsPreset == 1 ? 45f : 75f;
        runtimePipeline.shadowCascadeCount = GraphicsPreset < 2 ? 2 : 4;
        runtimePipeline.mainLightShadowmapResolution = GraphicsPreset == 0 ? 1024 : GraphicsPreset == 1 ? 2048 : 4096;
        runtimePipeline.additionalLightsShadowmapResolution = GraphicsPreset == 0 ? 512 : GraphicsPreset == 1 ? 1024 : 2048;
        runtimePipeline.maxAdditionalLightsCount = GraphicsPreset == 0 ? 2 : GraphicsPreset == 1 ? 4 : 8;
        runtimePipeline.supportsCameraOpaqueTexture = false;
        runtimePipeline.supportsDynamicBatching = true;

        QualitySettings.lodBias = GraphicsPreset == 0 ? 0.55f : GraphicsPreset == 1 ? 0.9f : 1.5f;
        QualitySettings.globalTextureMipmapLimit = GraphicsPreset == 0 ? 1 : 0;
        QualitySettings.realtimeReflectionProbes = GraphicsPreset == 2;

        var antialiasing = GraphicsPreset == 0 ? AntialiasingMode.None :
            GraphicsPreset == 1 ? AntialiasingMode.FastApproximateAntialiasing : AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        foreach (var cameraData in Object.FindObjectsByType<UniversalAdditionalCameraData>())
        {
            cameraData.antialiasing = antialiasing;
            cameraData.antialiasingQuality = GraphicsPreset == 2 ? AntialiasingQuality.High : AntialiasingQuality.Low;
        }
    }
    private float DisplayScaleCompensation(float targetPixels)
    {
#if !UNITY_EDITOR && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX)
        if (Fullscreen)
        {
            float pixels = (float)Display.main.systemWidth * Display.main.systemHeight;
            if (pixels > 0f) return Mathf.Clamp(Mathf.Sqrt(targetPixels / pixels), 0.4f, 1f);
        }
#endif
        return 1f;
    }
    private void ApplyDisplay()
    {
#if !UNITY_EDITOR && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX)
        int nativeWidth = Display.main.systemWidth > 0 ? Display.main.systemWidth : Screen.width;
        int nativeHeight = Display.main.systemHeight > 0 ? Display.main.systemHeight : Screen.height;
        if (Fullscreen)
        {
            Screen.SetResolution(nativeWidth, nativeHeight, FullScreenMode.FullScreenWindow);
        }
        else
        {
            Screen.SetResolution(Mathf.Min(1600, nativeWidth), Mathf.Min(900, nativeHeight), FullScreenMode.Windowed);
        }
#endif
    }
    public void Save() => PlayerPrefs.Save();
    private void OnApplicationQuit() => Save();
    private void OnDestroy()
    {
        Save();
        if (QualitySettings.renderPipeline == runtimePipeline) QualitySettings.renderPipeline = original;
        if (runtimePipeline != null) Destroy(runtimePipeline);
        Application.targetFrameRate = originalTarget;
        QualitySettings.vSyncCount = originalVSync;
        QualitySettings.lodBias = originalLodBias;
        QualitySettings.globalTextureMipmapLimit = originalMipmapLimit;
        QualitySettings.realtimeReflectionProbes = originalRealtimeReflections;
    }
}
