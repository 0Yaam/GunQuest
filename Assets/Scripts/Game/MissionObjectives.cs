using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using GunQuest.Game;

/// <summary>Sequential, spatial mission goals. Combat alone cannot complete an operation.</summary>
public sealed class MissionObjectives : MonoBehaviour
{
    public const int RelayCount = 3;
    public int RelaysSecured { get; private set; }
    public bool Uploading { get; private set; }
    public bool IsComplete { get; private set; }
    public float Progress { get; private set; }
    public bool Extracting => RelaysSecured == RelayCount && session.Wave >= session.totalWaves && session.EnemiesRemaining == 0;
    public int RequiredWave => Mathf.Min(session.totalWaves, 1 + RelaysSecured * 2);
    public bool Unlocked => RelaysSecured < RelayCount && session.Wave >= RequiredWave;
    public Vector3 TargetPosition => RelaysSecured < RelayCount ? relayPositions[RelaysSecured] : extractionPosition;
    public float Distance => Vector3.Distance(session.player.transform.position, TargetPosition);
    public string TargetName => RelaysSecured < RelayCount ? $"RELAY {RelaysSecured + 1} / {RelayCount}" : "EXTRACTION";
    public string Instruction
    {
        get
        {
            if (IsComplete) return "OPERATOR EXTRACTED";
            if (Extracting) return Distance <= 3.5f ? "HOLD POSITION / extraction in progress" : "RETURN TO THE EXTRACTION BEACON";
            if (RelaysSecured == RelayCount) return "UPLINK SECURE / eliminate remaining hostiles";
            if (!Unlocked) return $"RELAY LOCKED / available in wave {RequiredWave}";
            if (Uploading) return "UPLOADING / stay within the marked area";
            if (Distance > 3.2f) return "REACH THE MARKED RELAY";
            if (Contested()) return "AREA CONTESTED / clear nearby hostiles";
            return "E / X BUTTON / establish uplink";
        }
    }

    private GameSession session;
    private readonly Vector3[] relayPositions = new Vector3[RelayCount];
    private readonly Transform[] relayVisuals = new Transform[RelayCount];
    private readonly Renderer[] screens = new Renderer[RelayCount];
    private Vector3 extractionPosition;
    private Transform extractionVisual;
    private Material casing, active, complete, dormant;
    private float progressSeconds;

    public void Initialize(GameSession owner)
    {
        session = owner;
        casing = Material("Relay casing", new Color(0.055f, 0.08f, 0.10f), false);
        active = Material("Active objective", owner.missionAccent, true);
        complete = Material("Secured objective", new Color(0.25f, 0.85f, 0.40f), true);
        dormant = Material("Inactive objective", new Color(0.18f, 0.24f, 0.28f), false);
        float side = owner.MissionIndex == 1 ? -1f : 1f;
        Vector3[] locations = { new Vector3(-4f * side, 0, -9), new Vector3(4f * side, 0, 7), new Vector3(-4f * side, 0, 21) };
        for (int i = 0; i < RelayCount; i++)
        {
            relayPositions[i] = ReachablePosition(locations[i]);
            var root = new GameObject("Mission relay " + (i + 1)).transform;
            root.SetParent(transform);
            root.position = relayPositions[i];
            relayVisuals[i] = root;
            Part(root, PrimitiveType.Cylinder, new Vector3(0, 0.07f, 0), new Vector3(1.5f, 0.07f, 1.5f), casing);
            Part(root, PrimitiveType.Cube, new Vector3(0, 0.75f, 0), new Vector3(0.65f, 1.4f, 0.48f), casing);
            screens[i] = Part(root, PrimitiveType.Cube, new Vector3(0, 1.34f, -0.25f), new Vector3(0.51f, 0.30f, 0.035f), dormant);
            Part(root, PrimitiveType.Cylinder, new Vector3(0.24f, 1.94f, 0), new Vector3(0.035f, 0.54f, 0.035f), casing);
            Part(root, PrimitiveType.Sphere, new Vector3(0.24f, 2.51f, 0), Vector3.one * 0.13f, active);
            Ring(root, 2.5f, active);
        }
        extractionPosition = ReachablePosition(owner.player.transform.position);
        extractionVisual = new GameObject("Extraction beacon").transform;
        extractionVisual.SetParent(transform);
        extractionVisual.position = extractionPosition;
        Ring(extractionVisual, 3.2f, complete);
        for (int i = 0; i < 4; i++)
        {
            float angle = i * Mathf.PI * 0.5f;
            Part(extractionVisual, PrimitiveType.Cylinder, new Vector3(Mathf.Cos(angle) * 3.2f, 0.2f, Mathf.Sin(angle) * 3.2f), new Vector3(0.18f, 0.2f, 0.18f), complete);
        }
        extractionVisual.gameObject.SetActive(false);
    }

    public Vector3 RelayPosition(int index) => relayPositions[Mathf.Clamp(index, 0, RelayCount - 1)];

    private Vector3 ReachablePosition(Vector3 requested)
    {
        if (NavMesh.SamplePosition(requested, out var hit, 5f, NavMesh.AllAreas) &&
            NavMesh.SamplePosition(session.player.transform.position, out var start, 4f, NavMesh.AllAreas))
        {
            var path = new NavMeshPath();
            if (NavMesh.CalculatePath(start.position, hit.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete) return hit.position;
        }
        Debug.LogError("Mission objective has no complete navigation route: " + requested);
        return requested;
    }

    public bool TryBeginUpload()
    {
        if (session == null || session.State != SessionState.Playing || !Unlocked || Uploading || Distance > 3.2f || Contested()) return false;
        Uploading = true;
        progressSeconds = Progress = 0f;
        session.Announce("UPLINK CONNECTED / Hold this position for 5 seconds");
        return true;
    }

    private void Update()
    {
        if (session == null) return;
        for (int i = 0; i < RelayCount; i++)
        {
            screens[i].sharedMaterial = i < RelaysSecured ? complete : i == RelaysSecured && Unlocked ? active : dormant;
            relayVisuals[i].GetComponentInChildren<LineRenderer>().enabled = i == RelaysSecured && Unlocked;
        }
        extractionVisual.gameObject.SetActive(Extracting);
        if (session.State != SessionState.Playing || IsComplete) return;
        if ((Keyboard.current?.eKey.wasPressedThisFrame ?? false) || (Gamepad.current?.buttonWest.wasPressedThisFrame ?? false)) TryBeginUpload();
        if (Uploading)
        {
            if (Distance > 3.2f || Contested())
            {
                Uploading = false;
                progressSeconds = Progress = 0;
                session.Announce("UPLINK INTERRUPTED / Clear the area and reconnect");
                return;
            }
            progressSeconds += Time.deltaTime;
            Progress = Mathf.Clamp01(progressSeconds / 5f);
            if (Progress >= 1f)
            {
                RelaysSecured++;
                Uploading = false;
                progressSeconds = Progress = 0;
                session.Announce($"RELAY SECURED / {RelaysSecured} of {RelayCount} linked");
            }
        }
        else if (Extracting)
        {
            progressSeconds = Distance <= 3.5f ? progressSeconds + Time.deltaTime : 0f;
            Progress = Mathf.Clamp01(progressSeconds / 5f);
            if (Progress >= 1f) { IsComplete = true; session.CompleteExtraction(); }
        }
    }

    private bool Contested()
    {
        foreach (var hit in Physics.OverlapSphere(TargetPosition, 5f, ~0, QueryTriggerInteraction.Ignore))
        {
            var enemy = hit.GetComponentInParent<EnemyHealth>();
            if (enemy != null && !enemy.IsDead) return true;
        }
        return false;
    }

    private static Material Material(string label, Color color, bool emissive)
    {
        var result = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = label };
        result.SetColor("_BaseColor", color);
        result.SetFloat("_Metallic", emissive ? 0.15f : 0.65f);
        result.SetFloat("_Smoothness", 0.5f);
        if (emissive) { result.EnableKeyword("_EMISSION"); result.SetColor("_EmissionColor", color * 2f); }
        return result;
    }

    private static Renderer Part(Transform root, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
    {
        var go = GameObject.CreatePrimitive(type);
        go.transform.SetParent(root, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        // Markers must not introduce unbaked navigation obstacles or block rifle rays.
        var collider = go.GetComponent<Collider>();
        collider.enabled = false;
        Destroy(collider);
        var renderer = go.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        return renderer;
    }

    private static void Ring(Transform root, float radius, Material material)
    {
        var go = new GameObject("Objective perimeter");
        go.transform.SetParent(root, false);
        var ring = go.AddComponent<LineRenderer>();
        ring.useWorldSpace = false;
        ring.loop = true;
        ring.widthMultiplier = 0.055f;
        ring.sharedMaterial = material;
        ring.positionCount = 64;
        for (int i = 0; i < 64; i++)
        {
            float angle = i / 64f * Mathf.PI * 2;
            ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0.08f, Mathf.Sin(angle) * radius));
        }
    }

    private void OnDestroy()
    {
        if (casing != null) Destroy(casing);
        if (active != null) Destroy(active);
        if (complete != null) Destroy(complete);
        if (dormant != null) Destroy(dormant);
    }
}
