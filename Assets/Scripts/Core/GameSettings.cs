using System.Collections.Generic;
using UnityEngine;

/// <summary>A ladder or snake: goes from one tile to another (tile numbers 1 to 30, as on screen).</summary>
[System.Serializable]
public struct TileLink
{
    public int from;
    public int to;

    public TileLink(int from, int to)
    {
        this.from = from;
        this.to = to;
    }
}

/// <summary>
/// Every setting you can change in the Inspector. It lives on the GameManager
/// component. Change values while the game is NOT playing, then save the scene.
///
/// All tile numbers here are the numbers you see on the board (1 to 30).
/// </summary>
[System.Serializable]
public class GameSettings
{
    [Header("Board (tile numbers 1 to 30, as shown on screen)")]
    [Tooltip("Ladders: 'from' is the bottom tile, 'to' is the top tile.")]
    public List<TileLink> ladders = new List<TileLink>
    {
        new TileLink(2, 14), new TileLink(8, 20), new TileLink(22, 27)
    };

    [Tooltip("Snakes: 'from' is the head tile, 'to' is the tail tile.")]
    public List<TileLink> snakes = new List<TileLink>
    {
        new TileLink(18, 4), new TileLink(24, 12), new TileLink(28, 16)
    };

    [Tooltip("Tiles that give 1 or 2 coins.")]
    public List<int> coinTiles = new List<int> { 5, 10, 23, 26 };

    [Tooltip("Tiles that draw a Life Event card.")]
    public List<int> eventTiles = new List<int> { 3, 9, 19, 25, 29 };

    [Header("Characters (image names in Resources/Art, without .png)")]
    [Tooltip("Players pick from these on the character select screen. Add as many as you like (at least 2).")]
    public List<string> characterChoices = new List<string> { "pawn_blue", "pawn_orange" };
    [Tooltip("How big a character is on the board (its longest side, in world units). A tile is about 1.5.")]
    [Range(0.4f, 1.4f)] public float characterSize = 0.95f;
    [Tooltip("A soft dark oval under each character.")]
    public bool showCharacterShadow = false;
    [Tooltip("A glowing oval in the player's color under whoever's turn it is.")]
    public bool showTurnRing = false;

    [Header("Colors")]
    public Color startFinishWordColor = Color.black;
    public Color tileNumberColor = Color.white;
    public Color player1Color = new Color(0.27f, 0.55f, 0.94f);
    public Color player2Color = new Color(0.98f, 0.55f, 0.2f);

    [Header("Camera")]
    [Tooltip("On: the camera and background are placed for you so the board fits next to the HUD. " +
             "Off: they stay exactly where you put them in the scene.")]
    public bool autoFrameCamera = true;

    [Header("Gameplay")]
    [Range(0, 10)] public int startingCoins = 1;
    [Tooltip("Seconds for a pawn to hop one tile.")]
    [Range(0.05f, 1f)] public float stepTime = 0.2f;
}
