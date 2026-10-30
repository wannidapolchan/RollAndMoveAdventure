using System.Collections.Generic;
using UnityEngine;

/// <summary>The kinds of tiles on the board.</summary>
public enum TileType
{
    Normal,
    Start,
    Finish,
    Ladder,
    Snake,
    Coin,
    LifeEvent
}

/// <summary>
/// The rules of the board: how many tiles, where they are, and what each one does.
/// This class has no visuals. The ladders, snakes, coin tiles and event tiles
/// come from GameSettings, so they can be changed in the Inspector.
///
/// Tile numbers in code start at 0 (tile 0 = Start, tile 29 = Finish).
/// On screen (and in the Inspector) they are shown as 1 to 30.
/// </summary>
public class BoardLayout
{
    public const int TileCount = 30;
    public const int Columns = 6;
    public const int Rows = TileCount / Columns;

    /// <summary>Distance between tile centers in world units.</summary>
    public const float Spacing = 1.55f;

    public int LastTile { get { return TileCount - 1; } }

    // Ladder bottom -> ladder top (code numbers, 0 to 29)
    public readonly Dictionary<int, int> Ladders = new Dictionary<int, int>();

    // Snake head -> snake tail (code numbers, 0 to 29)
    public readonly Dictionary<int, int> Snakes = new Dictionary<int, int>();

    public readonly HashSet<int> CoinTiles = new HashSet<int>();
    public readonly HashSet<int> EventTiles = new HashSet<int>();

    /// <summary>
    /// Reads the board from the Inspector settings. Screen numbers (1 to 30) are
    /// turned into code numbers (0 to 29). Bad entries are skipped with a warning
    /// in the Console, so a typo cannot break the game.
    /// </summary>
    public BoardLayout(GameSettings settings)
    {
        var used = new HashSet<int>(); // each tile can only have one job

        foreach (TileLink link in settings.ladders)
        {
            if (!ValidLink(link, true, used, "Ladder")) continue;
            Ladders[link.from - 1] = link.to - 1;
            used.Add(link.from - 1);
        }
        foreach (TileLink link in settings.snakes)
        {
            if (!ValidLink(link, false, used, "Snake")) continue;
            Snakes[link.from - 1] = link.to - 1;
            used.Add(link.from - 1);
        }
        foreach (int tile in settings.coinTiles)
        {
            if (ValidTile(tile, used, "Coin tile")) { CoinTiles.Add(tile - 1); used.Add(tile - 1); }
        }
        foreach (int tile in settings.eventTiles)
        {
            if (ValidTile(tile, used, "Event tile")) { EventTiles.Add(tile - 1); used.Add(tile - 1); }
        }
    }

    private bool ValidTile(int screenNumber, HashSet<int> used, string what)
    {
        if (screenNumber < 2 || screenNumber > TileCount - 1)
        {
            Debug.LogWarning(what + " " + screenNumber + " skipped: use tiles 2 to " + (TileCount - 1) + ".");
            return false;
        }
        if (used.Contains(screenNumber - 1))
        {
            Debug.LogWarning(what + " " + screenNumber + " skipped: that tile already has something on it.");
            return false;
        }
        return true;
    }

    private bool ValidLink(TileLink link, bool goesUp, HashSet<int> used, string what)
    {
        if (!ValidTile(link.from, used, what)) return false;
        if (link.to < 1 || link.to > TileCount)
        {
            Debug.LogWarning(what + " " + link.from + " to " + link.to + " skipped: the end tile must be 1 to " + TileCount + ".");
            return false;
        }
        if (goesUp != (link.to > link.from))
        {
            Debug.LogWarning(what + " " + link.from + " to " + link.to + " skipped: ladders must go up and snakes must go down.");
            return false;
        }
        return true;
    }

    /// <summary>What happens on this tile.</summary>
    public TileType GetTileType(int tile)
    {
        if (tile == 0) return TileType.Start;
        if (tile == LastTile) return TileType.Finish;
        if (Ladders.ContainsKey(tile)) return TileType.Ladder;
        if (Snakes.ContainsKey(tile)) return TileType.Snake;
        if (CoinTiles.Contains(tile)) return TileType.Coin;
        if (EventTiles.Contains(tile)) return TileType.LifeEvent;
        return TileType.Normal;
    }

    /// <summary>
    /// World position of a tile's center. The path zigzags: row 1 goes left to right,
    /// row 2 goes right to left, and so on. The whole board is centered on (0, 0).
    /// </summary>
    public Vector3 TilePosition(int tile)
    {
        int row = tile / Columns;
        int col = tile % Columns;
        if (row % 2 == 1) col = Columns - 1 - col;

        float x = (col - (Columns - 1) / 2f) * Spacing;
        float y = (row - (Rows - 1) / 2f) * Spacing;
        return new Vector3(x, y, 0f);
    }

    /// <summary>Size of the whole board in world units (used for the frame and camera).</summary>
    public Vector2 BoardSize
    {
        get { return new Vector2(Columns * Spacing, Rows * Spacing); }
    }
}
