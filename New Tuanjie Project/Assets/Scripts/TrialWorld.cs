using UnityEngine;

[DefaultExecutionOrder(-100)]
public class TrialWorld : MonoBehaviour
{
    Transform scenery;
    Transform[] layers;
    Vector3[] origins;

    // Existing scenes containing the original scripts also receive the new environment.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureWorld()
    {
        if (FindObjectOfType<TrialWorld>() == null) new GameObject("Desert environment").AddComponent<TrialWorld>();
    }

    void Awake()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            var obj = new GameObject("Main Camera");
            obj.tag = "MainCamera";
            cam = obj.AddComponent<Camera>();
            obj.AddComponent<AudioListener>();
        }
        cam.orthographic = true;
        cam.orthographicSize = 4.8f;
        cam.backgroundColor = TrialArt.Hex(0xF3D6AF);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.transform.position = new Vector3(5, 3, -10);
        if (FindObjectOfType<TerrainGenerator>() == null) new GameObject("Track").AddComponent<TerrainGenerator>();
        if (FindObjectOfType<BikeController>() == null) new GameObject("Bike").AddComponent<BikeController>();
        if (FindObjectOfType<GameManager>() == null) new GameObject("HUD").AddComponent<GameManager>();
        ApplyLevel();
    }

    public void ApplyLevel()
    {
        TrialArt.ClearGenerated(scenery);
        scenery = new GameObject("Landscape artwork").transform;
        scenery.SetParent(transform, false);
        Camera.main.backgroundColor = TrialArt.Hex(TrialLevels.Current.Sky);
        BuildLandscape();
    }

    void BuildLandscape()
    {
        layers = new Transform[3];
        origins = new Vector3[3];
        Color[] colors = { TrialArt.Hex(TrialLevels.Current.FarMountain), TrialArt.Hex(TrialLevels.Current.MidMountain), TrialArt.Hex(TrialLevels.Current.NearMountain) };
        for (int layer = 0; layer < 3; layer++)
        {
            layers[layer] = new GameObject("Mountain layer " + layer).transform;
            layers[layer].SetParent(scenery, false);
            for (int i = -5; i < Mathf.CeilToInt(TrialLevels.Current.FinishX / 12) + 16; i++)
            {
                float x = i * 12f;
                float peak = 3.8f + Mathf.PerlinNoise(i * .73f + 20 + TrialLevels.CurrentIndex * 13, layer * 3.4f) * 5 - layer * 1.8f;
                TrialArt.Polygon(layers[layer], "Sandstone ridge", colors[layer], -40 + layer,
                    new Vector2(x - 9, -12), new Vector2(x - 9, peak - 4),
                    new Vector2(x - 2, peak - .5f), new Vector2(x, peak),
                    new Vector2(x + 3, peak - .2f), new Vector2(x + 10, peak - 4), new Vector2(x + 10, -12));
            }
            origins[layer] = layers[layer].position;
        }
        TrialArt.Disc(layers[0], "Sun halo", new Vector2(12, 7), 1.55f, TrialArt.Hex(0xEBC69E), -43);
        TrialArt.Disc(layers[0], "Sun", new Vector2(12, 7), 1.15f, TrialArt.Cream, -42);
        for (int i = 0; i < Mathf.CeilToInt(TrialLevels.Current.FinishX / 5.7f) + 5; i++)
        {
            float x = i * 5.7f - 8;
            float y = -1.5f + Mathf.PerlinNoise(i, 3) * 1.3f;
            TrialArt.Line(scenery, "Distant scrub", TrialArt.Hex(0x74877C), .07f, -12,
                new Vector2(x - .2f, y), new Vector2(x, y + .4f), new Vector2(x + .1f, y));
        }
    }

    void LateUpdate()
    {
        if (Camera.main == null || layers == null) return;
        Vector3 cameraPosition = Camera.main.transform.position;
        for (int i = 0; i < layers.Length; i++)
            layers[i].position = origins[i] + new Vector3((cameraPosition.x - 5) * (.85f - i * .17f), (cameraPosition.y - 3) * .65f, 0);
    }
}
