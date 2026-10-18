# Trash Talk: code contract

Unity 6000.6.0f1, Universal 2D (URP), new Input System only, WebGL target. Jam: Mini Jame Gam #60, theme **Trash**, special object **Rocket**.

## The game in one paragraph
A junk rocket on a launch pad carries a stack of trash. You **pull back and release** (slingshot drag) to launch it. In flight it **burns fuel automatically** and its nose **turns toward the mouse** (or A/D keys). **Click / Space drops the next piece of trash**: the piece keeps the rocket's momentum and arcs down. Get it into a **trash can**. The cans are characters with faces whose lids are mouths, and they **talk trash** in speech bubbles. When fuel runs out the rocket falls; if it hits anything it crashes, explodes and sprays its leftover trash. Trash that touches the ground is **litter**: "MISS", and it goes back into the level's queue. Each level gives 3 rockets. Win = every piece scored. Lose = rockets gone and trash left. Stars: 3 if won with the 1st rocket, 2 with the 2nd, 1 otherwise.

## Hard rules for all code
- **Simple code.** Plain MonoBehaviours, public fields with good defaults, coroutines or Update lerps for animation. No DOTween, no async/await, no LINQ in Update, no ScriptableObjects, no interfaces/abstract hierarchies, no namespaces, no #if except where noted.
- **Everything is created from code at runtime.** No prefabs, no scene edits, no inspector wiring. A GameObject is built with `new GameObject(...)` + `AddComponent`. Sprites come from `Art`, sounds from `Sfx`.
- **No custom layers or tags** (project settings are off-limits). Identify things by component: `GetComponent<Trash>()`, `GetComponentInParent<TrashCan>()`, `GetComponent<Ground>()`.
- Input only through `Inp` (Assets/Scripts/Shared/Inp.cs). Uses the new Input System; never `UnityEngine.Input`.
- Text: legacy `Font` via `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")`. World text = `TextMesh` (set `GetComponent<MeshRenderer>().sortingOrder`). UI text = `UnityEngine.UI.Text`. No TextMeshPro.
- Physics 2D only. Units: world units. Ground top is **y = 0**. Gravity is set by Bootstrap to (0, -12).
- Sorting orders come from `Order` in Types.cs.
- Only touch your own files. Shared files `Assets/Scripts/Shared/Types.cs` and `Inp.cs` are read-only: if you truly need something there, put a small helper in your own file instead.
- Do **not** run Unity, `unity` CLI commands, or builds. The lead compiles and integrates.
- Every public API listed below must exist with exactly that signature (you may add more).
- Comments: one short line for the why when it's not obvious. No essays.

## Files and owners

| Area | Files (Assets/Scripts/...) | Owner |
|---|---|---|
| Shared (done) | Shared/Types.cs, Shared/Inp.cs | lead |
| Core | Core/Bootstrap.cs, Core/Game.cs, Core/Art.cs | agent **core** |
| Feel | Feel/Sfx.cs, Feel/Juice.cs, Feel/CameraRig.cs | agent **feel** |
| Rocket | Gameplay/Launcher.cs, Gameplay/Rocket.cs, Gameplay/Trash.cs | agent **rocket** |
| Cans | Gameplay/TrashCan.cs, Gameplay/Speech.cs, Gameplay/Lines.cs | agent **cans** |
| Levels | Level/LevelDefs.cs, Level/LevelBuilder.cs, Level/LevelRoot.cs, Level/Ground.cs, Level/Fan.cs, Level/Mover.cs, Level/Backdrop.cs | agent **levels** |
| UI | UI/UI.cs, UI/UIButtonJuice.cs | agent **ui** |

## Public APIs (the contract)

### Art (static) — core
```csharp
public static class Art {
  public static Sprite Get(string name);  // Resources/Art/<name> if present, else a cached procedural placeholder
  public static SpriteRenderer Make(string sprite, Transform parent, Vector2 localPos, Vector2 worldSize, Color tint, int order);
      // child GameObject named after the sprite; scale set so the sprite's bounds == worldSize (works for any PPU)
  public static Font Font { get; }        // LegacyRuntime.ttf
  public static readonly Color Sky, SkyLow, GroundCol, Dirt, Recycle, Compost, General, Dumpster, Accent, Danger, Outline, Cream;
}
```
Placeholder sprite names (all must exist; pivot center unless noted; look cartoony: flat colors, dark outline, simple highlight). **Generate them white/light-gray where they'll be tinted**, full color where they won't:
`white` (4x4 white square), `circle` (soft-edged white disc), `ring`, `rounded` (white rounded rectangle, good for 9-slice-ish panels/bubbles), `rocket` (full color: red/white body, fins, round window; **nose points UP**), `flame` (yellow/orange teardrop, tip down), `trash_bottle`, `trash_soda`, `trash_paper` (crumpled ball), `trash_banana`, `trash_apple` (core), `trash_fish` (fishbone) — full color; `can_body` (white/light, ribbed, tinted per can; **pivot bottom-center**), `can_lid` (white/light, tinted; pivot at its **left edge middle** so it hinges), `eye` (white with outline), `pupil` (black disc), `brow` (black bar), `ground` (grass top + dirt, tileable horizontally), `wall` (brick/plank, light, tinted), `house` (light silhouette, tinted), `cloud`, `sun`, `fan` (blades, white), `star` (yellow), `pad` (launch pad, hazard stripes), `confetti` (small white rect), `smoke` (soft white puff), `spark` (small white star/diamond), `bubble` (white speech bubble with a tail at bottom-left).

### Bootstrap — core
`[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot()`: sets `Physics2D.gravity = (0,-12)`, `Application.targetFrameRate = 60`, finds `Camera.main` (create one if missing: orthographic, size 7), adds `CameraRig` to it, ensures a `Global Light 2D` exists is NOT needed (scene has one, leave it). Creates one persistent GameObject "Game" with `Game`, `Sfx`, `Juice`, `UI` components (in that order). Then `Game.I.GoToTitle()`.

### Game (singleton MonoBehaviour) — core
```csharp
public class Game : MonoBehaviour {
  public static Game I;
  public GameState State;
  public int LevelIndex;                   // 0-based
  public int LevelCount;                   // LevelDefs.All.Count
  public LevelDef Def;                     // current level
  public LevelRoot Root;                   // built level
  public List<TrashType> Queue;            // trash waiting to be loaded
  public int RocketsLeft, RocketsUsed, Scored, Missed, Combo;
  public Rocket ActiveRocket;              // null when none
  public int ActiveTrash;                  // loose trash pieces still flying
  public int TrashLeft => Queue.Count + ActiveTrash + (ActiveRocket ? ActiveRocket.Load.Count : 0);
  public float Fuel01 => ActiveRocket ? ActiveRocket.Fuel01 : 1f;
  public int LastStars;                    // stars from the last win

  public void GoToTitle(); public void GoToLevelSelect();
  public void StartLevel(int index); public void Restart(); public void NextLevel();
  public void TogglePause();
  public int GetStars(int level);          // PlayerPrefs "stars_<i>", 0..3
  public bool IsUnlocked(int level);       // level 0 always; others if previous has >=1 star
  public List<TrashType> TakeLoad(int max);   // removes up to max from Queue

  // called by gameplay
  public void OnRocketLaunched(Rocket r);  // RocketsLeft--, RocketsUsed++, Combo=0, CameraRig follow r
  public void OnRocketFinished(Rocket r);  // ActiveRocket=null; leftover trash is spawned as loose Trash by the rocket itself before this
  public void OnTrashSpawned(Trash t);     // ActiveTrash++
  public void OnTrashScored(Trash t, TrashCan can);  // ActiveTrash--, Scored++, Combo++; Combo>=2 → UI.I.Banner("COMBO x"+Combo); juice
  public void OnTrashLittered(Trash t);    // ActiveTrash--, Missed++, Queue.Add(t.Type); TrashCan.BroadcastMiss(t.transform.position)
}
```
Flow: `StartLevel` destroys old `Root`, `Root = LevelBuilder.Build(Def)`, fills `Queue`, RocketsLeft = Def.rockets, `CameraRig.I.SetLevel(Def.Bounds, Root.Home)`, `CameraRig.I.Intro()`, `Root.Launcher.Reload()`, `UI.I.Banner(Def.name)`, State=Playing, `Sfx.Music(true)`. In Update while Playing: once there's no ActiveRocket and ActiveTrash==0 and the launcher is empty (`!Root.Launcher.HasRocket`): wait 0.6 s, then if `TrashLeft==0` → Win; else if `RocketsLeft==0` → Lose; else `Root.Launcher.Reload()` and `CameraRig.I.FollowNone()`. Win: stars = RocketsUsed<=1?3: RocketsUsed==2?2:1, save if better, `TrashCan.BroadcastWin()`, Sfx "win", State=LevelWon. Lose: `TrashCan.BroadcastLose()`, Sfx "lose", State=LevelLost. `Inp.Restart` → Restart (while Playing/Won/Lost). `Inp.Pause` → TogglePause (Time.timeScale 0/1). Game never touches UI widgets directly except `UI.I.Banner`.

### Sfx (singleton) — feel
```csharp
public class Sfx : MonoBehaviour {
  public static Sfx I;
  public static void Play(string name, float volume = 1f, float pitch = 1f);   // small random pitch jitter added inside
  public static void Thrust(bool on);           // looping engine sound
  public static void Talk(float pitch, int syllables);  // gibberish voice blips for talking cans
  public static void Music(bool on);
  public static bool Muted { get; set; }        // persisted in PlayerPrefs "muted"
}
```
Clips: `Resources/Audio/<name>` if present, else **synthesized at startup** with `AudioClip.Create` (make them genuinely pleasant/funny: pitch sweeps, noise bursts, envelopes). Names: `launch, thrust (loop), drop, clunk, rim, spit, bonk, crash, miss, click, win, lose, star, boing, whoosh, lid, pop, combo, laugh` and `music` (a simple cheerful looping chiptune, ~8–16 s, generated from a note pattern). Use a small pool of AudioSources.

### Juice (static API + host MonoBehaviour) — feel
```csharp
public class Juice : MonoBehaviour {
  public static Juice I;
  public static void Shake(float strength, float duration);       // forwards to CameraRig.I
  public static void HitStop(float seconds);                       // brief Time.timeScale freeze, unscaled restore; never stacks badly; ignored while paused
  public static void Burst(string kind, Vector2 pos, Color color, int count = 12); // kinds: confetti, smoke, dust, sparkle, debris, fire
  public static void Popup(string text, Vector2 pos, Color color, float size = 1f); // world text that pops, rises, fades
  public static void Punch(Transform t, float amount = 0.25f, float duration = 0.25f); // squash/stretch scale punch, returns to original scale
  public static ParticleSystem Trail(Transform parent, Vector2 localPos, Color color); // continuous smoke trail (world space sim)
}
```
Particles: ParticleSystem built in code, renderer material = a sprite-capable material. Use `new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"))` with the particle texture from `Art.Get(...).texture`, and fall back to `Shader.Find("Sprites/Default")` if null. Set renderer sortingOrder = Order.Fx.

### CameraRig (on Main Camera) — feel
```csharp
public class CameraRig : MonoBehaviour {
  public static CameraRig I;
  public void SetLevel(Rect bounds, Vector2 home);  // home = where the pad view centers
  public void Intro();            // starts zoomed out over the whole level, glides to home in ~1.8 s
  public void Follow(Transform t);// smooth follow with lookahead on velocity, zooms out a bit at speed
  public void FollowNone();       // glide back to home
  public void Shake(float strength, float duration);
  public void Punch(float zoom);  // quick zoom punch (+ = zoom in), springs back
}
```
Camera clamps inside bounds. Default ortho size 7, follow size up to 9. Also sets up URP post-processing in `Awake`: enable `UniversalAdditionalCameraData.renderPostProcessing`, add a global `Volume` with a runtime `VolumeProfile` containing `Bloom` (threshold ~0.9, intensity ~0.6) and `Vignette` (~0.25). Sky color comes from Backdrop, camera background = Art.Sky.

### Trash — rocket
```csharp
public class Trash : MonoBehaviour {
  public TrashType Type; public Rigidbody2D Body; public bool Done;
  public static Trash Spawn(TrashType type, Vector2 pos, Vector2 velocity, float spin = 0f); // builds sprite+collider+rb, calls Game.I.OnTrashSpawned
  public void Score(TrashCan can);        // Done=true; disable physics; tween into the can mouth & shrink; Game.I.OnTrashScored
  public void Reject(Vector2 spitVelocity); // spat back out (stays in play), Sfx "spit"
  public void Litter();                   // Done=true; Game.I.OnTrashLittered; grey tint, "MISS" popup, dust burst, fade+destroy after 1.2 s
}
```
Litter when: touches a `Ground`, falls below y = -3, leaves bounds, or rests (speed < 0.2 for 1.2 s) anywhere outside a can. Trash collides with walls/cans/other trash (bounces = funny). Ignore collisions with the rocket that dropped it. Uses a `BoxCollider2D`/`CircleCollider2D` sized from `TrashInfo.Size`, a light `PhysicsMaterial2D` bounce (0.3).

### Rocket — rocket
```csharp
public class Rocket : MonoBehaviour {
  public List<TrashType> Load;   // stack, last = top / next to drop
  public float Fuel01;           // 1 → 0
  public bool Launched, Dead;
  public Rigidbody2D Body;
  public static Rocket Create(Vector2 padPos, List<TrashType> load); // sits on the pad, trash stacked visibly on its back
  public void Launch(Vector2 velocity); // Game.I.OnRocketLaunched(this), Sfx launch, smoke burst, shake
}
```
Flight: gravityScale 1, auto-thrust along nose while fuel lasts (~3.2 s, thrust force ~ 22 * mass), nose turns toward `Inp.WorldPos` (or by `Inp.Steer` when keys pressed) at ~200°/s. **Mass = 1 + 0.35 × pieces**, so every drop makes it lighter and zippier (small upward hop + Juice.Punch). Flame sprite flickers + scales with thrust, smoke `Juice.Trail`, `Sfx.Thrust(true)` while burning. Out of fuel: flame off, "sputter" popup, nose follows velocity. `Inp.Action` → drop top piece: `Trash.Spawn(type, pos below rocket, rocket velocity + (0,-1.5))`, Sfx "drop". Crash: any collision with impact speed > 2.5 or touching `Ground`: explosion (fire + debris + smoke bursts, Shake 0.5, HitStop 0.08, Sfx crash), leftover Load spawned as Trash with random spray velocities, rocket destroyed, `Game.I.OnRocketFinished`. Leaving the level bounds (x > bounds.xMax+6, x < xMin-6, or y > 40): leftover pieces get Spawned at the rocket's last position with zero velocity so they fall (they'll litter or luck in), then finished. Empty rocket with no fuel just keeps flying until it crashes (it's funny).

### Launcher — rocket
```csharp
public class Launcher : MonoBehaviour {
  public static Launcher Create(Vector2 padPos, Transform parent); // pad sprite + idle rocket spot + visible queue pile next to pad
  public bool HasRocket;            // a rocket sits on the pad, not launched yet
  public void Reload();             // Rocket.Create(pad, Game.I.TakeLoad(5)); HasRocket = true; little drop-in animation, Sfx "pop"
}
```
Aiming: press anywhere (not over UI) while HasRocket and Game.I.State==Playing → drag; **launch direction = from pointer to pad** (pull back like a slingshot), power = clamp(drag length, 0, 3.5) mapped to speed 4..15; angle clamped to 10°–170°. While dragging: rocket tilts to aim, shakes a little at max power, dotted trajectory preview (12–16 fading dots from a short simulation of gravity + thrust along the aim). Release with tiny drag (<0.3) cancels. On release → `rocket.Launch(vel)`, HasRocket=false. The pile next to the pad shows `Game.I.Queue` as small trash sprites (refresh when Queue count changes).

### TrashCan — cans
```csharp
public class TrashCan : MonoBehaviour {
  public static readonly List<TrashCan> All;
  public CanKind Kind; public bool LidOpen;
  public static TrashCan Create(CanSpec spec, Transform parent);
  public bool Accepts(TrashType t);           // Snobby: Recycle only. Compost: Food only. others: all
  public static void BroadcastMiss(Vector2 pos);   // nearest can within 8 units mocks you
  public static void BroadcastWin();               // all cans cheer / grumble
  public static void BroadcastLose();              // all cans laugh
  public void Say(string text);                    // Speech bubble + Sfx.Talk with this can's voice pitch
}
```
Build: body sprite `can_body` tinted per kind (Normal=General grey, Lazy=muted purple, Snobby=Recycle blue, Compost=green, Dumpster=Dumpster dark green and 2.2× wide, Nervous=yellow, Boss=huge dark dumpster 3.5× with angry brows). Size: normal can 1.3 w × 1.6 h. Face on the body: 2 `eye` + `pupil` that track the nearest flying trash or the rocket; `brow`s for mood. `can_lid` on top hinged at the left: **opens wide (≈ -110°) when trash/rocket is near** (anticipation!), closes otherwise; flaps when talking. Colliders: solid `BoxCollider2D` body (trash bouncing off the rim is fine → Sfx "rim" + "Ooh, rim!"), plus a trigger mouth zone on top. Trash enters the mouth trigger moving down (vel.y < 0.5) and lid open → if `Accepts` → `trash.Score(this)`, chomp (lid slam + Punch + confetti in can colour + Sfx clunk + Popup "+1"), happy line; else `trash.Reject(up-and-back velocity)` + rude line. Closed lid = solid lid collider, `bonk`.
Behaviours: **Lazy** — lid shut on a 3 s nap / 2 s awake loop, "Zzz" popup, yawn line when waking. **Nervous** — slides away (up to 2.5 units, then returns) when the rocket comes within 5 units, shaking, "AAH!". **Dumpster** — accepts all, heckles every miss. **Boss** — needs `bossHp` pieces; moves back and forth (`Mover`), lid opens/closes on a 1.8 s rhythm, talks a lot, health shown as hearts/pips; each hit = big shake + angry line; it counts as scoring normally, so the level's trash count equals its HP. Each can kind has a voice pitch for `Sfx.Talk`.

### Speech — cans
```csharp
public static class Speech {
  public static void Say(Transform anchor, string text, Color bubble, float voicePitch, float duration = 2.2f);
}
```
World-space bubble (`bubble`/`rounded` sprite + `TextMesh`) above the anchor (+1.4 y), pops in with overshoot, typewriter text with `Sfx.Talk` blips, wobbles, then shrinks away. Max one bubble per anchor (replace). Text sorting = Order.Speech+1. Keep lines short so bubbles stay small; auto-wrap ~18 chars per line.

### Lines — cans
`public static class Lines { public static string Get(CanKind kind, string evt); }` events: `hello, near, score, reject, miss, rim, win, lose, nap, wake, scared, hit, bossIntro`. 4–8 punchy, kid-friendly but sassy lines per (kind, evt) with fallbacks to a shared pool. Snobby talks posh ("Ew. Compost, darling."), Compost is a chill hippie, Dumpster is a loud heckler, Nervous stutters, Lazy is sleepy, Boss is a dramatic villain ("I AM THE MEGA DUMPSTER!"). Random pick, avoid repeating the last line per kind.

### LevelRoot / LevelBuilder / LevelDefs — levels
```csharp
public class LevelRoot : MonoBehaviour { public Launcher Launcher; public Vector2 Home; public List<TrashCan> Cans; }
public static class LevelBuilder { public static LevelRoot Build(LevelDef def); } // ground, walls, cans, fans, backdrop, launcher; all under one root
public static class LevelDefs { public static List<LevelDef> All; }
public class Ground : MonoBehaviour {}   // marker on ground colliders
public class Fan : MonoBehaviour { public static Fan Create(FanSpec spec, Transform parent); } // pushes Rigidbody2D in a box (AddForce), spinning blades, wind streak particles, Sfx hum optional
public class Mover : MonoBehaviour { public Vector2 a, b; public float speed; } // ping-pong with smooth easing; uses kinematic Rigidbody2D.MovePosition if a body exists, else transform
public class Backdrop : MonoBehaviour { public static Backdrop Create(Rect bounds, Transform parent); } // sky gradient, sun, drifting clouds, two parallax rows of houses/fences, ground strip
```
Home = padPos + (6, 4). Ground: one long BoxCollider2D (top at y=0) + `Ground`, visual tiled `ground` sprites. Walls: solid boxes with `wall` sprite (bricks), not `Ground` (trash resting on a wall later litters by the rest rule). Platforms for rooftop cans are just walls.

10 levels, each teaching one thing, ramping gently (first one must be winnable in seconds):
1. **First Toss** — 1 Normal can at x≈10, 2 trash. Hint: "Pull back & release to launch. CLICK to drop trash!"
2. **Aim Higher** — can on a 3 m platform behind a low wall. Hint: "Move the mouse to steer the rocket."
3. **Picky Bin** — Snobby + Normal; bottles + banana. Hint: "Blue bins are snobs. Recycling only!"
4. **Sort It Out** — Snobby + Compost, mixed trash. Hint: "Food goes in the green one."
5. **Sleepy Sam** — Lazy cans. Hint: "Wait for it to wake up..."
6. **Windy Day** — fans blowing up/sideways near cans.
7. **Scaredy Can** — Nervous cans. Hint: "It's shy. Drop from far away!"
8. **Rooftops** — cans on rooftops at different heights + Dumpster.
9. **Rush Hour** — moving cans (moveRange) + fan + mix.
10. **MEGA DUMPSTER** — Boss (hp 6), 6 trash, 4 rockets.

### UI — ui
```csharp
public class UI : MonoBehaviour {
  public static UI I;
  public void Banner(string text, Color? color = null);  // big punchy centre text: scales in, holds 0.8 s, flies out
}
```
Builds one Screen Space Overlay Canvas (CanvasScaler 1920×1080, match 0.5) + an EventSystem with `InputSystemUIInputModule` if none exists. Polls `Game.I` every frame and shows the right panel for `Game.I.State`:
- **Title**: big wobbling "TRASH TALK" logo (letters bob), subtitle "a junk rocket game", buttons PLAY (→ first unlocked unbeaten level or level select), LEVELS, sound toggle. A couple of talking-can lines cycle under the logo. Credits line small at the bottom.
- **LevelSelect**: 10 big buttons (number + 3 stars), locked ones dimmed with a lock; Back.
- **HUD** (Playing): level name + number top-left, rocket icons left (RocketsLeft), trash left counter with icon, fuel bar that appears only while a rocket is flying (colour shifts to red when low, shakes), pause + restart buttons top-right, hint text at bottom that fades out after 5 s (re-show on restart).
- **LevelWon**: popup "SPOTLESS!" / "CLEAN!" / "GOOD ENOUGH" by stars; stars pop in one by one with Sfx "star"; Next / Retry / Menu.
- **LevelLost**: "TRASHED!" with a random can roast from `Lines.Get(CanKind.Dumpster, "lose")`; Retry / Menu.
- **Paused**: Resume / Restart / Menu.
Every button gets `UIButtonJuice` (hover scale 1.08, press squash, Sfx "click"). Panels scale in with overshoot (unscaled time, since pause sets timeScale 0). Bold, chunky, readable: big fonts, outlines (`Outline` component), the Art palette colors.
