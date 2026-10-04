using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class CombatFeedback
{
    public static void Warmup() => CombatFeedbackDriver.GetOrCreate();

    public static void Tracer(Vector3 from, Vector3 to, Color color) =>
        CombatFeedbackDriver.GetOrCreate().SpawnTracer(from, to, color);

    public static void Impact(Vector3 position, Vector3 normal, bool enemy) =>
        CombatFeedbackDriver.GetOrCreate().SpawnImpact(position, normal, enemy);

    public static void Clear() => CombatFeedbackDriver.ClearCurrent();
}

/// <summary>Small reusable pools keep automatic fire visually rich without per-shot allocations.</summary>
[DefaultExecutionOrder(-80)]
public sealed class CombatFeedbackDriver : MonoBehaviour
{
    private const int TracerCount = 16;
    private const int SparkCount = 10;
    private const int DecalCount = 24;
    private static CombatFeedbackDriver instance;

    private sealed class TracerSlot
    {
        public LineRenderer line;
        public Vector3 from;
        public Vector3 to;
        public Color color;
        public float started;
        public float duration;
    }

    private sealed class DecalSlot
    {
        public Transform transform;
        public Renderer renderer;
        public float started;
    }

    private readonly TracerSlot[] tracers = new TracerSlot[TracerCount];
    private readonly ParticleSystem[] sparks = new ParticleSystem[SparkCount];
    private readonly DecalSlot[] decals = new DecalSlot[DecalCount];
    private Material tracerMaterial;
    private Material sparkMaterial;
    private Material decalMaterial;
    private MaterialPropertyBlock decalProperties;
    private int tracerCursor;
    private int sparkCursor;
    private int decalCursor;

    public static CombatFeedbackDriver GetOrCreate()
    {
        if (instance != null) return instance;
        instance = Object.FindAnyObjectByType<CombatFeedbackDriver>();
        if (instance != null) return instance;
        var root = new GameObject("Combat feedback pool");
        DontDestroyOnLoad(root);
        instance = root.AddComponent<CombatFeedbackDriver>();
        return instance;
    }

    public static void ClearCurrent()
    {
        if (instance != null) instance.ClearEffects();
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        tracerMaterial = CreateMaterial("CombatTracer", "GunQuest/Combat Tracer", "Universal Render Pipeline/Particles/Unlit");
        sparkMaterial = CreateMaterial("CombatSpark", "GunQuest/Combat Spark", "Universal Render Pipeline/Particles/Unlit");
        decalMaterial = CreateMaterial("ImpactDecal", "GunQuest/Impact Decal", "Universal Render Pipeline/Unlit");
        decalProperties = new MaterialPropertyBlock();
        BuildTracerPool();
        BuildSparkPool();
        BuildDecalPool();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Update()
    {
        float now = Time.unscaledTime;
        for (int i = 0; i < tracers.Length; i++)
        {
            var slot = tracers[i];
            if (!slot.line.enabled) continue;
            float progress = (now - slot.started) / slot.duration;
            if (progress >= 1f) { slot.line.enabled = false; continue; }
            float distance = Vector3.Distance(slot.from, slot.to);
            float headDistance = Mathf.SmoothStep(0f, distance, Mathf.Clamp01(progress * 1.2f));
            float tailDistance = Mathf.Max(0f, headDistance - Mathf.Lerp(2.5f, 9f, Mathf.Clamp01(distance / 80f)));
            Vector3 direction = distance > 0.001f ? (slot.to - slot.from) / distance : Vector3.forward;
            slot.line.SetPosition(0, slot.from + direction * tailDistance);
            slot.line.SetPosition(1, slot.from + direction * headDistance);
            Color fading = slot.color;
            fading.a = 1f - Mathf.SmoothStep(0.55f, 1f, progress);
            slot.line.startColor = fading;
            slot.line.endColor = new Color(fading.r, fading.g, fading.b, fading.a * 0.15f);
        }

        for (int i = 0; i < decals.Length; i++)
        {
            var slot = decals[i];
            if (!slot.renderer.enabled) continue;
            float age = now - slot.started;
            if (age >= 18f) { slot.renderer.enabled = false; continue; }
            decalProperties.SetFloat("_Fade", 1f - Mathf.SmoothStep(12f, 18f, age));
            slot.renderer.SetPropertyBlock(decalProperties);
        }
    }

    public void SpawnTracer(Vector3 from, Vector3 to, Color color)
    {
        var slot = tracers[tracerCursor++ % tracers.Length];
        slot.from = from;
        slot.to = to;
        slot.color = color;
        slot.started = Time.unscaledTime;
        slot.duration = Mathf.Lerp(0.045f, 0.085f, Mathf.Clamp01(Vector3.Distance(from, to) / 120f));
        slot.line.enabled = true;
        slot.line.SetPosition(0, from);
        slot.line.SetPosition(1, from);
    }

    public void SpawnImpact(Vector3 position, Vector3 normal, bool enemy)
    {
        var system = sparks[sparkCursor++ % sparks.Length];
        system.transform.position = position + normal * 0.025f;
        Color color = enemy ? new Color(1f, 0.16f, 0.055f, 1f) : new Color(1f, 0.72f, 0.28f, 1f);
        Vector3 tangent = Vector3.Cross(normal, Mathf.Abs(normal.y) > 0.8f ? Vector3.right : Vector3.up).normalized;
        Vector3 bitangent = Vector3.Cross(normal, tangent);
        int count = enemy ? 12 : 8;
        for (int i = 0; i < count; i++)
        {
            var emit = new ParticleSystem.EmitParams
            {
                position = Vector3.zero,
                velocity = normal * Random.Range(1.2f, 4.8f) + tangent * Random.Range(-2.2f, 2.2f) + bitangent * Random.Range(-2.2f, 2.2f),
                startColor = Color.Lerp(color, Color.white, Random.Range(0f, 0.45f)),
                startLifetime = Random.Range(0.12f, 0.32f),
                startSize = Random.Range(0.018f, 0.055f)
            };
            system.Emit(emit, 1);
        }
        var flash = new ParticleSystem.EmitParams
        {
            position = Vector3.zero,
            velocity = Vector3.zero,
            startColor = color,
            startLifetime = 0.055f,
            startSize = enemy ? 0.19f : 0.13f
        };
        system.Emit(flash, 1);

        if (enemy) return;
        var decal = decals[decalCursor++ % decals.Length];
        decal.transform.position = position + normal * 0.012f;
        decal.transform.rotation = Quaternion.LookRotation(normal) * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        decal.transform.localScale = Vector3.one * Random.Range(0.075f, 0.12f);
        decal.started = Time.unscaledTime;
        decal.renderer.enabled = true;
        decalProperties.SetFloat("_Fade", 1f);
        decal.renderer.SetPropertyBlock(decalProperties);
    }

    public void ClearEffects()
    {
        for (int i = 0; i < tracers.Length; i++)
        {
            if (tracers[i]?.line != null) tracers[i].line.enabled = false;
        }
        for (int i = 0; i < sparks.Length; i++)
        {
            if (sparks[i] == null) continue;
            sparks[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        for (int i = 0; i < decals.Length; i++)
        {
            if (decals[i]?.renderer != null) decals[i].renderer.enabled = false;
        }
        tracerCursor = sparkCursor = decalCursor = 0;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ClearEffects();

    private void BuildTracerPool()
    {
        for (int i = 0; i < tracers.Length; i++)
        {
            var child = new GameObject("Pooled tracer " + i);
            child.transform.SetParent(transform, false);
            var line = child.AddComponent<LineRenderer>();
            line.sharedMaterial = tracerMaterial;
            line.useWorldSpace = true;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.positionCount = 2;
            line.startWidth = 0.026f;
            line.endWidth = 0.008f;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
            tracers[i] = new TracerSlot { line = line };
        }
    }

    private void BuildSparkPool()
    {
        for (int i = 0; i < sparks.Length; i++)
        {
            var child = new GameObject("Pooled impact sparks " + i);
            child.transform.SetParent(transform, false);
            var system = child.AddComponent<ParticleSystem>();
            var main = system.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startSpeed = 0f;
            main.startLifetime = 0.25f;
            main.startSize = 0.04f;
            main.gravityModifier = 0.7f;
            main.maxParticles = 48;
            var emission = system.emission;
            emission.enabled = false;
            var shape = system.shape;
            shape.enabled = false;
            var size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = sparkMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            sparks[i] = system;
        }
    }

    private void BuildDecalPool()
    {
        for (int i = 0; i < decals.Length; i++)
        {
            var child = GameObject.CreatePrimitive(PrimitiveType.Quad);
            child.name = "Pooled impact mark " + i;
            child.transform.SetParent(transform, false);
            var collider = child.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            var renderer = child.GetComponent<Renderer>();
            renderer.sharedMaterial = decalMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.enabled = false;
            decals[i] = new DecalSlot { transform = child.transform, renderer = renderer };
        }
    }

    private static Material CreateMaterial(string resourceName, string shaderName, string fallback)
    {
        var template = Resources.Load<Material>(resourceName);
        if (template != null) return new Material(template) { name = shaderName + " runtime" };
        var shader = Shader.Find(shaderName);
        if (shader == null) shader = Shader.Find(fallback);
        return new Material(shader) { name = shaderName + " runtime" };
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this) instance = null;
        if (tracerMaterial != null) Destroy(tracerMaterial);
        if (sparkMaterial != null) Destroy(sparkMaterial);
        if (decalMaterial != null) Destroy(decalMaterial);
    }
}
