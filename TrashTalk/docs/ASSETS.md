# Trash Talk: optional asset swaps

Everything already works with procedural art (`Core/Art.cs`) and synthesized audio (`Feel/Sfx.cs`). To replace one, drop a file at:

- Sprite: `Assets/Resources/Art/<name>.png`
- Sound: `Assets/Resources/Audio/<name>.wav` or `.ogg` (one file per name, never both)

The name must match exactly (lowercase, no extension in code). Missing files fall back to the placeholder, so you can swap one at a time. `Assets/Resources/` doesn't exist yet; create it.

**Style rule:** the placeholders are flat cartoon with thick dark outlines. A half-swapped mix of styles looks worse than all placeholders, so swap a whole group (for example all 6 trash pieces) or nothing.

---

## Top 8 swaps (do these first)

| # | Swap | Why it scores |
|---|---|---|
| 1 | `music` | A real looping track is the biggest single jump in audio score. The synth loop sounds like a placeholder. |
| 2 | `thrust` + `launch` + `crash` | The rocket is the special object. Meaty launch, rumble and boom sell it. |
| 3 | `clunk` + `rim` + `miss` + `spit` | Score / near-miss / litter / reject feedback, heard every few seconds. |
| 4 | `win` + `lose` + `star` | End-of-level jingles; judges always hear these. |
| 5 | `rocket` | On screen the whole time, also used as UI icon and title art. |
| 6 | `trash_*` (all 6) | The "trash" theme lives here; hand-drawn items read better than code shapes. |
| 7 | `can_body` + `can_lid` | The characters. Tricky constraints (see below); only swap if the art follows them. |
| 8 | `ground` + `wall` | Big screen area; a textured tile makes levels feel less flat. |

The cans' gibberish speech voice (vowel blips) is synthesized and is **not** overridable by file.

---

## Import settings (all sprites)

Select the PNG in Unity, Inspector:

- Texture Type: **Sprite (2D and UI)**, Sprite Mode: **Single**
- Pixels Per Unit: anything (code rescales every sprite to a fixed world size)
- Pivot: **Center** unless the table says otherwise. For custom pivots: open **Sprite Editor**, set Pivot to Custom (or pick the preset), Apply.
- Filter Mode: Bilinear, Compression: Normal, Max Size: 512 is plenty (1024 for `ground`)
- Wrap Mode: Clamp
- Transparent background, trim empty margins: code stretches the image's bounds to the target box, so padding shrinks the visible art.

**Aspect ratio matters.** `Art.Make` stretches the sprite to an exact width x height. Draw at the aspect given below or it will squash.

**Tinted sprites** are multiplied by a color in code. Draw them **white / light grey** with dark outlines; full-color art turns muddy when tinted.

## Import settings (all audio)

- Format: `.ogg` (smaller WebGL build) or `.wav`. 44.1 kHz, mono is fine for SFX.
- Trim leading silence (the game plays them on the exact frame of the event). Normalize to about -3 dB peak.
- Load Type: Decompress On Load for short SFX; **Compressed In Memory** for `music` and `thrust`.
- `thrust` and `music` are looped by code: they must loop seamlessly (no click at the seam).
- Code already randomizes pitch +-6% and repitches some (stars, combo, drop), so plain single hits are ideal.

---

## Sprites (names verified in Art.cs `Build()` and every `Art.Make/Get` call)

Size column = world units width x height (the aspect to draw at).

### Gameplay

| File | Depicts | Size / aspect | Pivot | Color |
|---|---|---|---|---|
| `rocket.png` | Junky cartoon rocket: body, fins, round window, nozzle. **Nose points UP.** | 0.9 x 1.8 (1:2) | Center | Full color (tint is white). Also used for UI rocket icons and title. |
| `flame.png` | Exhaust flame, teardrop, **tip points DOWN**, wide end at top touching the nozzle | 0.55 x 0.85 | Center | Full color (yellow/orange/white core) |
| `pad.png` | Launch pad slab with hazard stripes | 2.4 x 0.5 (~5:1) | Center | Full color |
| `trash_bottle.png` | Plastic bottle, upright | 0.35 x 0.8 | Center | Full color. Also the UI trash-counter icon. |
| `trash_soda.png` | Soda can, upright (dented is nice) | 0.4 x 0.55 | Center | Full color |
| `trash_paper.png` | Crumpled paper ball | 0.55 x 0.55 | Center | Full color |
| `trash_banana.png` | Banana peel, lying sideways | 0.75 x 0.45 | Center | Full color |
| `trash_apple.png` | Apple core | 0.5 x 0.5 | Center | Full color |
| `trash_fish.png` | Fish skeleton, sideways | 0.8 x 0.4 (2:1) | Center | Full color |
| `can_body.png` | Trash can body, ribbed. **No face, no lid, blank front**: code draws eyes, brows and mouth on top (mouth is a dark oval at the top edge). | 1.3 x 1.6 (Dumpster stretches to 2.2x wider, Boss 3.5x) | **Bottom-center** (0.5, 0) | **White / light grey** (tinted per can type: blue, green, purple, yellow...) |
| `can_lid.png` | Flat lid seen from the side, small handle on top | ~1.43 x 0.22 (about 6.5:1, very thin) | **Left-middle** (0, 0.5) so it hinges open like a mouth | **White / light grey** (tinted) |
| `eye.png` | Eye white with dark outline, round | square | Center | White |
| `pupil.png` | Black disc | square | Center | Black (tint is white) |
| `brow.png` | Eyebrow bar, rounded ends (code rotates it for moods) | 5:1 | Center | Black (tint is white) |
| `bubble.png` | Speech bubble, **tail at bottom-left** | stretched to fit text (roughly 2:1 to 4:1) | Center | **White** (tinted per can) |
| `fan.png` | Fan blades (3-4), seen from front; code spins it | square | Center | White / light grey (tinted grey) |
| `wall.png` | One brick/plank tile, tiles in ~1x1 cells | 1:1 | Center | **Light grey** (tinted brick color) |
| `ground.png` | Grass strip on top, dirt below. **Grass must touch the top edge** (top edge = ground surface). Must tile left-to-right seamlessly. | 3.02 x 2 (3:2) | Center | Full color |

### Backdrop

| File | Depicts | Size / aspect | Pivot | Color |
|---|---|---|---|---|
| `house.png` | Simple house/building silhouette. **No windows**: code adds two lit windows. Stretched to random sizes. | varies | Center | **White / light grey** (tinted + hazed) |
| `cloud.png` | Puffy cloud | 2:1 | Center | White (tinted white with alpha) |
| `sun.png` | Cartoon sun (a smiley face fits the game) | square | Center | Full color |

### Effects and UI

| File | Depicts | Size / aspect | Pivot | Color |
|---|---|---|---|---|
| `star.png` | 5-point star, chunky outline (level-select and win screen) | square | Center | Full color yellow (empty star = tinted black 30%, so silhouette must read) |
| `smoke.png` | Soft round puff, blurry edges. Also used as the rocket smoke-trail texture. | square | Center | **White** (tinted grey/cream) |
| `spark.png` | Small 4-point twinkle | square | Center | White |
| `confetti.png` | Small rectangle | 2:1 | Center | White (tinted per can) |

**Leave alone:** `white`, `circle`, `ring`, `rounded`. These are generic primitives used for UI panels, buttons, fuel bar, mouths, bushes and particles. (`rounded` is a 9-slice: if you ever replace it, set a Border in the Sprite Editor.)

---

## Sounds (names verified in Sfx.cs `Names[]` and every `Sfx.Play` call)

| File | When it plays | What to look for |
|---|---|---|
| `music` | Looping background track | Upbeat, goofy, cartoon. 60-120 s seamless loop. Mixed quiet (code plays at 0.35). |
| `thrust` | Loop while rocket burns fuel (fades in/out) | 1-3 s seamless rocket/engine rumble |
| `launch` | Rocket released from slingshot | Whoosh + ignition burst |
| `crash` | Rocket hits something and explodes | Cartoon explosion, short |
| `drop` | Each trash piece released | Short falling whistle or "plip" (code pitches it up as the load empties) |
| `clunk` | Trash lands in a can (score) | Satisfying metal-can thunk |
| `rim` | Trash bounces off a can's rim | Metallic "clang" |
| `spit` | Can rejects the wrong trash | Cartoon spit / "ptooey" |
| `miss` | Trash hits the ground (litter) | "Wah-wah" or deflating squeak |
| `bonk` | Trash/rocket bumps a wall; button hover; boss hit | Light cartoon bonk |
| `lid` | A can's lid flaps open | Plastic clack |
| `laugh` | Cans laugh at a miss / your loss | Short cartoon "ha-ha-ha" (non-verbal blips is fine) |
| `combo` | 2+ scores in a row | Rising sparkle (code pitches up per combo) |
| `pop` | Rocket loaded on pad; rocket sputters out of fuel | Bubble pop |
| `boing` | Aim cancelled | Spring boing |
| `whoosh` | UI screen transitions; rocket leaves the level | Soft swish |
| `click` | Button press; slingshot pull ticks | Short UI click |
| `star` | Each star revealed on win screen | Bright ding (code pitches up per star) |
| `win` | Level cleared | 1-3 s happy jingle |
| `lose` | Out of rockets | Sad trombone / descending jingle |

---

## Where to get them (CC0 first)

All Kenney assets are CC0 (no credit required, credit appreciated). Browse at https://kenney.nl/assets and check each pack's license line before using.

**Audio**
- Kenney **Impact Sounds**: https://kenney.nl/assets/impact-sounds → `clunk`, `rim`, `bonk`, `crash` (layer a heavy impact)
- Kenney **Interface Sounds**: https://kenney.nl/assets/interface-sounds → `click`, `pop`, `whoosh`, `lid`
- Kenney **Digital Audio**: https://kenney.nl/assets/digital-audio → `star`, `combo`, `drop`, `boing`
- Kenney **Sci-Fi Sounds**: https://kenney.nl/assets/sci-fi-sounds → `thrust` (engine loops), `launch`, `crash`
- Kenney **Music Jingles**: https://kenney.nl/assets/music-jingles → `win`, `lose`
- freesound.org with the **Creative Commons 0** license filter: https://freesound.org/search/?f=license:%22Creative+Commons+0%22 . Good searches: "sad trombone", "cartoon spit", "rocket loop", "cartoon laugh", "wah wah", "trash can lid".
- OpenGameArt CC0 sound effects: https://opengameart.org/art-search-advanced?field_art_type_tid[]=13&field_art_licenses_tid[]=4

**Music**
- CC-BY 4.0 (needs a credit line): Kevin MacLeod, incompetech.com. Fits the tone: "Fluffing a Duck", "Monkeys Spinning Monkeys", "Sneaky Snitch". Credit format: `"<Title>" Kevin MacLeod (incompetech.com), Licensed under Creative Commons: By Attribution 4.0`
- CC0: OpenGameArt CC0 music: https://opengameart.org/art-search-advanced?field_art_type_tid[]=12&field_art_licenses_tid[]=4 (search "happy", "funny", "loop")

**Sprites**
- Kenney **Space Shooter Redux**: https://kenney.nl/assets/space-shooter-redux → flames, stars, effects (ships point UP already, but they are jets, not a junk rocket)
- Kenney **Particle Pack**: https://kenney.nl/assets/particle-pack and **Smoke Particles**: https://kenney.nl/assets/smoke-particles → `smoke`, `spark`, `flame` (pick white ones for `smoke`/`spark`)
- Kenney **Background Elements Redux**: https://kenney.nl/assets/background-elements-redux → `cloud`, `sun`, `house`
- Kenney **Physics Assets** / **Abstract Platformer**: https://kenney.nl/assets → `wall`, `ground` tiles
- Kenney **Toon Characters**: https://kenney.nl/assets/toon-characters-1 → reference for face style only (the cans' faces are code-drawn)
- OpenGameArt CC0 2D art: https://opengameart.org/art-search-advanced?field_art_type_tid[]=9&field_art_licenses_tid[]=4 (search "trash", "garbage", "rocket", "banana peel")

Record every file and its license in the Credits section of `docs/ITCH_PAGE.md` as you add it.
