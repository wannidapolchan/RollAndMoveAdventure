using System.Collections.Generic;
using UnityEngine;

/// <summary>Drawing order for sprites (higher numbers draw on top).</summary>
public static class SortOrder
{
    public const int Background = -100;
    public const int Frame = -10;
    public const int Tiles = 0;
    public const int TileLabels = 1;
    public const int Ladders = 5;
    public const int Snakes = 6;
    public const int Tokens = 20;
    public const int Effects = 100;
    public const int FloatingText = 110;
}

/// <summary>
/// Everything you see on the board: background, frame, tiles (with their numbers
/// and icons), ladders and snakes.
///
/// The board can be built two ways:
///   1. In the editor, with the "Build Board in Scene" button on GameManager.
///      The objects are saved in the scene, so you can see them and move them
///      without pressing Play. The game then uses your positions.
///   2. Automatically when the game starts, if there is no board in the scene.
///
/// Tokens always go to the real position of each tile object, and slide along
/// the real snake objects, so moving things in the scene works in the game too.
/// </summary>
public class BoardView : MonoBehaviour
{
    // Where the head and tail are in a snake image, as a fraction of its height
    // (0 = top, 1 = bottom). The snake image must stand up, head at the top.
    private const float SnakeHeadY = 0.064f;      // center of the head
    private const float SnakeBodyStartY = 0.082f; // where the body starts below the head
    private const float SnakeTailY = 0.945f;      // tip of the tail
    private const float SnakeWave = 0.21f;        // how far the body waves sideways (fraction of width)

    private static readonly string[] SnakeColors = { "snake_green", "snake_purple", "snake_red" };

    private BoardLayout layout;
    private GameSettings settings;
    private Transform[] tiles = new Transform[BoardLayout.TileCount];
    private readonly Dictionary<int, BoardLink> snakes = new Dictionary<int, BoardLink>();

    /// <summary>The background image (a child of the board).</summary>
    public SpriteRenderer Background { get; private set; }

    // ================================================================ build or reuse

    /// <summary>Removes the old board objects and builds a new board from the settings.</summary>
    public void Build(BoardLayout boardLayout, GameSettings gameSettings)
    {
        layout = boardLayout;
        settings = gameSettings;
        ClearChildren();
        tiles = new Transform[BoardLayout.TileCount];
        snakes.Clear();

        CreateBackground();
        CreateFrame();
        for (int i = 0; i < BoardLayout.TileCount; i++)
        {
            CreateTile(i);
        }
        foreach (var ladder in layout.Ladders)
        {
            CreateLadder(ladder.Key, ladder.Value);
        }
        int colorIndex = 0;
        foreach (var snake in layout.Snakes)
        {
            CreateSnake(snake.Key, snake.Value, SnakeColors[colorIndex % SnakeColors.Length]);
            colorIndex++;
        }
    }

    /// <summary>
    /// Uses a board that was built in the scene. If it does not match the
    /// Inspector settings (for example a snake was changed after building),
    /// a fresh board is built for this game instead, and a warning explains why.
    /// </summary>
    public void UseExisting(BoardLayout boardLayout, GameSettings gameSettings)
    {
        layout = boardLayout;
        settings = gameSettings;
        if (!CollectExisting())
        {
            Debug.LogWarning("The board in the scene does not match the Settings, so a fresh board is used for this game. " +
                             "Click 'Build Board in Scene' on the Game object to update the scene.");
            Build(boardLayout, gameSettings);
        }
    }

    /// <summary>Finds the tile, snake and ladder objects already in the scene. Returns false if something is missing.</summary>
    private bool CollectExisting()
    {
        tiles = new Transform[BoardLayout.TileCount];
        snakes.Clear();
        var ladders = new Dictionary<int, int>();

        foreach (BoardTile tile in GetComponentsInChildren<BoardTile>(true))
        {
            if (tile.index >= 0 && tile.index < tiles.Length) tiles[tile.index] = tile.transform;
        }
        foreach (BoardLink link in GetComponentsInChildren<BoardLink>(true))
        {
            if (link.isSnake) snakes[link.from] = link;
            else ladders[link.from] = link.to;
        }
        Transform bg = transform.Find("Background");
        Background = bg != null ? bg.GetComponent<SpriteRenderer>() : null;

        foreach (Transform t in tiles)
        {
            if (t == null) return false;
        }
        foreach (var snake in layout.Snakes)
        {
            if (!snakes.ContainsKey(snake.Key) || snakes[snake.Key].to != snake.Value) return false;
        }
        foreach (var ladder in layout.Ladders)
        {
            int top;
            if (!ladders.TryGetValue(ladder.Key, out top) || top != ladder.Value) return false;
        }
        return snakes.Count == layout.Snakes.Count && ladders.Count == layout.Ladders.Count;
    }

    private void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }
    }

    // ================================================================ building helpers

    private GameObject NewChild(string objectName, Transform parent, Vector3 localPosition)
    {
        var go = new GameObject(objectName);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        return go;
    }

    /// <summary>Adds an image and scales it so its longest side is "size" world units.</summary>
    private SpriteRenderer AddImage(GameObject go, string image, float size, int order)
    {
        var sr = GameAssets.AddSpriteRenderer(go);
        sr.sprite = GameAssets.SceneSprite(image);
        sr.sortingOrder = order;
        Vector2 bounds = sr.sprite.bounds.size;
        float scale = size / Mathf.Max(bounds.x, bounds.y);
        go.transform.localScale = new Vector3(scale, scale, 1f);
        return sr;
    }

    private void CreateBackground()
    {
        var go = NewChild("Background", transform, new Vector3(0f, 0f, 5f));
        Background = GameAssets.AddSpriteRenderer(go);
        Background.sprite = GameAssets.SceneSprite("background");
        Background.sortingOrder = SortOrder.Background;
    }

    private void CreateFrame()
    {
        var go = NewChild("Board Frame", transform, Vector3.zero);
        var sr = GameAssets.AddSpriteRenderer(go);
        sr.sprite = GameAssets.SceneSlicedSprite("panel_dark");
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = layout.BoardSize + new Vector2(0.5f, 0.5f);
        sr.sortingOrder = SortOrder.Frame;
    }

    /// <summary>
    /// A tile is an empty object (with a BoardTile marker) holding its image,
    /// number and icon, so moving the tile moves everything on it.
    /// </summary>
    private void CreateTile(int tile)
    {
        TileType type = layout.GetTileType(tile);
        var tileObject = NewChild("Tile " + (tile + 1), transform, layout.TilePosition(tile));
        tileObject.AddComponent<BoardTile>().index = tile;
        tiles[tile] = tileObject.transform;

        AddImage(NewChild("Image", tileObject.transform, Vector3.zero), TileImageName(tile, type),
                 BoardLayout.Spacing * 0.95f, SortOrder.Tiles);

        // Start and Finish show words instead of a number.
        if (type != TileType.Start && type != TileType.Finish)
        {
            CreateTileNumber(tile, tileObject.transform);
        }
        CreateTileDecoration(type, tileObject.transform);
    }

    private static string TileImageName(int tile, TileType type)
    {
        switch (type)
        {
            case TileType.Start: return "tile_start";
            case TileType.Finish: return "tile_finish";
            case TileType.Coin: return "tile_coin";
            case TileType.LifeEvent: return "tile_event";
            default: return tile % 2 == 0 ? "tile_a" : "tile_b"; // checker pattern
        }
    }

    /// <summary>Adds the icon or word that shows what a tile does.</summary>
    private void CreateTileDecoration(TileType type, Transform tile)
    {
        float s = BoardLayout.Spacing;
        Vector3 iconSpot = new Vector3(0.06f, -0.05f, 0f) * s;
        switch (type)
        {
            case TileType.Coin:
                AddImage(NewChild("Icon", tile, iconSpot), "coin", 0.5f * s, SortOrder.TileLabels);
                break;
            case TileType.LifeEvent:
                AddImage(NewChild("Icon", tile, iconSpot), "icon_event", 0.85f * s, SortOrder.TileLabels);
                break;
            case TileType.Start:
                CreateWord("START", tile, Vector3.zero, 0.055f);
                break;
            case TileType.Finish:
                AddImage(NewChild("Icon", tile, new Vector3(0f, 0.1f, 0f) * s), "icon_flag", 0.55f * s, SortOrder.TileLabels);
                CreateWord("FINISH", tile, new Vector3(0f, -0.3f, 0f) * s, 0.042f);
                break;
        }
    }

    private void CreateTileNumber(int tile, Transform tileObject)
    {
        Vector3 corner = new Vector3(-0.42f, 0.36f, 0f) * BoardLayout.Spacing;
        MakeText((tile + 1).ToString(), tileObject, corner, 0.045f, TextAnchor.MiddleLeft,
                 settings.tileNumberColor, SortOrder.TileLabels);
    }

    /// <summary>Writes a word with a soft shadow (light shadow for dark text, dark shadow for light text).</summary>
    private void CreateWord(string word, Transform tile, Vector3 position, float characterSize)
    {
        Color color = settings.startFinishWordColor;
        float brightness = (color.r + color.g + color.b) / 3f;
        Color shadow = brightness < 0.5f ? new Color(1f, 1f, 1f, 0.5f) : new Color(0.1f, 0.15f, 0.12f, 0.6f);

        Vector3 shadowOffset = new Vector3(0.03f, -0.03f, 0f);
        MakeText(word, tile, position + shadowOffset, characterSize, TextAnchor.MiddleCenter, shadow, SortOrder.TileLabels);
        MakeText(word, tile, position, characterSize, TextAnchor.MiddleCenter, color, SortOrder.TileLabels + 1);
    }

    private void MakeText(string content, Transform parent, Vector3 position, float characterSize,
                          TextAnchor anchor, Color color, int order)
    {
        var go = NewChild("Text " + content, parent, position);
        var text = go.AddComponent<TextMesh>();
        text.font = GameAssets.MainFont;
        text.text = content;
        text.fontSize = 64;
        text.characterSize = characterSize;
        text.anchor = anchor;
        text.color = color;

        var mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = GameAssets.MainFont.material;
        mr.sortingOrder = order;
    }

    /// <summary>Stretches the ladder image between two tiles and rotates it to point up the board.</summary>
    private void CreateLadder(int bottomTile, int topTile)
    {
        Vector3 bottom = layout.TilePosition(bottomTile);
        Vector3 top = layout.TilePosition(topTile);
        Vector3 dir = top - bottom;

        var go = NewChild("Ladder " + (bottomTile + 1) + " to " + (topTile + 1), transform, (bottom + top) / 2f);
        go.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(-dir.x, dir.y) * Mathf.Rad2Deg);

        var sr = GameAssets.AddSpriteRenderer(go);
        sr.sprite = GameAssets.SceneSprite("ladder");
        sr.sortingOrder = SortOrder.Ladders;
        Vector2 bounds = sr.sprite.bounds.size;
        go.transform.localScale = new Vector3(BoardLayout.Spacing * 0.5f / bounds.x,
                                              (dir.magnitude + BoardLayout.Spacing * 0.35f) / bounds.y, 1f);

        BoardLink link = go.AddComponent<BoardLink>();
        link.from = bottomTile;
        link.to = topTile;
        link.isSnake = false;
    }

    /// <summary>
    /// Places a snake with its head on the head tile and its tail on the tail tile:
    /// stretch it to the right length, rotate it so it points at the tail,
    /// then move it so the head sits on the head tile.
    /// </summary>
    private void CreateSnake(int headTile, int tailTile, string imageName)
    {
        Vector3 head = layout.TilePosition(headTile);
        Vector3 tail = layout.TilePosition(tailTile);
        Vector3 dir = tail - head;

        var go = NewChild("Snake " + (headTile + 1) + " to " + (tailTile + 1), transform, Vector3.zero);
        var sr = GameAssets.AddSpriteRenderer(go);
        sr.sprite = GameAssets.SceneSprite(imageName);
        sr.sortingOrder = SortOrder.Snakes;

        Vector2 bounds = sr.sprite.bounds.size;
        float scaleX = BoardLayout.Spacing * 0.9f / bounds.x;
        float scaleY = dir.magnitude / ((SnakeTailY - SnakeHeadY) * bounds.y);
        Quaternion rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.x, -dir.y) * Mathf.Rad2Deg);

        Vector3 headOffset = new Vector3(0f, (0.5f - SnakeHeadY) * bounds.y * scaleY, 0f);
        go.transform.localScale = new Vector3(scaleX, scaleY, 1f);
        go.transform.localRotation = rotation;
        go.transform.localPosition = head - rotation * headOffset;

        BoardLink link = go.AddComponent<BoardLink>();
        link.from = headTile;
        link.to = tailTile;
        link.isSnake = true;
        snakes[headTile] = link;
    }

    // ================================================================ positions for the game

    /// <summary>World position of a tile: wherever its object is in the scene.</summary>
    public Vector3 TileWorldPosition(int tile)
    {
        if (tiles[tile] != null) return tiles[tile].position;
        return transform.TransformPoint(layout.TilePosition(tile));
    }

    /// <summary>
    /// Points along a snake's wavy body, from head to tail, in world space.
    /// Uses the snake object's real position, rotation and size, so the pawn
    /// follows the snake even if you moved it in the scene.
    /// </summary>
    public List<Vector3> SnakePath(int headTile)
    {
        var points = new List<Vector3>();
        BoardLink link;
        if (!snakes.TryGetValue(headTile, out link) || link == null) return points;

        SpriteRenderer sr = link.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return points;
        Vector2 bounds = sr.sprite.bounds.size; // in the snake's own (unscaled) units

        const int steps = 40;
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            float amplitude = SnakeWave * bounds.x * (0.6f + 0.4f * (1f - t));
            float x = amplitude * Mathf.Sin(t * Mathf.PI * 4.2f);
            float y = (0.5f - Mathf.Lerp(SnakeBodyStartY, SnakeTailY, t)) * bounds.y;
            points.Add(link.transform.TransformPoint(new Vector3(x, y, 0f)));
        }
        return points;
    }
}
