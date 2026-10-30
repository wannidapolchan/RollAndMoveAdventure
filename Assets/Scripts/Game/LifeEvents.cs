using System.Collections.Generic;
using UnityEngine;

/// <summary>What a Life Event card does.</summary>
public enum EventEffect
{
    GainCoins,          // player gets coins
    LoseCoins,          // player pays coins
    MoveForward,        // player moves ahead (no extra tile effect)
    MoveBack,           // player moves back (no extra tile effect)
    SkipTurn,           // player skips their next turn
    ExtraTurn,          // player rolls again right away
    TakeCoinFromOthers, // every other player gives the player a coin
    EveryoneBack,       // all players move back
    FreeLuckyRoll       // next Lucky Die roll is free
}

/// <summary>One Life Event card.</summary>
public class LifeEventCard
{
    public string Title;
    public string Story;     // short flavor text
    public string RuleText;  // what actually happens, shown in bold
    public EventEffect Effect;
    public int Amount;
    public bool IsGood;

    public LifeEventCard(string title, string story, string ruleText, EventEffect effect, int amount, bool isGood)
    {
        Title = title;
        Story = story;
        RuleText = ruleText;
        Effect = effect;
        Amount = amount;
        IsGood = isGood;
    }
}

/// <summary>
/// The deck of Life Event cards. Cards are shuffled and drawn one by one,
/// so the same card does not come up again until the whole deck has been used.
/// </summary>
public class LifeEventDeck
{
    private readonly List<LifeEventCard> allCards = new List<LifeEventCard>
    {
        new LifeEventCard("Payday!", "Your part-time job finally pays off.", "+3 coins", EventEffect.GainCoins, 3, true),
        new LifeEventCard("Found a Wallet", "You return a lost wallet and get a reward.", "+2 coins", EventEffect.GainCoins, 2, true),
        new LifeEventCard("Shortcut", "You know a secret path through the park.", "Move forward 3", EventEffect.MoveForward, 3, true),
        new LifeEventCard("Lucky Day", "Everything is going your way today.", "Roll again!", EventEffect.ExtraTurn, 0, true),
        new LifeEventCard("Birthday Party", "Your friends bring presents.", "Take 1 coin from each player", EventEffect.TakeCoinFromOthers, 1, true),
        new LifeEventCard("Four-Leaf Clover", "You spot one in the grass.", "Next Lucky Die roll is free", EventEffect.FreeLuckyRoll, 1, true),
        new LifeEventCard("Car Trouble", "Your car will not start this morning.", "Skip your next turn", EventEffect.SkipTurn, 1, false),
        new LifeEventCard("Forgot Homework", "You have to run back home to get it.", "Move back 3", EventEffect.MoveBack, 3, false),
        new LifeEventCard("Phone Bill", "Streaming every night was not cheap.", "Pay 2 coins", EventEffect.LoseCoins, 2, false),
        new LifeEventCard("Rainy Day", "A storm slows everyone down.", "Everyone moves back 2", EventEffect.EveryoneBack, 2, false)
    };

    private readonly List<LifeEventCard> drawPile = new List<LifeEventCard>();

    /// <summary>Takes the top card. Reshuffles when the pile is empty.</summary>
    public LifeEventCard Draw()
    {
        if (drawPile.Count == 0) Shuffle();
        LifeEventCard card = drawPile[drawPile.Count - 1];
        drawPile.RemoveAt(drawPile.Count - 1);
        return card;
    }

    /// <summary>Puts all cards back and shuffles them (Fisher-Yates shuffle).</summary>
    public void Shuffle()
    {
        drawPile.Clear();
        drawPile.AddRange(allCards);
        for (int i = drawPile.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            LifeEventCard temp = drawPile[i];
            drawPile[i] = drawPile[j];
            drawPile[j] = temp;
        }
    }
}
