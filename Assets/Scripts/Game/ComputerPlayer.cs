using System.Collections.Generic;

/// <summary>
/// Simple decisions for the computer player. It is not perfect on purpose,
/// so a human can beat it, but it uses its coins in a sensible way.
/// </summary>
public static class ComputerPlayer
{
    /// <summary>Picks which die to roll this turn.</summary>
    public static DieType ChooseDie(Player me, List<Player> allPlayers, int lastTile)
    {
        if (!me.CanUseLuckyDie) return DieType.Plain;
        if (me.FreeLuckyRolls > 0) return DieType.Lucky;    // free, so always use it

        // Keep coins for escaping snakes if we only have one.
        if (me.Coins < 2) return DieType.Plain;

        // Close to the finish, the Plain Die is safer (no -2 or 0 faces).
        if (lastTile - me.Tile <= 6) return DieType.Plain;

        // Take the risk when someone is far ahead.
        int leaderTile = 0;
        foreach (Player p in allPlayers)
        {
            if (p != me && p.Tile > leaderTile) leaderTile = p.Tile;
        }
        if (leaderTile - me.Tile >= 4) return DieType.Lucky;

        // Rich enough to gamble.
        return me.Coins >= 3 ? DieType.Lucky : DieType.Plain;
    }

    /// <summary>Decides whether to pay to escape a snake.</summary>
    public static bool ShouldEscapeSnake(Player me, int tilesLost)
    {
        // Every snake on this board costs a lot of tiles, so pay when we can.
        return me.Coins >= 1 && tilesLost >= 4;
    }
}
