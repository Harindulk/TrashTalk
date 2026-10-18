using System.Collections.Generic;
using UnityEngine;

// Root of a level scene: level data plus the pre-placed rockets and trash.
public class LevelRoot : MonoBehaviour
{
    public string levelName, hint;
    public float width = 30f;   // level spans x from -8 to width
    public Launcher Launcher;

    public List<TrashCan> Cans = new List<TrashCan>();
    public List<Rocket> rocketsInScene = new List<Rocket>(); // launch order; the line of them is the rockets-left display
    public List<Trash> trashInScene = new List<Trash>();     // queue order; the pile is the trash queue display

    public Rect Bounds => new Rect(-8, -2, width + 8, 30);
    void Start() { if (Game.I != null) Game.I.Begin(this); } // Start: every rocket/trash Awake has run

    public Vector2 Home => (Vector2)Launcher.transform.position + new Vector2(6f, 4f);

}
