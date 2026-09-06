using UnityEngine;

public static class CombatFeedback
{
    private static Material lineMaterial;

    public static void Tracer(Vector3 from, Vector3 to, Color color)
    {
        if (lineMaterial == null) lineMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        var go = new GameObject("Shot tracer");
        var line = go.AddComponent<LineRenderer>();
        line.sharedMaterial = lineMaterial;
        line.startColor = line.endColor = color;
        line.startWidth = 0.025f;
        line.endWidth = 0.008f;
        line.positionCount = 2;
        line.SetPosition(0, from);
        line.SetPosition(1, to);
        Object.Destroy(go, 0.055f);
    }

    public static void Impact(Vector3 position, Vector3 normal, bool enemy)
    {
        Tracer(position + normal * 0.03f, position + normal * (enemy ? 0.45f : 0.2f), enemy ? new Color(1f, 0.35f, 0.2f) : Color.white);
    }
}
