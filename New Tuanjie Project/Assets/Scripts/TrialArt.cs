using UnityEngine;

// Small vector drawing toolkit. Shared materials keep the procedural artwork inexpensive.
public static class TrialArt
{
    static Material material;
    public static readonly Color Ink = Hex(0x24363B);
    public static readonly Color Sand = Hex(0xDBA078);
    public static readonly Color Cream = Hex(0xFFF0D1);
    public static readonly Color Accent = Hex(0xE77D51);
    public static Color Hex(uint rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);
    public static Material Material
    {
        get
        {
            if (material == null) material = new Material(Shader.Find("Sprites/Default"));
            return material;
        }
    }

    // Rebuilding a course also releases its runtime meshes.
    public static void ClearGenerated(Transform root)
    {
        if (root == null) return;
        root.gameObject.SetActive(false);
        foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            if (filter.sharedMesh != null) Object.Destroy(filter.sharedMesh);
        Object.Destroy(root.gameObject);
    }

    public static Transform Polygon(Transform parent, string name, Color color, int order, params Vector2[] points)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var vertices = new Vector3[points.Length];
        var colors = new Color[points.Length];
        var triangles = new int[(points.Length - 2) * 3];
        for (int i = 0; i < points.Length; i++) { vertices[i] = points[i]; colors[i] = color; }
        for (int i = 0; i < points.Length - 2; i++)
        { triangles[i * 3] = 0; triangles[i * 3 + 1] = i + 1; triangles[i * 3 + 2] = i + 2; }
        Mesh mesh = new Mesh { name = name, vertices = vertices, colors = colors, triangles = triangles };
        mesh.RecalculateBounds();
        obj.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = obj.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = Material;
        renderer.sortingOrder = order;
        return obj.transform;
    }

    public static Transform Disc(Transform parent, string name, Vector2 center, float radius, Color color, int order)
    {
        var points = new Vector2[48];
        for (int i = 0; i < points.Length; i++)
        {
            float a = i * Mathf.PI * 2 / points.Length;
            points[i] = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
        }
        return Polygon(parent, name, color, order, points);
    }

    public static LineRenderer Line(Transform parent, string name, Color color, float width, int order, params Vector2[] points)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var line = obj.AddComponent<LineRenderer>();
        line.sharedMaterial = Material;
        line.useWorldSpace = false;
        line.startColor = line.endColor = color;
        line.startWidth = line.endWidth = width;
        line.numCapVertices = 5;
        line.numCornerVertices = 4;
        line.sortingOrder = order;
        line.positionCount = points.Length;
        for (int i = 0; i < points.Length; i++) line.SetPosition(i, points[i]);
        return line;
    }
}
