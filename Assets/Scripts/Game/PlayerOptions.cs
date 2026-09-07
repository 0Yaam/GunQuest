using UnityEngine;
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
    private RenderPipelineAsset original;
    private UniversalRenderPipelineAsset runtimePipeline;
    private int originalTarget, originalVSync;

    private void Awake()
    {
        FieldOfView = Mathf.Clamp(PlayerPrefs.GetFloat("GunQuest.FOV", 75f), 65f, 100f);
        ReducedMotion = PlayerPrefs.GetInt("GunQuest.ReducedMotion", 0) == 1;
        InvertY = PlayerPrefs.GetInt("GunQuest.InvertY", 0) == 1;
        FrameLimit = PlayerPrefs.GetInt("GunQuest.FrameLimit", 1) == 1;
        GraphicsPreset = Mathf.Clamp(PlayerPrefs.GetInt("GunQuest.Graphics", 2), 0, 2);
        original = QualitySettings.renderPipeline;
        originalTarget = Application.targetFrameRate;
        originalVSync = QualitySettings.vSyncCount;
        var source = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (source != null)
        {
            runtimePipeline = Instantiate(source);
            runtimePipeline.name = "GunQuest runtime graphics";
            QualitySettings.renderPipeline = runtimePipeline;
        }
        ApplyGraphics();
        ApplyFrameLimit();
    }

    public void SetFieldOfView(float value)
    {
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
        runtimePipeline.renderScale = GraphicsPreset == 0 ? 0.75f : GraphicsPreset == 1 ? 1f : 1.15f;
        runtimePipeline.msaaSampleCount = GraphicsPreset == 0 ? 2 : 4;
        runtimePipeline.shadowDistance = GraphicsPreset == 0 ? 35f : GraphicsPreset == 1 ? 65f : 100f;
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
    }
}
