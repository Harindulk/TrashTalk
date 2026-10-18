using System.Collections.Generic;
using UnityEngine;

// Shared data types every script uses. Keep this file small and boring.

public enum TrashType { Bottle, Soda, Paper, Banana, Apple, Fish }
public enum TrashCategory { Recycle, Food }
public enum CanKind { Normal, Lazy, Snobby, Compost, Dumpster, Nervous, Boss }
public enum GameState { Title, LevelSelect, Playing, LevelWon, LevelLost, Paused }

public static class TrashInfo
{
    public static TrashCategory Category(TrashType t) =>
        t == TrashType.Bottle || t == TrashType.Soda || t == TrashType.Paper ? TrashCategory.Recycle : TrashCategory.Food;

    public static string SpriteName(TrashType t) => "trash_" + t.ToString().ToLower();

    // World size of each piece (width, height).
    public static Vector2 Size(TrashType t)
    {
        switch (t)
        {
            // Matches the aspect of the Trash Pack sprites in Art/Sprites.
            case TrashType.Bottle: return new Vector2(0.55f, 0.7f);
            case TrashType.Soda: return new Vector2(0.5f, 0.66f);
            case TrashType.Paper: return new Vector2(0.55f, 0.65f);
            case TrashType.Banana: return new Vector2(0.62f, 0.68f);
            case TrashType.Apple: return new Vector2(0.55f, 0.58f);
            default: return new Vector2(0.72f, 0.65f); // Fish
        }
    }
}

// Sorting orders so everything layers the same way.
public static class Order
{
    public const int Sky = -100, Far = -80, Near = -60, Ground = 0, Walls = 5, Cans = 10, Trash = 20, Rocket = 30, Fx = 40, Speech = 50;
}
