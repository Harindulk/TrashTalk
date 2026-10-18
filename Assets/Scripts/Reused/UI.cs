using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Menus + HUD. The canvas is built in the scene; this toggles screens, updates the HUD and handles buttons.
public class UI : MonoBehaviour
{
    public static UI I;

    [Serializable]
    public class LevelButton
    {
        public Button button;
        public Sprite openSprite;
        public Color openInk = Color.white;
        public Text label;
        public GameObject lockedTag;
        public Image[] stars;
    }

    public float hintTime = 5f;

    [Header("Screens")]
    public GameObject title;
    public GameObject levelSelect, hud, pause, win, lose;

    [Header("Title / levels")]
    public Text soundLabel;
    public LevelButton[] levelButtons;
    public Sprite lockedSprite;

    [Header("HUD")]
    public Text levelText;
    public Text trashText, hintText;
    public Image[] rocketIcons;
    public RectTransform fuelRoot, fuelFill;
    public Image fuelImg;

    [Header("Win / lose")]
    public RectTransform winPanel;
    public Text winWord;
    public Image[] winStars;
    public GameObject nextButton, beatText;
    public RectTransform loseTitle;
    public Text roastText;
    public string[] roasts;                // lose-screen heckles

    [Header("Overlay")]
    public Text banner;
    public Text coachText;   // Tutorial writes here

    static readonly Color StarOff = new Color(0, 0, 0, 0.3f);

    GameState shownState = (GameState)(-1);
    LevelRoot shownRoot;
    int shownTrash, shownRockets;
    float hintStart;
    Vector2 fuelBase, bannerBase;
    Coroutine bannerCo;

    void Awake()
    {
        I = this;
        fuelBase = fuelRoot.anchoredPosition;
        bannerBase = banner.rectTransform.anchoredPosition;
    }

    void Update()
    {
        var g = Game.I;
        if (g == null) return;
        if (g.State != shownState || g.Root != shownRoot) Show(g);
        if ((g.State == GameState.Playing || g.State == GameState.Paused) && g.Root != null) UpdateHud(g);
        else fuelRoot.localScale = Vector3.zero; // no fuel bar over the win/lose popups
    }

    void Show(Game g)
    {
        var s = g.State;
        bool inLevel = s != GameState.Title && s != GameState.LevelSelect;
        bool entering = inLevel && g.Root != null && (g.Root != shownRoot || !hud.activeSelf); // level scene loads a frame later
        shownState = s;
        shownRoot = g.Root;

        title.SetActive(s == GameState.Title);
        levelSelect.SetActive(s == GameState.LevelSelect);
        pause.SetActive(s == GameState.Paused);
        win.SetActive(s == GameState.LevelWon);
        lose.SetActive(s == GameState.LevelLost);
        if (entering) EnterLevel(g);
        hud.SetActive(inLevel);

        if (s == GameState.Title) soundLabel.text = SoundLabel();
        if (s == GameState.LevelSelect) RefreshLevels(g);
        if (s == GameState.Paused || s == GameState.LevelWon || s == GameState.LevelLost) Sfx.Play("whoosh", 0.6f);
        if (s == GameState.LevelWon) ShowWin(g);
        if (s == GameState.LevelLost)
        {
            roastText.text = "\"" + (roasts.Length > 0 ? roasts[UnityEngine.Random.Range(0, roasts.Length)] : "Ha! Missed!") + "\"";
            StartCoroutine(Shake(loseTitle, 0.5f, 14f, 0.25f));
        }
    }

    void EnterLevel(Game g)
    {
        hintStart = Time.unscaledTime;
        levelText.text = "LEVEL " + (g.LevelIndex + 1) + "  " + g.Root.levelName;
        hintText.text = g.Root.hint;
        hintText.gameObject.SetActive(false); // re-enable replays its pop-in
        hintText.gameObject.SetActive(true);
        for (int i = 0; i < rocketIcons.Length; i++) rocketIcons[i].gameObject.SetActive(i < g.Root.rocketsInScene.Count);
        shownTrash = shownRockets = -1;
        fuelRoot.localScale = Vector3.zero;
    }

    void RefreshLevels(Game g)
    {
        for (int i = 0; i < levelButtons.Length; i++)
        {
            var b = levelButtons[i];
            bool open = i < g.LevelCount && g.IsUnlocked(i);
            int stars = g.GetStars(i);
            b.button.interactable = open;
            ((Image)b.button.targetGraphic).sprite = open ? b.openSprite : lockedSprite;
            b.label.text = open ? (i + 1).ToString() : "?";
            b.label.color = open ? b.openInk : new Color(0.16f, 0.12f, 0.20f);
            b.lockedTag.SetActive(!open);
            for (int k = 0; k < b.stars.Length; k++)
            {
                b.stars[k].gameObject.SetActive(open);
                b.stars[k].color = k < stars ? Color.white : new Color(0, 0, 0, 0.35f);
            }
        }
    }

    void ShowWin(Game g)
    {
        int stars = Mathf.Clamp(g.LastStars, 0, 3);
        winWord.text = stars >= 3 ? "SPOTLESS!" : stars == 2 ? "CLEAN!" : "GOOD ENOUGH";
        foreach (var s in winStars) { s.color = StarOff; s.transform.localScale = Vector3.one; s.transform.localRotation = Quaternion.identity; }
        bool hasNext = g.LevelIndex + 1 < g.LevelCount;
        nextButton.SetActive(hasNext);
        beatText.SetActive(!hasNext);
        StartCoroutine(SlamStars(stars));
    }

    void UpdateHud(Game g)
    {
        int trash = g.TrashLeft;
        if (trash != shownTrash)
        {
            trashText.text = "x " + trash;
            if (shownTrash >= 0) StartCoroutine(Punch(trashText.transform, 0.4f));
            shownTrash = trash;
        }

        if (g.RocketsLeft != shownRockets)
        {
            for (int i = 0; i < rocketIcons.Length; i++)
            {
                rocketIcons[i].color = i < g.RocketsLeft ? Color.white : StarOff;
                if (i == g.RocketsLeft && shownRockets >= 0) StartCoroutine(Punch(rocketIcons[i].transform, 0.6f));
            }
            shownRockets = g.RocketsLeft;
        }

        // fuel bar springs open while a rocket flies
        bool flying = g.ActiveRocket != null && !g.ActiveRocket.Dead;
        float fuel = Mathf.Clamp01(g.Fuel01);
        fuelRoot.localScale = Vector3.Lerp(fuelRoot.localScale, flying ? Vector3.one : Vector3.zero, 1 - Mathf.Exp(-14f * Time.unscaledDeltaTime));
        fuelFill.anchorMax = new Vector2(Mathf.Max(0.02f, fuel), 1);
        fuelImg.color = fuel > 0.5f ? Color.Lerp(new Color(1.00f, 0.71f, 0.18f), new Color(0.25f, 0.64f, 0.30f), (fuel - 0.5f) * 2f) : Color.Lerp(new Color(0.94f, 0.33f, 0.36f), new Color(1.00f, 0.71f, 0.18f), fuel * 2f);
        bool low = flying && fuel > 0f && fuel < 0.25f;
        fuelRoot.anchoredPosition = fuelBase + (low ? UnityEngine.Random.insideUnitCircle * 6f : Vector2.zero);

        var c = hintText.color;
        c.a = Mathf.Clamp01(1f - (Time.unscaledTime - hintStart - hintTime));
        hintText.color = c;
    }

    // ---------- buttons (persistent OnClick listeners wired in the scene) ----------

    public void Play()
    {
        var g = Game.I;
        for (int i = 0; i < g.LevelCount; i++)
            if (g.IsUnlocked(i) && g.GetStars(i) == 0) { g.StartLevel(i); return; }
        g.GoToLevelSelect();
    }

    public void OpenLevels() => Game.I.GoToLevelSelect();
    public void PlayLevel(int index) => Game.I.StartLevel(index);
    public void Next() => Game.I.NextLevel();
    public void Pause() => Game.I.TogglePause();
    public void Resume() => Game.I.TogglePause();
    public void Retry() => Restart();

    public void Restart()
    {
        Time.timeScale = 1f;
        Game.I.Restart();
    }

    public void Menu()
    {
        Time.timeScale = 1f;
        Game.I.GoToTitle();
    }

    public void ToggleSound()
    {
        Sfx.Muted = !Sfx.Muted;
        soundLabel.text = SoundLabel();
    }

    static string SoundLabel() => Sfx.Muted ? "SOUND: OFF" : "SOUND: ON";

    // ---------- banner ----------

    public void Banner(string text, Color? color = null)
    {
        if (bannerCo != null) StopCoroutine(bannerCo);
        banner.text = text;
        banner.color = color ?? new Color(1.00f, 0.71f, 0.18f);
        banner.gameObject.SetActive(true);
        bannerCo = StartCoroutine(BannerRoutine());
    }

    IEnumerator BannerRoutine()
    {
        var rt = banner.rectTransform;
        rt.anchoredPosition = bannerBase;
        for (float a = 0f; a < 1f;)
        {
            a += Time.unscaledDeltaTime / 0.3f;
            float k = EaseOutBack(Mathf.Clamp01(a));
            rt.localScale = Vector3.one * k;
            rt.localRotation = Quaternion.Euler(0, 0, (1f - k) * -15f);
            yield return null;
        }
        for (float hold = 0f; hold < 0.8f;)
        {
            hold += Time.unscaledDeltaTime;
            rt.localScale = Vector3.one * (1f + Mathf.Sin(hold * 12f) * 0.03f);
            yield return null;
        }
        Color col = banner.color;
        for (float b = 0f; b < 1f;)
        {
            b += Time.unscaledDeltaTime / 0.3f;
            float e = b * b;
            rt.anchoredPosition = bannerBase + new Vector2(0, 400f * e);
            rt.localScale = Vector3.one * (1f - 0.4f * e);
            col.a = 1f - e;
            banner.color = col;
            yield return null;
        }
        banner.gameObject.SetActive(false);
    }

    // ---------- animations (unscaled, so they run while paused) ----------

    IEnumerator Punch(Transform t, float amount)
    {
        for (float a = 0f; a < 1f;)
        {
            a += Time.unscaledDeltaTime / 0.25f;
            float s = Mathf.Sin(a * Mathf.PI) * amount * (1f - a);
            t.localScale = new Vector3(1f + s, 1f - s * 0.5f, 1f);
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    IEnumerator Shake(RectTransform t, float duration, float strength, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        Vector2 basePos = t.anchoredPosition;
        for (float a = 0f; a < duration;)
        {
            a += Time.unscaledDeltaTime;
            t.anchoredPosition = basePos + UnityEngine.Random.insideUnitCircle * strength * (1f - a / duration);
            yield return null;
        }
        t.anchoredPosition = basePos;
    }

    IEnumerator SlamStars(int count)
    {
        yield return new WaitForSecondsRealtime(0.5f);
        for (int i = 0; i < count; i++)
        {
            if (!win.activeInHierarchy) yield break; // left the win screen early
            var t = winStars[i].transform;
            winStars[i].color = Color.white;
            for (float a = 0f; a < 1f;)
            {
                a += Time.unscaledDeltaTime / 0.18f;
                float k = Mathf.Clamp01(a);
                t.localScale = Vector3.one * Mathf.Lerp(3f, 1f, k * k); // slam down from big
                t.localRotation = Quaternion.Euler(0, 0, (1f - k) * 90f);
                yield return null;
            }
            Sfx.Play("star", 1f, 1f + i * 0.15f);
            StartCoroutine(Shake(winPanel, 0.2f, 12f, 0f));
            StartCoroutine(Punch(t, 0.35f));
            yield return new WaitForSecondsRealtime(0.3f);
        }
    }

    public static float EaseOutBack(float x)
    {
        const float c1 = 2.2f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3) + c1 * Mathf.Pow(x - 1f, 2);
    }
}
