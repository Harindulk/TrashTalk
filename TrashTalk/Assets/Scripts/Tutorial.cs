using UnityEngine;
using UnityEngine.UI;

// Level 1 coach: a ghost hand shows the slingshot pull, then slow-mo + "TAP NOW!" when a drop would land in a can.
// Lives on the Level01 prefab root, so a restart or menu removes it. Text goes to the scene UI coach label.
public class Tutorial : MonoBehaviour
{
    public float slowMo = 0.3f;
    public float lineUp = 1.0f;   // how close the predicted landing must be to a mouth

    public SpriteRenderer hand;   // ghost "finger" that demos the pull-back

    Text label;
    bool slowed, scoredOnce;
    int lastScored;
    float t;

    void Start()
    {
        label = UI.I != null ? UI.I.coachText : null;
        lastScored = Game.I != null ? Game.I.Scored : 0;
    }

    void Update()
    {
        var g = Game.I;
        if (g == null || g.Root == null || g.State != GameState.Playing) { SetSlow(false); Show(""); hand.enabled = false; return; }
        t += Time.unscaledDeltaTime;

        if (g.Scored > lastScored && !scoredOnce) { scoredOnce = true; Show("NICE! Drop the rest!"); }
        lastScored = g.Scored;

        var la = g.Root.Launcher;
        var r = g.ActiveRocket;

        // Step 1: waiting on the pad.
        if (la.Ready && !la.Dragging)
        {
            SetSlow(false);
            Show(scoredOnce ? "" : "DRAG BACK from the rocket, then LET GO!");
            Vector2 pad = la.transform.position + Vector3.up * 1.2f;
            float k = Mathf.Repeat(t, 1.6f) / 1.2f;
            hand.enabled = k <= 1f;
            hand.transform.position = Vector2.Lerp(pad, pad + new Vector2(-2.2f, -1.4f), Mathf.SmoothStep(0, 1, k));
            return;
        }
        hand.enabled = false;
        if (la.Dragging) { Show("Aim with the dotted line... LET GO!"); return; }

        // Step 2: flying with trash. Slow down and prompt when a drop would land in a can.
        if (r != null && !r.Dead && r.Load.Count > 0)
        {
            bool lined = LinedUp(r);
            SetSlow(lined && !scoredOnce);
            if (lined) Show("TAP NOW!");
            else Show(scoredOnce ? "" : "Watch the white dots... TAP when they hit the can!");
            if (lined) label.transform.localScale = Vector3.one * (1f + 0.12f * Mathf.Sin(t * 20f));
            else label.transform.localScale = Vector3.one;
            return;
        }
        SetSlow(false);
        if (!scoredOnce) Show("");
    }

    bool LinedUp(Rocket r)
    {
        foreach (var c in TrashCan.All)
        {
            if (c == null || !c.Accepts(r.Load[r.Load.Count - 1].Type)) continue;
            Vector2 m = c.MouthPos;
            Vector2 p = r.DropPos, v = r.DropVelocity;
            for (int i = 0; i < 300; i++)
            {
                v += Physics2D.gravity * 0.01f; p += v * 0.01f;
                if (v.y < 0f && p.y <= m.y) break;
            }
            if (Mathf.Abs(p.x - m.x) < lineUp) return true;
        }
        return false;
    }

    void SetSlow(bool on)
    {
        if (on == slowed) return;
        slowed = on;
        Time.timeScale = on ? slowMo : 1f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale; // keep physics smooth in slow-mo
    }

    void Show(string s) { if (label != null && label.text != s) label.text = s; }

    void OnDestroy()
    {
        if (slowed) { Time.timeScale = 1f; Time.fixedDeltaTime = 0.02f; }
        Show("");
        if (label != null) label.transform.localScale = Vector3.one;
    }
}
