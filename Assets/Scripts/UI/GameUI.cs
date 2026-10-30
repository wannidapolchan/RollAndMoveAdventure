using UnityEngine;

/// <summary>
/// Draws every screen with Unity's immediate mode GUI (OnGUI):
/// title screen, how to play, the HUD, the snake choice, Life Event cards,
/// pause menu and win screen. It also reads the keyboard.
///
/// The UI never changes game rules itself. It only reads GameManager and
/// calls its public methods (RequestRoll, SelectDie, AnswerSnake, ...).
/// </summary>
public class GameUI : MonoBehaviour
{
    public const float HudWidth = 620f; // design pixels, must match GameManager

    private GameManager game;
    private UIStyles ui;
    private bool showHowToPlay;

    public void Init(GameManager manager)
    {
        game = manager;
        ui = new UIStyles(manager.Sound);
    }

    private void OnGUI()
    {
        if (game == null) return;
        ui.Begin();
        HandleKeyboard();

        if (game.State == GameState.Title)
        {
            DrawTitleScreen();
            return;
        }
        if (game.State == GameState.ChoosingCharacters)
        {
            DrawCharacterSelect();
            return;
        }

        DrawHud();
        DrawBanner();
        if (game.Phase == TurnPhase.SnakeChoice) DrawSnakeChoice();
        if (game.CurrentCard != null) DrawCard(game.CurrentCard);
        if (game.State == GameState.GameOver) DrawWinScreen();
        if (game.IsPaused) DrawPauseMenu();
    }

    // ================================================================ keyboard

    private void HandleKeyboard()
    {
        Event e = Event.current;
        if (e.type != EventType.KeyDown) return;
        if (HandleKey(e.keyCode)) e.Use();
    }

    /// <summary>Returns true if the key did something.</summary>
    private bool HandleKey(KeyCode key)
    {
        bool confirm = key == KeyCode.Space || key == KeyCode.Return || key == KeyCode.KeypadEnter;
        bool one = key == KeyCode.Alpha1 || key == KeyCode.Keypad1;
        bool two = key == KeyCode.Alpha2 || key == KeyCode.Keypad2;

        if (game.State == GameState.Title)
        {
            if (showHowToPlay)
            {
                if (confirm || key == KeyCode.Escape || key == KeyCode.H) { showHowToPlay = false; return true; }
                return false;
            }
            if (one) { game.RequestStart(true); return true; }
            if (two) { game.RequestStart(false); return true; }
            if (key == KeyCode.H) { showHowToPlay = true; game.Sound.Play("click"); return true; }
            return false;
        }

        if (game.State == GameState.ChoosingCharacters)
        {
            if (key == KeyCode.LeftArrow || key == KeyCode.A) { game.MoveHighlight(-1); return true; }
            if (key == KeyCode.RightArrow || key == KeyCode.D) { game.MoveHighlight(1); return true; }
            if (confirm) { game.ChooseCharacter(game.HighlightedChoice); return true; }
            if (key == KeyCode.Escape) { game.RequestMainMenu(); return true; }
            return false;
        }

        if (game.State == GameState.GameOver)
        {
            if (confirm) { game.RequestRestart(); return true; }
            if (key == KeyCode.Escape) { game.RequestMainMenu(); return true; }
            return false;
        }

        // Playing
        if (key == KeyCode.Escape) { game.RequestTogglePause(); return true; }
        if (game.IsPaused)
        {
            if (key == KeyCode.R) { game.RequestRestart(); return true; }
            if (key == KeyCode.M) { game.RequestMainMenu(); return true; }
            return false;
        }

        switch (game.Phase)
        {
            case TurnPhase.ChoosingDie:
                if (one) { game.SelectDie(DieType.Plain); return true; }
                if (two) { game.SelectDie(DieType.Lucky); return true; }
                if (confirm) { game.RequestRoll(); return true; }
                break;
            case TurnPhase.SnakeChoice:
                if (key == KeyCode.Y) { game.AnswerSnake(true); return true; }
                if (key == KeyCode.N) { game.AnswerSnake(false); return true; }
                break;
            case TurnPhase.ShowingCard:
                if (confirm) { game.CloseCard(); return true; }
                break;
        }
        return false;
    }

    // ================================================================ title screen

    private void DrawTitleScreen()
    {
        float w = ui.Width;
        float cx = w / 2f;
        ui.Fill(ui.R(0, 0, w, 1080), new Color(0.05f, 0.09f, 0.12f, 0.72f));

        // Two pawns bouncing on each side of the title.
        float bounceA = Mathf.Abs(Mathf.Sin(Time.time * 3f)) * 30f;
        float bounceB = Mathf.Abs(Mathf.Sin(Time.time * 3f + 1.4f)) * 30f;
        // Show the first two characters from the Inspector list.
        var choices = game.CharacterChoices;
        if (choices.Count > 0) ui.Image(ui.R(cx - 640, 150 - bounceA, 140, 190), choices[0]);
        if (choices.Count > 1) ui.Image(ui.R(cx + 500, 150 - bounceB, 140, 190), choices[1]);

        ui.ShadowLabel(ui.R(cx - 500, 120, 1000, 130), "Roll-and-Move", 120, UIStyles.Gold);
        ui.ShadowLabel(ui.R(cx - 500, 240, 1000, 100), "Adventure", 84, Color.white);
        ui.Label(ui.R(cx - 500, 350, 1000, 50), "Snakes, ladders, life events and a risky Lucky Die", 30, UIStyles.Muted);

        if (showHowToPlay)
        {
            DrawHowToPlay(cx);
            return;
        }

        if (ui.Button(ui.R(cx - 300, 450, 600, 100), "1 Player vs Computer  [1]", 38, UIStyles.ButtonColor.Orange))
            game.RequestStart(true);
        if (ui.Button(ui.R(cx - 300, 570, 600, 100), "2 Players  [2]", 38, UIStyles.ButtonColor.Blue))
            game.RequestStart(false);
        if (ui.Button(ui.R(cx - 200, 700, 400, 80), "How to Play  [H]", 30, UIStyles.ButtonColor.Blue))
            showHowToPlay = true;

        ui.Label(ui.R(cx - 500, 1000, 1000, 40), "Made by Wannida Polchan for CS 583", 22, UIStyles.Muted);
    }

    private void DrawHowToPlay(float cx)
    {
        ui.DarkPanel(ui.R(cx - 520, 420, 1040, 560));
        ui.Label(ui.R(cx - 480, 440, 960, 60), "How to Play", 44, UIStyles.Gold);

        string rules =
            "Race to tile 30. The first character on the Finish tile wins.\n\n" +
            "Each turn, pick a die and roll:\n" +
            "   Plain Die: 1 to 6, free.\n" +
            "   Lucky Die: -2, 0, 3, 5, 8 or 10. Costs 1 coin.\n\n" +
            "Ladders climb up. Snakes slide down, unless you pay 1 coin to escape!\n" +
            "Coin tiles give 1 or 2 coins. Card tiles (with a ?) draw a Life Event card.\n\n" +
            "Keys: 1 / 2 choose die,  Space roll,  Y / N snake choice,  Esc pause";
        ui.Label(ui.R(cx - 470, 505, 940, 380), rules, 27, UIStyles.SoftWhite, TextAnchor.UpperLeft, true);

        if (ui.Button(ui.R(cx - 150, 890, 300, 70), "Back", 30, UIStyles.ButtonColor.Orange))
            showHowToPlay = false;
    }

    // ================================================================ character select

    /// <summary>
    /// A row of characters to pick from. Player 1 picks first, then Player 2
    /// (or the computer picks a random one). Taken characters are grayed out.
    /// </summary>
    private void DrawCharacterSelect()
    {
        float w = ui.Width;
        float cx = w / 2f;
        ui.Fill(ui.R(0, 0, w, 1080), new Color(0.05f, 0.09f, 0.12f, 0.72f));

        bool playerTwo = game.PickingPlayer == 1;
        Color color = playerTwo ? game.Settings.player2Color : game.Settings.player1Color;
        string who = playerTwo ? "Player 2" : "Player 1";
        ui.ShadowLabel(ui.R(cx - 600, 150, 1200, 100), who + ", choose your character!", 64, color);
        ui.Label(ui.R(cx - 600, 250, 1200, 50), "Click one, or use Left / Right and Space", 28, UIStyles.Muted);

        // Lay the characters out in rows of up to 6, centered.
        int count = game.CharacterChoices.Count;
        const int perRow = 6;
        const float cardW = 200f, cardH = 240f, gap = 24f;
        for (int i = 0; i < count; i++)
        {
            int row = i / perRow;
            int inRow = Mathf.Min(perRow, count - row * perRow);
            float rowWidth = inRow * cardW + (inRow - 1) * gap;
            float x = cx - rowWidth / 2f + (i % perRow) * (cardW + gap);
            float y = 340f + row * (cardH + gap);
            DrawCharacterCard(i, ui.R(x, y, cardW, cardH));
        }

        if (ui.Button(ui.R(cx - 150, 960, 300, 76), "Back  [Esc]", 30, UIStyles.ButtonColor.Blue))
            game.RequestMainMenu();
    }

    private void DrawCharacterCard(int index, Rect r)
    {
        float s = ui.Scale;
        int owner = game.WhoPicked(index);
        bool highlighted = index == game.HighlightedChoice && owner < 0;

        // Gold outline on the highlighted card
        if (highlighted)
        {
            float pad = 6f * s;
            ui.Fill(new Rect(r.x - pad, r.y - pad, r.width + pad * 2, r.height + pad * 2), UIStyles.Gold);
        }
        ui.DarkPanel(r);

        // Taken characters are faded, with the owner's name on them.
        float alpha = owner >= 0 ? 0.35f : 1f;
        float bounce = highlighted ? Mathf.Abs(Mathf.Sin(Time.time * 4f)) * 10f * s : 0f;
        Rect imageRect = new Rect(r.x + 25 * s, r.y + 20 * s - bounce, r.width - 50 * s, 150 * s);
        ui.Image(imageRect, game.CharacterChoices[index], alpha);

        string label = CharacterLabel(game.CharacterChoices[index]);
        ui.Label(new Rect(r.x, r.y + 180 * s, r.width, 40 * s), label, 26, new Color(1f, 1f, 1f, alpha));
        if (owner >= 0)
        {
            Color ownerColor = owner == 0 ? game.Settings.player1Color : game.Settings.player2Color;
            ui.ShadowLabel(new Rect(r.x, r.y + 60 * s, r.width, 60 * s), owner == 0 ? "P1" : "P2", 48, ownerColor);
        }
        else if (ui.ClickArea(r))
        {
            game.ChooseCharacter(index);
        }
    }

    /// <summary>Turns a file name like "panda_round" into "Panda Round".</summary>
    private static string CharacterLabel(string imageName)
    {
        string[] words = imageName.Replace('-', '_').Split('_');
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0) words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
        }
        return string.Join(" ", words);
    }

    // ================================================================ HUD

    private void DrawHud()
    {
        ui.DarkPanel(ui.R(24, 24, HudWidth - 48, 1032));
        ui.Label(ui.R(48, 36, 380, 50), "ROLL-AND-MOVE", 30, UIStyles.Gold, TextAnchor.MiddleLeft);
        ui.Label(ui.R(380, 36, 190, 50), "Turn " + Mathf.Max(1, game.TurnNumber), 24, UIStyles.Muted, TextAnchor.MiddleRight);

        for (int i = 0; i < game.Players.Count; i++)
        {
            DrawPlayerCard(game.Players[i], 96 + i * 150);
        }

        DrawDiceChoice(410);
        DrawDieDisplay(600);
        DrawRollButton(860);

        ui.Label(ui.R(48, 965, 524, 56), game.Message ?? "", 23, UIStyles.SoftWhite, TextAnchor.MiddleCenter, true);
        ui.Label(ui.R(48, 1018, 524, 30), "1 / 2 die     Space roll     Esc pause", 18, UIStyles.Muted);
    }

    private void DrawPlayerCard(Player p, float y)
    {
        bool isTurn = game.State == GameState.Playing && p == game.CurrentPlayer;
        ui.Fill(ui.R(44, y, 532, 136), new Color(1f, 1f, 1f, isTurn ? 0.12f : 0.05f));
        if (isTurn) ui.Fill(ui.R(44, y, 10, 136), p.Color); // colored bar = whose turn

        ui.Image(ui.R(66, y + 14, 80, 108), p.PawnImage);
        ui.Label(ui.R(162, y + 10, 260, 46), p.Name, 34, p.Color, TextAnchor.MiddleLeft);
        ui.Label(ui.R(162, y + 54, 260, 34), "Tile " + (p.Tile + 1) + " / " + BoardLayout.TileCount, 24, UIStyles.SoftWhite, TextAnchor.MiddleLeft);

        // Coins
        ui.Image(ui.R(440, y + 18, 44, 44), "coin");
        ui.Label(ui.R(490, y + 18, 80, 44), "x " + p.Coins, 32, UIStyles.Gold, TextAnchor.MiddleLeft);

        // Status line
        string status = p.IsComputer ? "Computer" : "";
        if (p.SkipTurns > 0) status = "Skips next turn";
        else if (p.FreeLuckyRolls > 0) status = "Free Lucky roll ready";
        Color statusColor = p.SkipTurns > 0 ? new Color(1f, 0.5f, 0.45f) : UIStyles.Muted;
        ui.Label(ui.R(162, y + 90, 400, 34), status, 21, statusColor, TextAnchor.MiddleLeft);
    }

    private void DrawDiceChoice(float y)
    {
        Player p = game.CurrentPlayer;
        bool canChoose = game.State == GameState.Playing && !game.IsPaused &&
                         game.Phase == TurnPhase.ChoosingDie && !p.IsComputer;

        ui.Label(ui.R(44, y, 532, 36), "CHOOSE YOUR DIE", 22, UIStyles.Muted);

        string luckyNote = p.FreeLuckyRolls > 0 ? "FREE roll!  [2]" : (p.Coins >= Dice.LuckyDieCost ? "1 coin  [2]" : "Need 1 coin");
        DrawDieOption(ui.R(44, y + 44, 256, 150), DieType.Plain, "die_plain_6", "Plain Die", "Free  [1]", true, canChoose);
        DrawDieOption(ui.R(320, y + 44, 256, 150), DieType.Lucky, "die_lucky_10", "Lucky Die", luckyNote, p.CanUseLuckyDie, canChoose);
    }

    private void DrawDieOption(Rect r, DieType type, string image, string title, string note, bool affordable, bool canChoose)
    {
        bool selected = game.SelectedDie == type && game.Phase >= TurnPhase.ChoosingDie && game.Phase <= TurnPhase.Rolling;

        // Gold outline when selected
        if (selected)
        {
            float pad = 5f * ui.Scale;
            ui.Fill(new Rect(r.x - pad, r.y - pad, r.width + pad * 2, r.height + pad * 2), UIStyles.Gold);
        }
        ui.Fill(r, selected ? new Color(0.18f, 0.28f, 0.36f, 1f) : new Color(1f, 1f, 1f, 0.06f));

        float alpha = affordable ? 1f : 0.35f;
        float s = ui.Scale;
        ui.Image(new Rect(r.x + 18 * s, r.y + 22 * s, 76 * s, 76 * s), image, alpha);
        ui.Label(new Rect(r.x + 102 * s, r.y + 22 * s, 150 * s, 40 * s), title, 26, new Color(1f, 1f, 1f, alpha), TextAnchor.MiddleLeft);
        ui.Label(new Rect(r.x + 102 * s, r.y + 60 * s, 150 * s, 36 * s), note, 19,
                 affordable ? UIStyles.Gold : new Color(1f, 0.5f, 0.45f), TextAnchor.MiddleLeft);

        string faces = type == DieType.Plain ? "1  2  3  4  5  6" : "-2  0  3  5  8  10";
        ui.Label(new Rect(r.x + 10 * s, r.y + 108 * s, r.width - 20 * s, 32 * s), faces, 20, UIStyles.Muted);

        if (canChoose && ui.ClickArea(r)) game.SelectDie(type);
    }

    /// <summary>The big die in the HUD. Tumbles while rolling, pops when it lands.</summary>
    private void DrawDieDisplay(float y)
    {
        float centerX = 310f;
        float size = 170f;
        float angle = 0f;
        float scale = 1f;

        if (game.DieRolling)
        {
            angle = Mathf.Sin(Time.time * 31f) * 25f;
            scale = 0.9f + Mathf.Abs(Mathf.Sin(Time.time * 18f)) * 0.15f;
        }
        else
        {
            float sinceLand = Time.time - game.DieLandTime;
            if (game.HasRolled && sinceLand < 0.3f) scale = 1f + Mathf.Sin(sinceLand / 0.3f * Mathf.PI) * 0.25f;
        }

        Rect r = ui.R(centerX - size * scale / 2f, y + 20 + (size - size * scale) / 2f, size * scale, size * scale);
        Matrix4x4 oldMatrix = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle, r.center);
        ui.Image(r, Dice.FaceImage(game.ShownDieType, game.ShownDieFace),
                 game.HasRolled || game.DieRolling ? 1f : 0.45f);
        GUI.matrix = oldMatrix;

        string result = "";
        if (game.DieRolling) result = "Rolling...";
        else if (game.HasRolled) result = "Rolled " + game.LastRoll + "!";
        ui.Label(ui.R(44, y + 200, 532, 44), result, 32, Color.white);
    }

    private void DrawRollButton(float y)
    {
        Player p = game.CurrentPlayer;
        bool myTurnToRoll = game.State == GameState.Playing && !game.IsPaused &&
                            game.Phase == TurnPhase.ChoosingDie && !p.IsComputer;

        string label = p.IsComputer && game.State == GameState.Playing ? "Computer's turn" : "ROLL!  [Space]";
        if (ui.Button(ui.R(44, y, 532, 96), label, 40, UIStyles.ButtonColor.Orange, myTurnToRoll))
            game.RequestRoll();
    }

    // ================================================================ banner

    /// <summary>"Player 1's Turn" sliding in above the board, then fading out.</summary>
    private void DrawBanner()
    {
        if (string.IsNullOrEmpty(game.BannerText) || game.State != GameState.Playing) return;
        float age = Time.time - game.BannerTime;
        if (age > 1.6f) return;

        float alpha = age < 1.1f ? 1f : 1f - (age - 1.1f) / 0.5f;
        float slide = age < 0.2f ? (1f - age / 0.2f) * -60f : 0f;
        float cx = BoardCenterX();
        Color c = game.BannerColor;
        ui.ShadowLabel(ui.R(cx - 500, 20 + slide, 1000, 90), game.BannerText, 64, new Color(c.r, c.g, c.b, alpha));
    }

    private float BoardCenterX()
    {
        return HudWidth + (ui.Width - HudWidth) / 2f;
    }

    // ================================================================ snake choice

    private void DrawSnakeChoice()
    {
        Player p = game.CurrentPlayer;
        float cx = BoardCenterX();
        Rect panel = ui.R(cx - 330, 380, 660, 300);
        ui.LightPanel(panel);

        ui.Label(ui.R(cx - 300, 395, 600, 70), "SNAKE!", 56, UIStyles.Bad);
        string question = "Pay 1 coin to escape, or slide back " + game.SnakeTilesAtRisk + " tiles?";
        ui.Label(ui.R(cx - 300, 465, 600, 80), question, 28, UIStyles.DarkText, TextAnchor.MiddleCenter, true);

        if (p.IsComputer)
        {
            ui.Label(ui.R(cx - 300, 570, 600, 60), "Computer is deciding...", 28, UIStyles.Muted);
            return;
        }

        bool canAnswer = !game.IsPaused;
        if (ui.Button(ui.R(cx - 300, 565, 290, 90), "Pay 1 coin  [Y]", 30, UIStyles.ButtonColor.Blue, canAnswer))
            game.AnswerSnake(true);
        if (ui.Button(ui.R(cx + 10, 565, 290, 90), "Slide down  [N]", 30, UIStyles.ButtonColor.Orange, canAnswer))
            game.AnswerSnake(false);
    }

    // ================================================================ life event card

    private void DrawCard(LifeEventCard card)
    {
        string image = card.IsGood ? "card_good" : "card_bad";
        Texture2D tex = GameAssets.Texture(image);

        // Keep the card image's own shape: fixed height, width from the image.
        float h = 600f;
        float w = h * tex.width / tex.height;
        float cx = BoardCenterX();
        float x = cx - w / 2f, y = 540f - h / 2f + 20f;

        // Flip-in animation: the card grows from a thin line to full width.
        float flip = Mathf.Clamp01((Time.time - game.CardShownTime) / 0.25f);
        flip = Mathf.SmoothStep(0f, 1f, flip);

        Matrix4x4 oldMatrix = GUI.matrix;
        Rect full = ui.R(x, y, w, h);
        GUIUtility.ScaleAroundPivot(new Vector2(Mathf.Max(0.01f, flip), 1f), full.center);

        // All text is white with a shadow, so it reads well on the colored card.
        ui.Image(full, image);
        ui.Label(ui.R(x + 40, y + 62, w - 80, 34), "LIFE EVENT", 22, new Color(1f, 1f, 1f, 0.8f));
        ui.ShadowLabel(ui.R(x + 40, y + 100, w - 80, 76), card.Title, 46, Color.white);
        ui.ShadowLabel(ui.R(x + 60, y + 190, w - 120, 130), card.Story, 28, Color.white, TextAnchor.MiddleCenter, true);
        ui.ShadowLabel(ui.R(x + 60, y + 325, w - 120, 110), card.RuleText, 38, Color.white, TextAnchor.MiddleCenter, true);

        bool canClose = flip >= 1f && !game.IsPaused;
        if (ui.Button(ui.R(cx - 130, y + 460, 260, 80), "OK  [Space]", 30, UIStyles.ButtonColor.Blue, canClose))
            game.CloseCard();

        GUI.matrix = oldMatrix;
    }

    // ================================================================ win and pause

    private void DrawWinScreen()
    {
        Player winner = game.Winner;
        if (winner == null) return;

        float cx = BoardCenterX();
        ui.Fill(ui.R(HudWidth, 0, ui.Width - HudWidth, 1080), new Color(0f, 0f, 0f, 0.35f));
        ui.DarkPanel(ui.R(cx - 400, 200, 800, 640));

        float bounce = Mathf.Abs(Mathf.Sin(Time.time * 4f)) * 20f;
        ui.Image(ui.R(cx - 70, 220 - bounce, 140, 190), winner.PawnImage);
        ui.ShadowLabel(ui.R(cx - 380, 420, 760, 100), winner.Name + " wins!", 76, winner.Color);

        string stats =
            "Ladders climbed: " + winner.LaddersClimbed + "\n" +
            "Snakes escaped: " + winner.SnakesEscaped + "     Snakes hit: " + winner.SnakesHit + "\n" +
            "Coins left: " + winner.Coins + "     Total turns: " + game.TurnNumber;
        ui.Label(ui.R(cx - 360, 520, 720, 140), stats, 28, UIStyles.SoftWhite, TextAnchor.MiddleCenter, true);

        if (ui.Button(ui.R(cx - 360, 700, 340, 96), "Play Again  [Space]", 32, UIStyles.ButtonColor.Orange))
            game.RequestRestart();
        if (ui.Button(ui.R(cx + 20, 700, 340, 96), "Main Menu  [Esc]", 32, UIStyles.ButtonColor.Blue))
            game.RequestMainMenu();
    }

    private void DrawPauseMenu()
    {
        float cx = ui.Width / 2f;
        ui.Fill(ui.R(0, 0, ui.Width, 1080), new Color(0f, 0f, 0f, 0.55f));
        ui.DarkPanel(ui.R(cx - 280, 270, 560, 520));
        ui.ShadowLabel(ui.R(cx - 260, 300, 520, 90), "Paused", 70, Color.white);

        if (ui.Button(ui.R(cx - 220, 420, 440, 90), "Resume  [Esc]", 34, UIStyles.ButtonColor.Orange))
            game.RequestTogglePause();
        if (ui.Button(ui.R(cx - 220, 530, 440, 90), "Restart  [R]", 34, UIStyles.ButtonColor.Blue))
            game.RequestRestart();
        if (ui.Button(ui.R(cx - 220, 640, 440, 90), "Main Menu  [M]", 34, UIStyles.ButtonColor.Blue))
            game.RequestMainMenu();
    }
}
