using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Diffuse fill for generated scenes that do not contain baked light probes.</summary>
[DisallowMultipleComponent]
public sealed class EnvironmentLighting : MonoBehaviour
{
    public Color fill = new Color(0.26f, 0.30f, 0.36f);
    public Color sky = new Color(0.16f, 0.21f, 0.28f);

    private void OnEnable() => Apply();
    private void Start() => Apply();

    private void Apply()
    {
        var probe = new SphericalHarmonicsL2();
        probe.AddAmbientLight(fill);
        probe.AddDirectionalLight(Vector3.up, sky, 0.6f);
        RenderSettings.ambientProbe = probe;
    }
}
