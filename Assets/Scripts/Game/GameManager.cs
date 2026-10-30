using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Which screen the game is on.</summary>
public enum GameState
{
    Title,
    ChoosingCharacters,
    Playing,
    GameOver
}

/// <summary>Which step of a turn we are in. Input is only accepted in the right step.</summary>
public enum TurnPhase
{
    None,
    Announcing,   // "Player 1's Turn" banner
    ChoosingDie,  // waiting for the player to pick a die and roll
    Rolling,      // die animation
    Moving,       // pawn hopping along the board
    SnakeChoice,  // waiting for "pay a coin or slide down?"
    ShowingCard,  // a Life Event card is on screen
    Busy          // any other animation
}

/// <summary>
/// The heart of the game. Creates the board, runs the turn loop as a coroutine,
/// and applies all the rules (dice, ladders, snakes, coins, Life Events, winning).
/// The UI (GameUI) only reads from this class and calls its public methods.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ---------------------------------------------------------------- tuning

    private const float HudWidthPixels = 620f; // must match GameUI.HudWidth (at 1080p)
    private const float ReferenceHeight = 1080f;

    // ---------------------------------------------------------------- Inspector settings

    [SerializeField]
    [Tooltip("Change these while the game is not playing, then save the scene.")]
    private GameSettings settings = new GameSettings();

    // ---------------------------------------------------------------- state the UI reads

    public GameState State { get; private set; }
    public TurnPhase Phase { get; private set; }
    public bool IsPaused { get; private set; }
    public bool VsComputer { get; private set; }

    public List<Player> Players { get; private set; }
    public Player CurrentPlayer { get { return Players[currentIndex]; } }
    public Player Winner { get; private set; }
    public int TurnNumber { get; private set; }

    public DieType SelectedDie { get; private set; }
    public DieType ShownDieType { get; private set; }
    public int ShownDieFace { get; private set; }
    public bool DieRolling { get; private set; }
    public float DieLandTime { get; private set; }
    public bool HasRolled { get; private set; }
    public int LastRoll { get; private set; }

    public LifeEventCard CurrentCard { get; private set; }
    public float CardShownTime { get; private set; }
    public int SnakeTilesAtRisk { get; private set; }

    public string Message { get; private set; }
    public string BannerText { get; private set; }
    public Color BannerColor { get; private set; }
    public float BannerTime { get; private set; }

    public SoundManager Sound { get; private set; }
    public GameSettings Settings { get { return settings; } }

    // Character select screen
    public List<string> CharacterChoices { get { return settings.characterChoices; } }
    public int PickingPlayer { get; private set; }      // 0 = Player 1 is choosing, 1 = Player 2
    public int HighlightedChoice { get; private set; }  // the one the keyboard is on

    // ---------------------------------------------------------------- private

    private enum Command { None, StartVsComputer, StartTwoPlayers, Restart, MainMenu, TogglePause }

    private BoardLayout layout;
    private BoardView board;
    private LifeEventDeck deck;
    private Camera cam;
    private CameraRig cameraRig;
    private SpriteRenderer background;

    private int currentIndex;
    private readonly int[] picks = { 0, 1 };  // which character each player chose
    private bool extraTurn;
    private Command pendingCommand;

    // Answers from the player, set by the UI and read by the turn coroutine.
    private bool rollRequested;
    private int snakeAnswer;   // -1 = no answer yet, 0 = slide down, 1 = pay to escape
    private bool cardClosed;

    // ================================================================ setup

    private void Awake()
    {
        Players = new List<Player>();
        if (settings == null) settings = new GameSettings();
        layout = new BoardLayout(settings);
        deck = new LifeEventDeck();

        // Use the board you built in the scene, or build one now if there is none.
        board = FindBoardInScene();
        if (board != null)
        {
            board.UseExisting(layout, settings);
        }
        else
        {
            board = new GameObject("Board").AddComponent<BoardView>();
            board.Build(layout, settings);
        }
        background = board.Background;
        SetupCamera();

        Sound = gameObject.AddComponent<SoundManager>();
        gameObject.AddComponent<GameUI>().Init(this);

        State = GameState.Title;
        Phase = TurnPhase.None;
        ShownDieFace = 5;
    }

    private void SetupCamera()
    {
        cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.1f, 0.18f, 0.22f);

        cameraRig = cam.GetComponent<CameraRig>();
        if (cameraRig == null) cameraRig = cam.gameObject.AddComponent<CameraRig>();
        cameraRig.BasePosition = cam.transform.position; // used when auto framing is off
        FrameCamera();
    }

    private static BoardView FindBoardInScene()
    {
        return Object.FindAnyObjectByType<BoardView>();
    }

    // ================================================================ editor button

    /// <summary>
    /// Builds the board as real objects in the scene (called by the
    /// "Build Board in Scene" button). They can then be seen, moved and saved
    /// without pressing Play.
    /// </summary>
    public BoardView BuildBoardInScene()
    {
        GameAssets.ClearCache();
        if (settings == null) settings = new GameSettings();
        layout = new BoardLayout(settings);

        board = new GameObject("Board").AddComponent<BoardView>();
        board.Build(layout, settings);
        background = board.Background;

        cam = Camera.main;
        cameraRig = null; // not playing: move the camera itself
        if (cam != null)
        {
            cam.orthographic = true;
            FrameCamera();
        }
        return board;
    }

    /// <summary>
    /// Sizes and moves the camera so the board fits in the space to the right
    /// of the HUD panel, for any screen shape. Called every frame in case the
    /// window is resized.
    /// </summary>
    private void FrameCamera()
    {
        if (!settings.autoFrameCamera || cam == null) return;

        float aspect = Mathf.Max(cam.aspect, 1f);
        float hudFraction = HudWidthPixels / ReferenceHeight; // HUD width compared to screen height
        Vector2 boardSize = layout.BoardSize + new Vector2(0.9f, 0.9f);

        // Tall enough for the board plus room for the turn banner,
        // and wide enough that the board does not go under the HUD.
        float sizeForHeight = (boardSize.y + 2.2f) / 2f;
        float sizeForWidth = boardSize.x / (2f * Mathf.Max(0.3f, aspect - hudFraction));
        cam.orthographicSize = Mathf.Max(sizeForHeight, sizeForWidth);

        float cameraX = -cam.orthographicSize * hudFraction;
        Vector3 cameraPosition = new Vector3(cameraX, 0.45f, -10f);
        if (cameraRig != null) cameraRig.BasePosition = cameraPosition; // while playing (allows shake)
        else cam.transform.position = cameraPosition;                      // in the editor

        if (background != null)
        {
            // Cover the whole view, keeping the image's shape.
            float viewHeight = cam.orthographicSize * 2f;
            float viewWidth = viewHeight * aspect;
            Vector2 imageSize = background.sprite.bounds.size; // world size at scale 1
            float scale = Mathf.Max(viewWidth / imageSize.x, viewHeight / imageSize.y) * 1.05f;
            background.transform.position = new Vector3(cameraX, 0.45f, 5f);
            background.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    // ================================================================ main loop

    private void Update()
    {
        FrameCamera();

        Command command = pendingCommand;
        pendingCommand = Command.None;
        switch (command)
        {
            case Command.StartVsComputer: BeginCharacterSelect(true); break;
            case Command.StartTwoPlayers: BeginCharacterSelect(false); break;
            case Command.Restart: StartGame(VsComputer); break;
            case Command.MainMenu: GoToTitle(); break;
            case Command.TogglePause: SetPaused(!IsPaused); break;
        }
    }

    private void StartGame(bool vsComputer)
    {
        StopAllCoroutines();
        ClearPlayers();
        SetPaused(false);

        VsComputer = vsComputer;
        Players.Add(new Player("Player 1", settings.player1Color, CharacterImage(0), false));
        Players.Add(new Player(vsComputer ? "Computer" : "Player 2", settings.player2Color, CharacterImage(1), vsComputer));

        for (int i = 0; i < Players.Count; i++)
        {
            Player p = Players[i];
            p.Tile = 0;
            p.Coins = settings.startingCoins;
            p.Token = Token.Create(p, TokenPosition(p), settings.characterSize,
                                   settings.showCharacterShadow, settings.showTurnRing);
        }

        deck.Shuffle();
        currentIndex = 0;
        TurnNumber = 0;
        Winner = null;
        CurrentCard = null;
        HasRolled = false;
        ShownDieFace = 5;
        State = GameState.Playing;
        Sound.Play("click");
        StartCoroutine(GameLoop());
    }

    // ================================================================ character select

    /// <summary>Opens the character select screen after the player picks a game mode.</summary>
    private void BeginCharacterSelect(bool vsComputer)
    {
        VsComputer = vsComputer;
        if (CharacterChoices.Count < 2)
        {
            Debug.LogWarning("Add at least 2 character images to Settings > Character Choices.");
            StartGame(vsComputer);
            return;
        }
        PickingPlayer = 0;
        picks[0] = -1;
        picks[1] = -1;
        HighlightedChoice = 0;
        State = GameState.ChoosingCharacters;
        Sound.Play("click");
    }

    /// <summary>True if the other player already took this character.</summary>
    public bool IsCharacterTaken(int choice)
    {
        return picks[0] == choice || picks[1] == choice;
    }

    /// <summary>Which player (0 or 1) picked this character, or -1.</summary>
    public int WhoPicked(int choice)
    {
        if (picks[0] == choice) return 0;
        if (picks[1] == choice) return 1;
        return -1;
    }

    /// <summary>Moves the keyboard highlight left (-1) or right (+1), skipping taken characters.</summary>
    public void MoveHighlight(int direction)
    {
        if (State != GameState.ChoosingCharacters) return;
        int count = CharacterChoices.Count;
        for (int step = 1; step <= count; step++)
        {
            int next = ((HighlightedChoice + direction * step) % count + count) % count;
            if (!IsCharacterTaken(next))
            {
                HighlightedChoice = next;
                Sound.Play("step", 0.6f);
                return;
            }
        }
    }

    /// <summary>The current player picks a character. Then Player 2 (or the computer) picks.</summary>
    public void ChooseCharacter(int choice)
    {
        if (State != GameState.ChoosingCharacters) return;
        if (choice < 0 || choice >= CharacterChoices.Count || IsCharacterTaken(choice)) return;

        picks[PickingPlayer] = choice;
        Sound.Play("coin", 0.7f);

        if (PickingPlayer == 0 && !VsComputer)
        {
            PickingPlayer = 1;
            MoveHighlight(1); // move off the character that was just taken
            return;
        }
        if (PickingPlayer == 0 && VsComputer)
        {
            picks[1] = RandomFreeCharacter();
        }
        StartGame(VsComputer);
    }

    private int RandomFreeCharacter()
    {
        var free = new List<int>();
        for (int i = 0; i < CharacterChoices.Count; i++)
        {
            if (!IsCharacterTaken(i)) free.Add(i);
        }
        return free[Random.Range(0, free.Count)];
    }

    /// <summary>Image for a player: their pick, or a default if nothing was picked.</summary>
    private string CharacterImage(int playerIndex)
    {
        int choice = picks[playerIndex];
        if (choice >= 0 && choice < CharacterChoices.Count) return CharacterChoices[choice];
        if (CharacterChoices.Count > playerIndex) return CharacterChoices[playerIndex];
        return playerIndex == 0 ? "pawn_blue" : "pawn_orange";
    }

    private void GoToTitle()
    {
        StopAllCoroutines();
        ClearPlayers();
        SetPaused(false);
        State = GameState.Title;
        Phase = TurnPhase.None;
        CurrentCard = null;
    }

    private void ClearPlayers()
    {
        foreach (Player p in Players)
        {
            if (p.Token != null) Destroy(p.Token.gameObject);
        }
        Players.Clear();
    }

    private void SetPaused(bool paused)
    {
        if (paused && State != GameState.Playing) return;
        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
    }

    /// <summary>Players take turns until someone wins.</summary>
    private IEnumerator GameLoop()
    {
        while (State == GameState.Playing)
        {
            extraTurn = false;
            yield return TakeTurn(CurrentPlayer);
            if (State != GameState.Playing) yield break;

            if (!extraTurn)
            {
                CurrentPlayer.Token.SetActive(false);
                currentIndex = (currentIndex + 1) % Players.Count;
            }
        }
    }

    // ================================================================ one turn

    private IEnumerator TakeTurn(Player p)
    {
        TurnNumber++;
        foreach (Player other in Players) other.Token.SetActive(other == p);

        // 1. Announce whose turn it is.
        Phase = TurnPhase.Announcing;
        ShowBanner(extraTurn ? p.Name + " rolls again!" : p.Name + "'s Turn", p.Color);
        Sound.Play("turn", 0.7f);
        HasRolled = false;
        yield return new WaitForSeconds(0.7f);

        // 2. Skip the turn if a card said so.
        if (p.SkipTurns > 0)
        {
            p.SkipTurns--;
            Message = p.Name + " skips this turn (Car Trouble).";
            Sound.Play("skip");
            Effects.FloatingText(p.Token.transform.position, "Skip!", new Color(1f, 0.45f, 0.4f));
            yield return new WaitForSeconds(1.4f);
            yield break;
        }

        // 3. Choose a die and roll.
        yield return ChooseDie(p);
        DieType die = SelectedDie;
        if (die == DieType.Lucky) PayForLuckyDie(p);

        Phase = TurnPhase.Rolling;
        int faceIndex = Dice.RollFaceIndex();
        yield return RollAnimation(die, faceIndex);
        int roll = Dice.Faces(die)[faceIndex];
        LastRoll = roll;
        HasRolled = true;
        Message = RollMessage(p, roll);

        // 4. Move.
        Phase = TurnPhase.Moving;
        yield return MoveSteps(p, roll);
        if (CheckForWinner(p)) yield break;

        // 5. Whatever is on the tile happens (only the first tile, no chains).
        Phase = TurnPhase.Busy;
        yield return ResolveTile(p);
        if (CheckForWinner(p)) yield break;

        yield return new WaitForSeconds(0.4f);
    }

    private IEnumerator ChooseDie(Player p)
    {
        Phase = TurnPhase.ChoosingDie;
        SelectedDie = DieType.Plain;
        ShownDieType = DieType.Plain;
        rollRequested = false;

        if (p.IsComputer)
        {
            Message = "Computer is thinking...";
            yield return new WaitForSeconds(0.9f);
            ApplyDieChoice(ComputerPlayer.ChooseDie(p, Players, layout.LastTile));
            yield return new WaitForSeconds(0.7f);
        }
        else
        {
            Message = p.Name + ": choose a die and roll!";
            while (!rollRequested) yield return null;
        }
        rollRequested = false;
    }

    private void PayForLuckyDie(Player p)
    {
        if (p.FreeLuckyRolls > 0)
        {
            p.FreeLuckyRolls--;
            Effects.FloatingText(p.Token.transform.position, "Free roll!", new Color(1f, 0.85f, 0.3f));
        }
        else
        {
            p.Coins -= Dice.LuckyDieCost;
            Sound.Play("coin_spend", 0.8f);
            Effects.FloatingText(p.Token.transform.position, "-1 coin", new Color(1f, 0.85f, 0.3f));
        }
    }

    private static string RollMessage(Player p, int roll)
    {
        if (roll < 0) return p.Name + " rolled " + roll + "! Moving back...";
        if (roll == 0) return p.Name + " rolled 0. No move this time!";
        return p.Name + " rolled " + roll + "!";
    }

    /// <summary>Shows random faces for a moment, then lands on the real result.</summary>
    private IEnumerator RollAnimation(DieType die, int finalFace)
    {
        DieRolling = true;
        ShownDieType = die;
        Sound.Play("dice_rattle");

        float time = 0f;
        float nextSwap = 0f;
        while (time < 0.75f)
        {
            time += Time.deltaTime;
            nextSwap -= Time.deltaTime;
            if (nextSwap <= 0f)
            {
                ShownDieFace = Random.Range(0, 6);
                nextSwap = 0.05f + time * 0.08f; // faces change slower as the die settles
            }
            yield return null;
        }

        ShownDieFace = finalFace;
        DieRolling = false;
        DieLandTime = Time.time;
        Sound.Play("dice_land");
        yield return new WaitForSeconds(0.35f);
    }

    /// <summary>
    /// Hops one tile at a time. Negative steps move backward. The pawn stops on
    /// Start or Finish instead of going past them.
    /// </summary>
    private IEnumerator MoveSteps(Player p, int steps)
    {
        if (steps == 0)
        {
            Effects.FloatingText(p.Token.transform.position, "0", new Color(0.85f, 0.85f, 0.9f));
            yield return new WaitForSeconds(0.6f);
            yield break;
        }

        int direction = steps > 0 ? 1 : -1;
        for (int i = 0; i < Mathf.Abs(steps); i++)
        {
            int next = Mathf.Clamp(p.Tile + direction, 0, layout.LastTile);
            if (next == p.Tile) break;

            p.Tile = next;
            yield return p.Token.HopTo(TokenPosition(p), settings.stepTime, 0.35f);
            // Pitch goes up when moving forward and down when moving back.
            Sound.Play("step", 0.8f, 1f + 0.05f * i * direction);
        }
    }

    // ================================================================ tiles

    private IEnumerator ResolveTile(Player p)
    {
        switch (layout.GetTileType(p.Tile))
        {
            case TileType.Ladder:
                yield return ClimbLadder(p);
                break;
            case TileType.Snake:
                yield return HandleSnake(p);
                break;
            case TileType.Coin:
                GiveCoins(p, Random.Range(1, 3), "found");
                yield return new WaitForSeconds(0.9f);
                break;
            case TileType.LifeEvent:
                yield return HandleLifeEvent(p);
                break;
        }
    }

    private IEnumerator ClimbLadder(Player p)
    {
        int top = layout.Ladders[p.Tile];
        Message = p.Name + " climbs a ladder to tile " + (top + 1) + "!";
        Effects.FloatingText(p.Token.transform.position, "Ladder!", new Color(0.5f, 1f, 0.55f));
        Sound.Play("ladder");
        p.LaddersClimbed++;

        p.Tile = top;
        yield return p.Token.ClimbTo(TokenPosition(p), 0.9f);
        Effects.Sparkles(p.Token.transform.position);
        yield return new WaitForSeconds(0.3f);
    }

    /// <summary>
    /// The new twist: a player with a coin can pay it to escape the snake.
    /// Otherwise the pawn slides down along the snake's body.
    /// </summary>
    private IEnumerator HandleSnake(Player p)
    {
        int headTile = p.Tile;
        int tailTile = layout.Snakes[headTile];
        SnakeTilesAtRisk = headTile - tailTile;
        Effects.FloatingText(p.Token.transform.position, "Snake!", new Color(1f, 0.45f, 0.4f));
        Effects.Shake(0.06f, 0.25f);

        bool escape = false;
        if (p.Coins >= 1)
        {
            Phase = TurnPhase.SnakeChoice;
            Message = "A snake! Pay 1 coin to escape, or slide back " + SnakeTilesAtRisk + " tiles?";
            snakeAnswer = -1;

            if (p.IsComputer)
            {
                yield return new WaitForSeconds(1.1f);
                escape = ComputerPlayer.ShouldEscapeSnake(p, SnakeTilesAtRisk);
            }
            else
            {
                while (snakeAnswer < 0) yield return null;
                escape = snakeAnswer == 1;
            }
            Phase = TurnPhase.Busy;
        }
        else
        {
            Message = p.Name + " has no coins to escape the snake!";
            yield return new WaitForSeconds(0.8f);
        }

        if (escape)
        {
            p.Coins -= 1;
            p.SnakesEscaped++;
            Message = p.Name + " paid 1 coin and escaped the snake!";
            Sound.Play("escape");
            Effects.Sparkles(p.Token.transform.position);
            Effects.FloatingText(p.Token.transform.position, "Escaped!", new Color(0.55f, 0.85f, 1f));
            yield return new WaitForSeconds(1f);
            yield break;
        }

        // Slide down along the snake's body.
        p.SnakesHit++;
        Message = p.Name + " slides down to tile " + (tailTile + 1) + ".";
        Sound.Play("snake");
        yield return new WaitForSeconds(0.35f); // let the hiss play first
        Effects.Shake(0.12f, 0.45f);

        Vector3 offset = TokenOffset(p);
        List<Vector3> path = board.SnakePath(headTile);
        for (int i = 0; i < path.Count; i++) path[i] += offset;

        p.Tile = tailTile;
        path.Add(TokenPosition(p));
        if (path.Count >= 2) yield return p.Token.FollowPath(path, 1.0f);
        else yield return p.Token.HopTo(TokenPosition(p), 0.6f, 0.3f); // no snake object found
        Effects.FloatingText(p.Token.transform.position, "Ouch!", new Color(1f, 0.45f, 0.4f));
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator HandleLifeEvent(Player p)
    {
        LifeEventCard card = deck.Draw();
        CurrentCard = card;
        CardShownTime = Time.time;
        cardClosed = false;
        Phase = TurnPhase.ShowingCard;
        Message = "Life Event: " + card.Title;

        Sound.Play("card_flip");
        yield return new WaitForSeconds(0.25f);
        Sound.Play(card.IsGood ? "good" : "bad", 0.8f);

        // Humans close the card themselves. The computer's card closes on its own.
        float waited = 0f;
        while (!cardClosed && !(p.IsComputer && waited > 2.4f))
        {
            waited += Time.deltaTime;
            yield return null;
        }

        CurrentCard = null;
        Phase = TurnPhase.Busy;
        yield return ApplyEvent(p, card);
    }

    private IEnumerator ApplyEvent(Player p, LifeEventCard card)
    {
        Vector3 pos = p.Token.transform.position;
        switch (card.Effect)
        {
            case EventEffect.GainCoins:
                GiveCoins(p, card.Amount, "earned");
                yield return new WaitForSeconds(0.9f);
                break;

            case EventEffect.LoseCoins:
                int paid = Mathf.Min(card.Amount, p.Coins);
                p.Coins -= paid;
                Sound.Play("coin_spend");
                Effects.FloatingText(pos, paid > 0 ? "-" + paid + " coins" : "No coins!", new Color(1f, 0.45f, 0.4f));
                Message = paid > 0 ? p.Name + " paid " + paid + " coins." : p.Name + " had no coins to pay. Phew!";
                yield return new WaitForSeconds(0.9f);
                break;

            case EventEffect.MoveForward:
                yield return MoveSteps(p, card.Amount);
                break;

            case EventEffect.MoveBack:
                yield return MoveSteps(p, -card.Amount);
                break;

            case EventEffect.SkipTurn:
                p.SkipTurns++;
                Effects.FloatingText(pos, "Skip next turn", new Color(1f, 0.45f, 0.4f));
                yield return new WaitForSeconds(0.7f);
                break;

            case EventEffect.ExtraTurn:
                extraTurn = true;
                Effects.FloatingText(pos, "Roll again!", new Color(0.5f, 1f, 0.55f));
                yield return new WaitForSeconds(0.7f);
                break;

            case EventEffect.TakeCoinFromOthers:
                int collected = 0;
                foreach (Player other in Players)
                {
                    if (other == p || other.Coins <= 0) continue;
                    other.Coins--;
                    collected++;
                    Effects.FloatingText(other.Token.transform.position, "-1 coin", new Color(1f, 0.45f, 0.4f));
                }
                if (collected > 0) GiveCoins(p, collected, "got");
                else Effects.FloatingText(pos, "No one has coins!", Color.white);
                yield return new WaitForSeconds(0.9f);
                break;

            case EventEffect.EveryoneBack:
                foreach (Player player in Players)
                {
                    yield return MoveSteps(player, -card.Amount);
                }
                break;

            case EventEffect.FreeLuckyRoll:
                p.FreeLuckyRolls++;
                Sound.Play("lucky");
                Effects.FloatingText(pos, "Free Lucky roll!", new Color(1f, 0.85f, 0.3f));
                yield return new WaitForSeconds(0.7f);
                break;
        }
    }

    private void GiveCoins(Player p, int amount, string verb)
    {
        p.Coins += amount;
        Sound.Play("coin");
        Effects.CoinShower(p.Token.transform.position, amount);
        Effects.FloatingText(p.Token.transform.position, "+" + amount + (amount == 1 ? " coin" : " coins"), new Color(1f, 0.85f, 0.3f));
        Message = p.Name + " " + verb + " " + amount + (amount == 1 ? " coin!" : " coins!");
    }

    // ================================================================ winning

    private bool CheckForWinner(Player p)
    {
        if (p.Tile != layout.LastTile) return false;

        State = GameState.GameOver;
        Phase = TurnPhase.None;
        Winner = p;
        Message = p.Name + " wins!";
        p.Token.SetCelebrating(true);
        Sound.Play("win");
        Effects.Confetti(cam);
        Effects.Sparkles(p.Token.transform.position);
        return true;
    }

    // ================================================================ helpers

    /// <summary>Where a player's pawn stands. Each player has their own spot on a tile.</summary>
    private Vector3 TokenPosition(Player p)
    {
        return board.TileWorldPosition(p.Tile) + TokenOffset(p);
    }

    private Vector3 TokenOffset(Player p)
    {
        int index = Players.IndexOf(p);
        float x = index == 0 ? -0.24f : 0.24f;
        return new Vector3(x, -0.36f, 0f);
    }

    private void ShowBanner(string text, Color color)
    {
        BannerText = text;
        BannerColor = color;
        BannerTime = Time.time;
    }

    private void ApplyDieChoice(DieType die)
    {
        SelectedDie = die;
        ShownDieType = die;
        if (die == DieType.Lucky) Sound.Play("lucky", 0.6f);
        else Sound.Play("click");
    }

    private bool HumanCanAct(TurnPhase phase)
    {
        return State == GameState.Playing && !IsPaused && Phase == phase && !CurrentPlayer.IsComputer;
    }

    // ================================================================ called by the UI

    public void RequestStart(bool vsComputer)
    {
        pendingCommand = vsComputer ? Command.StartVsComputer : Command.StartTwoPlayers;
    }

    public void RequestRestart() { pendingCommand = Command.Restart; }
    public void RequestMainMenu() { pendingCommand = Command.MainMenu; }

    public void RequestTogglePause()
    {
        if (State == GameState.Playing) pendingCommand = Command.TogglePause;
    }

    public void SelectDie(DieType die)
    {
        if (!HumanCanAct(TurnPhase.ChoosingDie) || SelectedDie == die) return;
        if (die == DieType.Lucky && !CurrentPlayer.CanUseLuckyDie)
        {
            Message = "You need 1 coin to use the Lucky Die.";
            Sound.Play("skip", 0.5f);
            return;
        }
        ApplyDieChoice(die);
    }

    public void RequestRoll()
    {
        if (HumanCanAct(TurnPhase.ChoosingDie)) rollRequested = true;
    }

    public void AnswerSnake(bool payToEscape)
    {
        if (!HumanCanAct(TurnPhase.SnakeChoice)) return;
        snakeAnswer = payToEscape ? 1 : 0;
        Sound.Play("click");
    }

    public void CloseCard()
    {
        // A short delay stops a held key from closing the card instantly.
        if (State != GameState.Playing || IsPaused || Phase != TurnPhase.ShowingCard) return;
        if (Time.time - CardShownTime < 0.5f || cardClosed) return;
        cardClosed = true;
        Sound.Play("click");
    }
}
