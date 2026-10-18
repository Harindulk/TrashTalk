using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine;

// Game flow: title -> level select -> playing -> won/lost. Owns the trash queue and rocket count.
public class Game : MonoBehaviour
{
    public static Game I;

    [Header("Wiring")]
    public string[] levelScenes;        // Level01..Level10 scene names, in play order (all in Build Settings)
    public GameObject menuBackdrop;     // scene backdrop shown behind the menus

    [Header("Tuning")]
    public float settleDelay = 0.6f;   // pause after everything lands before deciding win/lose/reload

    [Header("Runtime")]
    public GameState State;
    public int LevelIndex;
    public LevelRoot Root;
    public List<Trash> Queue = new List<Trash>(); // waiting in the pile
    public int RocketsLeft, RocketsUsed, Scored, Missed, Combo;
    public Rocket ActiveRocket;
    public int ActiveTrash;
    public int LastStars;

    public int LevelCount => levelScenes.Length;
    Scene levelScene;

    // Counts the rocket sitting on the pad too, so the HUD doesn't dip when we load it.
    public int TrashLeft => Queue.Count + ActiveTrash + (ActiveRocket ? ActiveRocket.Load.Count : PadLoad);
    public float Fuel01 => ActiveRocket ? ActiveRocket.Fuel01 : 1f;

    int PadLoad => Root != null && Root.Launcher != null && Root.Launcher.HasRocket ? padCount : 0;

    readonly HashSet<Trash> live = new HashSet<Trash>();
    int padCount;
    float settleTimer;

    static readonly Rect MenuBounds = new Rect(-8, -2, 38, 30);

    void Awake()
    {
        I = this;
        Application.targetFrameRate = 60;
        Time.timeScale = 1f;
    }

    void Start() => GoToTitle();

    void Update()
    {
        if (Inp.Pause && (State == GameState.Playing || State == GameState.Paused)) TogglePause();
        if (Inp.Restart && (State == GameState.Playing || State == GameState.Paused || State == GameState.LevelWon || State == GameState.LevelLost))
        {
            Restart();
            return;
        }
        if (State == GameState.Playing) UpdatePlaying();
    }

    void UpdatePlaying()
    {
        if (Root == null || Root.Launcher == null) return;

        // Trash destroyed without reporting back must not block the level forever.
        live.RemoveWhere(t => t == null);
        ActiveTrash = live.Count;

        bool rocketDone = ActiveRocket == null || (Queue.Count == 0 && ActiveRocket.Load.Count == 0);
        bool settled = rocketDone && ActiveTrash == 0 && !Root.Launcher.HasRocket;
        if (!settled) { settleTimer = 0; return; }

        settleTimer += Time.deltaTime;
        if (settleTimer < settleDelay) return;
        settleTimer = 0;

        if (TrashLeft == 0) Win();
        else if (ActiveRocket != null) return; // still flying with trash somewhere
        else if (RocketsLeft <= 0) Lose();
        else
        {
            Root.Launcher.Reload();
            if (CameraRig.I != null) CameraRig.I.FollowNone();
        }
    }

    // ---------- Flow ----------

    public void GoToTitle()
    {
        ShowMenuScene();
        State = GameState.Title;
        Sfx.Music(true);
    }

    public void GoToLevelSelect()
    {
        ShowMenuScene();
        State = GameState.LevelSelect;
    }

    public void StartLevel(int index)
    {
        LevelIndex = Mathf.Clamp(index, 0, LevelCount - 1);

        ClearLevel();
        menuBackdrop.SetActive(false);
        State = GameState.Playing;
        SceneManager.LoadScene(levelScenes[LevelIndex], LoadSceneMode.Additive); // its LevelRoot calls Begin()
    }

    // Called by the level scene's LevelRoot when it wakes up.
    public void Begin(LevelRoot root)
    {
        Root = root;
        levelScene = root.gameObject.scene;
        Queue = new List<Trash>(Root.trashInScene);
        RocketsLeft = Root.rocketsInScene.Count;
        RocketsUsed = Scored = Missed = Combo = 0;
        settleTimer = 0;

        if (CameraRig.I != null)
        {
            CameraRig.I.SetLevel(Root.Bounds, Root.Home);
            CameraRig.I.Intro();
        }
        Root.Launcher.Reload();
        if (UI.I != null) UI.I.Banner(Root.levelName);
        State = GameState.Playing;
        Sfx.Music(true);
    }

    public void Restart() => StartLevel(LevelIndex);

    public void NextLevel()
    {
        if (LevelIndex + 1 < LevelCount) StartLevel(LevelIndex + 1);
        else GoToLevelSelect();
    }

    public void TogglePause()
    {
        if (State == GameState.Playing)
        {
            State = GameState.Paused;
            Time.timeScale = 0f;
            Sfx.Thrust(false);
        }
        else if (State == GameState.Paused)
        {
            State = GameState.Playing;
            Time.timeScale = 1f;
            if (ActiveRocket != null && ActiveRocket.Fuel01 > 0) Sfx.Thrust(true);
        }
    }

    public int GetStars(int level) => PlayerPrefs.GetInt("stars_" + level, 0);

    public bool IsUnlocked(int level) => level <= 0 || GetStars(level - 1) >= 1;

    public List<Trash> TakeLoad(int max)
    {
        int n = Mathf.Min(max, Queue.Count);
        var load = Queue.GetRange(0, n);
        Queue.RemoveRange(0, n);
        load.Reverse(); // last = top = dropped first, so pieces drop in the level's order
        padCount = n;
        return load;
    }

    // ---------- Gameplay callbacks ----------

    public void OnRocketLaunched(Rocket r)
    {
        ActiveRocket = r;
        RocketsLeft--;
        RocketsUsed++;
        Combo = 0;
        if (CameraRig.I != null) CameraRig.I.Follow(r.Body);
    }

    public void OnRocketFinished(Rocket r)
    {
        if (ActiveRocket == r || ActiveRocket == null) ActiveRocket = null;
        Sfx.Thrust(false);
    }

    public void OnTrashFlying(Trash t)
    {
        if (t != null) live.Add(t);
        ActiveTrash = live.Count;
    }

    public void OnTrashScored(Trash t, TrashCan can)
    {
        if (!live.Remove(t)) return; // already counted, or left over from an old level
        ActiveTrash = live.Count;
        Scored++;
        Combo++;

        if (Combo >= 2)
        {
            if (UI.I != null) UI.I.Banner("COMBO x" + Combo, new Color(1.00f, 0.71f, 0.18f));
            Sfx.Play("combo", 1f, 1f + 0.12f * (Combo - 2));
            Juice.Shake(0.15f + 0.05f * Combo, 0.2f);
        }

        // Last piece in: a beat of freeze + big confetti so the win lands.
        if (TrashLeft == 0)
        {
            Juice.HitStop(0.12f);
            Juice.Shake(0.4f, 0.35f);
            Vector2 pos = can != null ? (Vector2)can.transform.position + Vector2.up * 2f : (Vector2)t.transform.position;
            Juice.Burst("confetti", pos, new Color(1.00f, 0.71f, 0.18f), 40);
            if (CameraRig.I != null) CameraRig.I.Punch(0.6f);
        }
    }

    public void OnTrashLittered(Trash t)
    {
        if (!live.Remove(t)) return;
        ActiveTrash = live.Count;
        Missed++;
        Combo = 0;
        Queue.Add(t); // it walks back to the pile after its miss animation
        TrashCan.BroadcastMiss(t.transform.position);
    }

    // ---------- End of level ----------

    void Win()
    {
        LastStars = RocketsUsed <= 1 ? 3 : RocketsUsed == 2 ? 2 : 1;
        if (LastStars > GetStars(LevelIndex))
        {
            PlayerPrefs.SetInt("stars_" + LevelIndex, LastStars);
            PlayerPrefs.Save();
        }
        TrashCan.BroadcastWin();
        Sfx.Play("win");
        State = GameState.LevelWon;
    }

    void Lose()
    {
        TrashCan.BroadcastLose();
        Sfx.Play("lose");
        Juice.Shake(0.3f, 0.3f);
        State = GameState.LevelLost;
    }

    // ---------- Helpers ----------

    // Everything lives in the level scene, so unloading it cleans the level.
    void ClearLevel()
    {
        Time.timeScale = 1f;
        Sfx.Thrust(false);
        if (levelScene.IsValid() && levelScene.isLoaded) SceneManager.UnloadSceneAsync(levelScene);
        Root = null;

        live.Clear();
        ActiveRocket = null;
        ActiveTrash = 0;
        padCount = 0;
        Queue.Clear();
    }

    void ShowMenuScene()
    {
        ClearLevel();
        menuBackdrop.SetActive(true);
        if (CameraRig.I != null)
        {
            CameraRig.I.SetLevel(MenuBounds, new Vector2(8, 6));
            CameraRig.I.FollowNone();
        }
    }
}
