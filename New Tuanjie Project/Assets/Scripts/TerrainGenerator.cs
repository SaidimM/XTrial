using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public static float FinishX => TrialLevels.Current.FinishX;
    Transform artwork;

    void Start() { Rebuild(); }

    public void Rebuild()
    {
        TrialArt.ClearGenerated(artwork);
        artwork = new GameObject("Track artwork").transform;
        artwork.SetParent(transform, false);
        var oldLine = GetComponent<LineRenderer>();
        if (oldLine != null) oldLine.enabled = false;
        int count = Mathf.CeilToInt((FinishX + 40) / .25f) + 1;
        var vertices = new Vector3[count * 2];
        var colors = new Color[vertices.Length];
        var indices = new int[(count - 1) * 6];
        var edge = new Vector2[count];
        for (int i = 0; i < count; i++)
        {
            float x = -20 + i * .25f;
            float y = GetHeight(x);
            edge[i] = new Vector2(x, y);
            vertices[i * 2] = new Vector3(x, y);
            vertices[i * 2 + 1] = new Vector3(x, -20);
            colors[i * 2] = TrialArt.Hex(TrialLevels.Current.GroundTop);
            colors[i * 2 + 1] = TrialArt.Hex(TrialLevels.Current.GroundBottom);
            if (i == count - 1) continue;
            int a = i * 2, t = i * 6;
            indices[t] = a; indices[t + 1] = a + 2; indices[t + 2] = a + 1;
            indices[t + 3] = a + 1; indices[t + 4] = a + 2; indices[t + 5] = a + 3;
        }
        Mesh mesh = new Mesh { name = "Solid terrain", vertices = vertices, colors = colors, triangles = indices };
        mesh.RecalculateBounds();
        artwork.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = artwork.gameObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = TrialArt.Material;
        renderer.sortingOrder = 0;
        TrialArt.Line(artwork, "Sand track", TrialArt.Sand, .16f, 2, edge);
        for (int i = 0; i < Mathf.CeilToInt((FinishX + 25) / 1.1f); i++)
        {
            float x = i * 1.1f - 10;
            float y = GetHeight(x) - .45f - Mathf.PerlinNoise(i * 1.7f, 5) * 2;
            TrialArt.Line(artwork, "Rock grain", TrialArt.Hex(0x617064), .025f, 1,
                new Vector2(x, y), new Vector2(x + .15f + (i % 3) * .1f, y + .025f));
        }
        Marker(TrialLevels.StartX, false);
        Marker(FinishX, true);
        for (int i = 0; i < Mathf.FloorToInt((FinishX - 12) / 7.1f); i++)
        {
            float x = 10 + i * 7.1f, y = GetHeight(x);
            TrialArt.Line(artwork, "Trail post", TrialArt.Sand, .065f, -1, new Vector2(x, y), new Vector2(x, y + .65f));
            TrialArt.Line(artwork, "Trail blaze", TrialArt.Cream, .09f, -1, new Vector2(x, y + .5f), new Vector2(x, y + .6f));
        }
    }

    void Marker(float x, bool finish)
    {
        float y = GetHeight(x);
        TrialArt.Line(artwork, "Flagpole", TrialArt.Ink, .05f, -1, new Vector2(x, y), new Vector2(x, y + 2.1f));
        TrialArt.Polygon(artwork, finish ? "Finish flag" : "Start flag", finish ? TrialArt.Cream : TrialArt.Accent, -1,
            new Vector2(x, y + 2.1f), new Vector2(x + .85f, y + 2.1f), new Vector2(x + .65f, y + 1.6f), new Vector2(x, y + 1.6f));
        if (finish)
            for (int row = 0; row < 2; row++)
                for (int col = 0; col < 3; col++)
                    if ((row + col) % 2 == 0)
                    {
                        float left = x + col * .21f, top = y + 2.1f - row * .25f;
                        TrialArt.Polygon(artwork, "Chequer", TrialArt.Ink, 0,
                            new Vector2(left, top), new Vector2(left + .21f, top), new Vector2(left + .21f, top - .25f), new Vector2(left, top - .25f));
                    }
    }

    public float GetHeight(float x) => TrialLevels.Current.Height(x);
    public float GetSlope(float x) => GetHeight(x + .5f) - GetHeight(x - .5f);
}
