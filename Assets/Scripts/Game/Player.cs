using UnityEngine;

/// <summary>
/// Everything the game needs to know about one player.
/// The Token is only the picture on the board; the real position is Tile.
/// </summary>
public class Player
{
    public string Name;
    public Color Color;
    public string PawnImage;       // which pawn sprite to use
    public bool IsComputer;

    public int Tile;               // 0 = Start, BoardLayout.TileCount - 1 = Finish
    public int Coins;
    public int SkipTurns;          // turns to skip (from the "Car Trouble" card)
    public int FreeLuckyRolls;     // Lucky Die rolls that cost nothing

    // Simple stats shown on the win screen
    public int SnakesHit;
    public int LaddersClimbed;
    public int SnakesEscaped;

    public Token Token;

    public Player(string name, Color color, string pawnImage, bool isComputer)
    {
        Name = name;
        Color = color;
        PawnImage = pawnImage;
        IsComputer = isComputer;
    }

    /// <summary>True if the player can use the Lucky Die right now.</summary>
    public bool CanUseLuckyDie
    {
        get { return Coins >= Dice.LuckyDieCost || FreeLuckyRolls > 0; }
    }
}
