using UnityEngine;

[DefaultExecutionOrder(-20)]
public class GameManager : MonoBehaviour
{
    BikeController bike;
    GUIStyle label, number, button;
    Texture2D panel;
    float elapsed;
    int attempts = 1;
    bool paused;
    bool selectingLevel, pausedBeforeSelector, resultSaved, newBest;
    public bool IsSelectingLevel => selectingLevel;
    Color white = TrialArt.Cream;
    Color muted = new Color(.82f, .86f, .81f);

    void Start()
    {
        bike = FindObjectOfType<BikeController>();
        OpenLevelSelector();
    }

    void Update()
    {
        if (bike == null) bike = FindObjectOfType<BikeController>();
        UpdateTouchInput();
        if (Input.GetKeyDown(KeyCode.M))
        {
            if (selectingLevel) CloseLevelSelector(); else OpenLevelSelector();
            return;
        }
        if (selectingLevel)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) CloseLevelSelector();
            return;
        }
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)) TogglePause();
        if (bike != null && !paused && !bike.IsCrashed() && !bike.IsFinished()) elapsed += Time.deltaTime;
    }

    void InitStyles()
    {
        if (label != null) return;
        label = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
        label.normal.textColor = white;
        number = new GUIStyle(label) { fontSize = 31, fontStyle = FontStyle.Normal };
        button = new GUIStyle(GUI.skin.button) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        panel = new Texture2D(1, 1);
        panel.SetPixel(0, 0, new Color(.09f, .16f, .16f, .93f));
        panel.Apply();
        button.normal.background = panel;
        button.hover.background = panel;
        button.active.background = Texture2D.whiteTexture;
        button.normal.textColor = button.hover.textColor = white;
        button.active.textColor = TrialArt.Ink;
        button.border = new RectOffset();
    }

    void OnGUI()
    {
        if (bike == null) return;
        InitStyles();
        Matrix4x4 oldMatrix = GUI.matrix;
        Color oldColor = GUI.color;
        // Uniform scaling keeps text and controls proportionate on every aspect ratio.
        float scale = Screen.height / 900f;
        float w = Screen.width / scale;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
        Rect safe = Screen.safeArea;
        float left = Mathf.Max(28, safe.xMin / scale + 16);
        float right = Mathf.Min(w - 28, safe.xMax / scale - 16);
        float top = Mathf.Max(28, (Screen.height - safe.yMax) / scale + 16);
        float bottom = Mathf.Min(872, (Screen.height - safe.yMin) / scale - 16);
        if (selectingLevel)
        {
            DrawLevelSelector(w, left, right);
            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
            return;
        }
        DrawPanel(new Rect(left, top, 258, 80));
        Text(new Rect(left + 20, top + 10, 210, 32), "TRIAL / X", 25, white);
        Text(new Rect(left + 20, top + 43, 220, 22), (TrialLevels.CurrentIndex + 1).ToString("00") + "   /   " + TrialLevels.Current.Name.ToUpperInvariant(), 11, muted);
        if (GUI.Button(new Rect(right - 104, top, 104, 42), paused ? "RESUME" : "PAUSE  II", button)) TogglePause();
        if (GUI.Button(new Rect(right - 104, top + 48, 104, 32), "RETRY  /  R", button)) Restart();

        if (GUI.Button(new Rect(right - 104, top + 86, 104, 32), "TRACKS / M", button)) OpenLevelSelector();

        float statsX = left;
        float statsY = top + 100;
        DrawPanel(new Rect(statsX, statsY, 258, 80));
        Text(new Rect(statsX + 20, statsY + 7, 110, 18), "SPEED / KM/H", 10, muted);
        GUI.Label(new Rect(statsX + 20, statsY + 25, 100, 45), bike.GetSpeed().ToString("00"), number);
        Text(new Rect(statsX + 138, statsY + 7, 110, 18), "RIDE TIME", 10, muted);
        Text(new Rect(statsX + 138, statsY + 26, 110, 40), FormatTime(elapsed), 20, white);

        float progress = Mathf.InverseLerp(TrialLevels.StartX, TerrainGenerator.FinishX, bike.GetDistance());
        float barWidth = Mathf.Min(320, right - left - 310);
        if (barWidth > 100)
        {
            float barX = (left + right - barWidth) * .5f;
            Text(new Rect(barX, top, barWidth, 22), "TRAIL PROGRESS                    " + Mathf.RoundToInt(progress * 100) + "%", 11, TrialArt.Ink);
            Fill(new Rect(barX, top + 30, barWidth, 3), new Color(.14f, .21f, .23f, .18f));
            Fill(new Rect(barX, top + 30, barWidth * progress, 3), TrialArt.Ink);
            Fill(new Rect(barX + barWidth * progress - 3, top + 27, 6, 9), TrialArt.Accent);
        }

        bool ended = bike.IsCrashed() || bike.IsFinished();
        bool touch = Application.isMobilePlatform || Input.touchSupported;
        if (touch && !paused && !ended)
        {
            Rect[] controls = TouchRects();
            TouchButton(controls[0], "LEAN\nBACK", bike.touchLeanBack);
            TouchButton(controls[1], "LEAN\nFORWARD", bike.touchLeanForward);
            TouchButton(controls[2], "BRAKE", bike.touchBrake);
            TouchButton(controls[3], "THROTTLE", bike.touchGas);
        }
        else
        {
            bike.touchGas = bike.touchBrake = bike.touchLeanBack = bike.touchLeanForward = false;
            Text(new Rect(left, bottom - 28, right - left, 28), "W / UP   THROTTLE      S / DOWN   BRAKE      A D / LEFT RIGHT   BALANCE", 11, white);
            Text(new Rect(left, bottom - 53, right - left, 22), "TAKE YOUR TIME. FIND YOUR BALANCE.", 10, new Color(.85f, .87f, .78f, .6f));
        }
        if ((paused || ended) && !selectingLevel)
        {
            Fill(new Rect(0, 0, w, 900), new Color(.07f, .13f, .14f, .42f));
            float width = Mathf.Min(480, right - left);
            Rect card = new Rect((w - width) / 2, 280, width, bike.IsFinished() ? 390 : 350);
            DrawPanel(card);
            Fill(new Rect(card.x, card.y, width, 4), TrialArt.Accent);
            Text(new Rect(card.x + 32, card.y + 30, width - 64, 22), paused ? "A MOMENT TO BREATHE" : bike.IsFinished() ? (TrialLevels.CurrentIndex + 1).ToString("00") + " / TRAIL COMPLETE" : "ATTEMPT " + attempts.ToString("00"), 11, muted);
            Text(new Rect(card.x + 32, card.y + 68, width - 64, 60), paused ? "Ride paused." : bike.IsFinished() ? "Nicely ridden." : "Dust yourself off.", 34, white);
            Text(new Rect(card.x + 32, card.y + 136, width - 64, 40), bike.IsFinished() ? "TIME  " + FormatTime(elapsed) + "     /     ATTEMPTS  " + attempts : "Small movements. A little balance. One more go.", 12, muted);
            if (bike.IsFinished())
            {
                Text(new Rect(card.x + 32, card.y + 177, width - 64, 24),
                    (newBest ? "PERSONAL BEST  /  " : "BEST  /  ") + FormatTime(TrialLevels.BestTime(TrialLevels.CurrentIndex)), 12, TrialArt.Sand);
            }
            if (GUI.Button(new Rect(card.x + 32, card.y + 210, width - 64, 52), paused ? "CONTINUE THE RIDE" : "RIDE AGAIN   /   R", button))
            {
                if (paused) TogglePause(); else Restart();
            }
            bool hasNext = bike.IsFinished() && TrialLevels.CurrentIndex + 1 < TrialLevels.All.Length;
            if (GUI.Button(new Rect(card.x + 32, card.y + 274, width - 64, 44), hasNext ? "NEXT TRAIL  >" : "CHOOSE A TRAIL", button))
            {
                if (hasNext) StartLevel(TrialLevels.CurrentIndex + 1); else OpenLevelSelector();
            }
            if (bike.IsFinished())
                Text(new Rect(card.x + 32, card.y + 330, width - 64, 28), hasNext ? "Next trail unlocked. Keep exploring." : "All five trails unlocked. Chase your best times.", 12, muted);
        }
        GUI.matrix = oldMatrix;
        GUI.color = oldColor;
    }

    void OpenLevelSelector()
    {
        if (selectingLevel) return;
        pausedBeforeSelector = paused;
        selectingLevel = true;
        Time.timeScale = 0;
        if (bike != null) bike.touchGas = bike.touchBrake = bike.touchLeanBack = bike.touchLeanForward = false;
    }

    void CloseLevelSelector()
    {
        selectingLevel = false;
        paused = pausedBeforeSelector;
        Time.timeScale = paused ? 0 : 1;
    }

    void StartLevel(int index)
    {
        if (bike == null || !TrialLevels.Select(index)) return;
        FindObjectOfType<TerrainGenerator>().Rebuild();
        FindObjectOfType<TrialWorld>().ApplyLevel();
        selectingLevel = paused = pausedBeforeSelector = false;
        resultSaved = newBest = false;
        attempts = 1;
        elapsed = 0;
        bike.ResetBike();
        Time.timeScale = 1;
    }

    public void CompleteRun()
    {
        if (resultSaved || bike == null || !bike.IsFinished()) return;
        newBest = TrialLevels.Complete(TrialLevels.CurrentIndex, elapsed);
        resultSaved = true;
    }

    void DrawLevelSelector(float w, float left, float right)
    {
        Fill(new Rect(0, 0, w, 900), new Color(.07f, .13f, .14f, .7f));
        float width = Mathf.Min(940, right - left);
        float x = (w - width) / 2;
        float y = 82;
        DrawPanel(new Rect(x, y, width, 736));
        Fill(new Rect(x, y, width, 4), TrialArt.Accent);
        Text(new Rect(x + 24, y + 20, width - 48, 22), "TRIAL / X     —     THE FIRST FIVE", 12, TrialArt.Sand);
        Text(new Rect(x + 24, y + 47, width - 48, 48), "Choose your next trail.", width < 550 ? 25 : 34, white);
        Text(new Rect(x + 24, y + 94, width - 48, 22), "Finish each trail to unlock the next. Your best rides are saved.", 12, muted);
        for (int i = 0; i < TrialLevels.All.Length; i++)
        {
            TrialLevel level = TrialLevels.All[i];
            float rowY = y + 133 + i * 104;
            float rowWidth = width - 48;
            bool unlocked = TrialLevels.IsUnlocked(i);
            Fill(new Rect(x + 24, rowY, rowWidth, 96), new Color(.3f, .4f, .38f, unlocked ? .25f : .1f));
            Text(new Rect(x + 38, rowY + 5, rowWidth - 115, 20), (i + 1).ToString("00") + " / " + level.Difficulty + " / " + level.FinishX.ToString("0") + " M", 10, muted);
            Text(new Rect(x + 38, rowY + 25, rowWidth - 115, 32), level.Name, width < 550 ? 18 : 23, unlocked ? white : muted);
            float best = TrialLevels.BestTime(i);
            string detail = !unlocked ? "Complete " + TrialLevels.All[i - 1].Name + " to unlock" : best > 0 ? "BEST  " + FormatTime(best) : level.Description;
            Text(new Rect(x + 38, rowY + 63, width > 720 ? width * .45f : rowWidth - 115, 24), detail, 11, muted);
            if (width > 720) DrawProfile(new Rect(x + width * .52f, rowY + 14, width * .28f, 64), level, unlocked);
            bool wasEnabled = GUI.enabled;
            GUI.enabled = unlocked;
            if (GUI.Button(new Rect(x + width - 126, rowY + 26, 88, 44), unlocked ? "RIDE  >" : "LOCKED", button)) StartLevel(i);
            GUI.enabled = wasEnabled;
        }
        if (GUI.Button(new Rect(x + 24, y + 674, width - 48, 40), "BACK TO RIDE   /   M", button)) CloseLevelSelector();
    }

    void DrawProfile(Rect rect, TrialLevel level, bool unlocked)
    {
        float min = 0, max = 1;
        foreach (var point in level.Points) { min = Mathf.Min(min, point.y); max = Mathf.Max(max, point.y); }
        const int samples = 80;
        float dx = rect.width / samples;
        for (int j = 0; j < samples; j++)
        {
            float height = level.Height(j * level.FinishX / (samples - 1));
            float y = rect.yMax - 6 - (height - min) / (max - min + 1) * (rect.height - 12);
            Fill(new Rect(rect.x + j * dx, y, dx + .5f, rect.yMax - y), new Color(.55f, .65f, .58f, unlocked ? .3f : .1f));
            Fill(new Rect(rect.x + j * dx, y, dx + .5f, 2), unlocked ? TrialArt.Sand : new Color(.5f, .55f, .5f, .3f));
        }
    }

    Rect[] TouchRects()
    {
        float scale = Screen.height / 900f;
        Rect safe = Screen.safeArea;
        float left = Mathf.Max(28, safe.xMin / scale + 16);
        float right = Mathf.Min(Screen.width / scale - 28, safe.xMax / scale - 16);
        float bottom = Mathf.Min(872, (Screen.height - safe.yMin) / scale - 16);
        float size = Mathf.Min(88, (right - left - 48) / 4);
        float y = bottom - size;
        return new[] { new Rect(left, y, size, size), new Rect(left + size + 12, y, size, size),
            new Rect(right - size * 2 - 12, y, size, size), new Rect(right - size, y, size, size) };
    }

    void UpdateTouchInput()
    {
        if (bike == null) return;
        bike.touchGas = bike.touchBrake = bike.touchLeanBack = bike.touchLeanForward = false;
        if (selectingLevel || paused || bike.IsCrashed() || bike.IsFinished() || !(Application.isMobilePlatform || Input.touchSupported)) return;
        Rect[] controls = TouchRects();
        float scale = Screen.height / 900f;
        // Read each finger independently so throttle and balance work together.
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) continue;
            Vector2 point = new Vector2(touch.position.x, Screen.height - touch.position.y) / scale;
            bike.touchLeanBack |= controls[0].Contains(point);
            bike.touchLeanForward |= controls[1].Contains(point);
            bike.touchBrake |= controls[2].Contains(point);
            bike.touchGas |= controls[3].Contains(point);
        }
    }

    void TouchButton(Rect rect, string text, bool held)
    {
        Color before = GUI.backgroundColor;
        GUI.backgroundColor = held ? TrialArt.Accent : Color.white;
        GUI.Box(rect, text, button);
        GUI.backgroundColor = before;
    }

    void DrawPanel(Rect rect) { GUI.DrawTexture(rect, panel); }
    void Fill(Rect rect, Color color)
    {
        Color before = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = before;
    }
    void Text(Rect rect, string text, int size, Color color)
    {
        label.fontSize = size;
        label.normal.textColor = color;
        GUI.Label(rect, text, label);
    }
    static string FormatTime(float time)
    {
        int hundredths = Mathf.FloorToInt(time * 100);
        return (hundredths / 6000).ToString("00") + ":" + (hundredths / 100 % 60).ToString("00") + "." + (hundredths % 100).ToString("00");
    }
    void TogglePause()
    {
        if (selectingLevel || bike == null || bike.IsCrashed() || bike.IsFinished()) return;
        paused = !paused;
        Time.timeScale = paused ? 0 : 1;
        bike.touchGas = bike.touchBrake = bike.touchLeanBack = bike.touchLeanForward = false;
    }
    public void Restart()
    {
        if (selectingLevel) return;
        Time.timeScale = 1;
        paused = false;
        if (bike == null) return;
        attempts++;
        resultSaved = newBest = false;
        elapsed = 0;
        bike.ResetBike();
    }
    // Kept for existing scene/script references. Results are driven by bike state.
    public void ShowMessage(string message) { }
    public void ClearMessage() { }
    void OnDestroy() { Time.timeScale = 1; if (panel != null) Destroy(panel); }
}
