using UnityEngine;

// Track IDs are permanent save keys; display names and ordering can change safely.
public sealed class TrialLevel
{
    public readonly string Id, Name, Difficulty, Description;
    public readonly float FinishX;
    public readonly Vector2[] Points;
    public readonly uint Sky, FarMountain, MidMountain, NearMountain, GroundTop, GroundBottom;

    public TrialLevel(string id, string name, string difficulty, string description, float finish,
        Vector2[] points, uint sky, uint far, uint mid, uint near, uint groundTop, uint groundBottom)
    {
        Id = id; Name = name; Difficulty = difficulty; Description = description;
        FinishX = finish; Points = points;
        Sky = sky; FarMountain = far; MidMountain = mid; NearMountain = near;
        GroundTop = groundTop; GroundBottom = groundBottom;
    }

    public float Height(float x)
    {
        if (x <= Points[0].x) return Points[0].y;
        for (int i = 0; i < Points.Length - 1; i++)
        {
            if (x > Points[i + 1].x) continue;
            float t = (x - Points[i].x) / (Points[i + 1].x - Points[i].x);
            t = t * t * (3 - 2 * t);
            return Points[i].y + (Points[i + 1].y - Points[i].y) * t;
        }
        return Points[Points.Length - 1].y;
    }
}

public static class TrialLevels
{
    public const float StartX = 1.5f;
    public static readonly TrialLevel[] All = {
        new TrialLevel("quiet-canyon", "Quiet Canyon", "INTRODUCTION", "Find your balance on the original canyon trail.", 56,
            Points(0,0, 5,0, 8,3, 12,3, 14,0, 26,0, 30,4, 34,0, 38,0, 40,2, 44,2, 46,0, 62,0),
            0xF3D6AF, 0xDDB799, 0xBC9F89, 0x87948B, 0x47544A, 0x253B38),
        new TrialLevel("rolling-hills", "Rolling Hills", "FLOW", "Gentle rollers, small takeoffs and room to land.", 76,
            Points(0,0, 6,0, 12,1.4f, 17,0, 23,2, 28,.3f, 34,2.5f, 39,.2f, 45,1.8f, 50,0, 56,2.6f, 61,.4f, 67,1.2f, 71,0, 82,0),
            0xDCE6CE, 0xB7CBB5, 0x96B0A1, 0x728F83, 0x4D6552, 0x2C443C),
        new TrialLevel("rock-garden", "Rock Garden", "PRECISION", "Ease off the throttle. Work through the short rock steps.", 66,
            Points(0,0, 6,0, 9,.7f, 11,.7f, 14,0, 17,1.1f, 19,1.1f, 22,.2f, 25,1.6f, 27,1.6f, 30,.3f, 34,.9f, 37,.2f, 41,1.8f, 44,1.8f, 48,.4f, 52,1.2f, 55,.2f, 60,0, 72,0),
            0xDCD7CD, 0xBDB8AE, 0xA3A69D, 0x7F9290, 0x53615E, 0x303F40),
        new TrialLevel("broken-ridge", "Broken Ridge", "AIRTIME", "Build speed on the ramps. Settle the bike before landing.", 88,
            Points(0,0, 7,0, 14,2.8f, 17,2.8f, 20,-.6f, 27,-.6f, 34,3.2f, 37,3.2f, 40,-1, 48,-1, 55,3.8f, 58,3.8f, 62,0, 69,0, 75,2.5f, 78,2.5f, 82,0, 94,0),
            0xEDC3AA, 0xD3A18F, 0xB58A82, 0x8C8682, 0x64544E, 0x3C3D3D),
        new TrialLevel("summit-run", "Summit Run", "ENDURANCE", "Climb the high trail, then balance across the summit ridges.", 104,
            Points(0,0, 6,0, 13,2.4f, 18,2.4f, 25,5, 30,4, 38,7, 43,7, 50,9.5f, 55,8.2f, 63,11.5f, 69,11.5f, 75,9, 81,10.5f, 87,8, 94,9.5f, 99,9.5f, 110,9.5f),
            0xD2DFE5, 0xB1C4D1, 0x92A9B8, 0x718D9B, 0x4C6067, 0x2D414B)
    };

    public static int CurrentIndex { get; private set; }
    public static TrialLevel Current => All[CurrentIndex];
    public static int UnlockedCount => Mathf.Clamp(PlayerPrefs.GetInt("trialx.unlocked", 1), 1, All.Length);
    public static bool IsUnlocked(int index) => index >= 0 && index < UnlockedCount;
    public static float BestTime(int index) => PlayerPrefs.GetFloat("trialx.best." + All[index].Id, 0);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSession() { CurrentIndex = 0; }

    public static bool Select(int index)
    {
        if (!IsUnlocked(index)) return false;
        CurrentIndex = index;
        return true;
    }

    public static bool Complete(int index, float seconds)
    {
        if (!IsUnlocked(index) || seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return false;
        float best = BestTime(index);
        bool improved = best <= 0 || seconds < best;
        if (improved) PlayerPrefs.SetFloat("trialx.best." + All[index].Id, seconds);
        PlayerPrefs.SetInt("trialx.unlocked", Mathf.Max(UnlockedCount, Mathf.Min(index + 2, All.Length)));
        PlayerPrefs.Save();
        return improved;
    }

    static Vector2[] Points(params float[] coordinates)
    {
        var points = new Vector2[coordinates.Length / 2];
        for (int i = 0; i < points.Length; i++) points[i] = new Vector2(coordinates[i * 2], coordinates[i * 2 + 1]);
        return points;
    }
}
