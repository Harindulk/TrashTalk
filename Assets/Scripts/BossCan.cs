using UnityEngine;

// Boss-only behaviour on Can_Boss next to its TrashCan: hp pips, lid rhythm, intro, taunts, taking hits.
[RequireComponent(typeof(TrashCan))]
public class BossCan : MonoBehaviour
{
    public SpriteRenderer[] pips;       // one per hit
    public Color pipHitColor = new Color(0.2f, 0.2f, 0.2f, 0.6f);
    public float lidRhythm = 1.8f;

    public int Hp { get; private set; }
    public bool RhythmOpen => Mathf.Repeat(Time.time, lidRhythm * 2f) < lidRhythm * 1.4f; // open 70% of the cycle

    TrashCan can;
    float tauntT = 5f;

    void Awake() { can = GetComponent<TrashCan>(); Hp = pips.Length; }

    void Update()
    {
        if ((tauntT -= Time.deltaTime) > 0f) return;
        tauntT = Random.Range(5f, 8f);
        can.Chat("near");
    }

    public void Intro()
    {
        can.Say(can.Line("bossIntro"));
        can.anim.SetTrigger("Angry");
        if (Juice.I != null) Juice.Shake(0.4f, 0.6f);
        TrashCan.Snd("laugh", 1f, 0.6f);
    }

    public void Hit()
    {
        Hp = Mathf.Max(0, Hp - 1);
        if (Hp < pips.Length) pips[Hp].color = pipHitColor;
        can.anim.SetTrigger("Angry");
        TrashCan.Snd("bonk", 1f, 0.7f);
        if (Juice.I != null)
        {
            Juice.Shake(0.5f, 0.35f);
            Juice.HitStop(0.06f);
            Juice.Burst("debris", can.MouthPos, can.tint, 10);
        }
        can.Say(can.Line(Hp == 0 ? "win" : "hit"));
    }
}
