using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LogicScript : MonoBehaviour
{
    public static LogicScript Instance { get; private set; }

    [Header("Legacy / optional")]
    public int playerScore;
    public Text scoreText;

    [Header("End screens")]
    public GameObject gameOverScreen;
    public GameObject winScreen;

    [Header("HUD (optional — auto-created under first Canvas)")]
    public Text pipesHudText;
    public Text coinsHudText;
    public Text timerHudText;
    public Text shieldHudText;
    public Image powerBarFill;
    public Image powerBarBackground;

    [Header("Stats lines (optional)")]
    public Text gameOverStatsText;
    public Text winStatsText;

    public int pipesPassed;
    public int coinsCollected;
    [Range(0f, 1f)] public float power01 = 1f;
    /// <summary>Pipe body collisions while not shielded (for HUD / debugging).</summary>
    public int pipeHitsTaken;
    public float shieldTimeRemaining;
    public float matchTimer = 60f;

    public bool isGameOver;
    public bool hasWon;

    /// <summary>Every 10 coins grants shield (evaluator note).</summary>
    public const int CoinsPerShield = 10;
    /// <summary>Shield lasts 5 seconds; pipe hits ignored while active.</summary>
    public const float ShieldDurationSeconds = 5f;
    /// <summary>Game ends after this many pipe collisions (shield blocks a hit).</summary>
    public const int MaxPipeHitsBeforeGameOver = 3;

    static Font s_uiFont;
    static Sprite s_uiWhiteSprite;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    static Font UiFont()
    {
        if (s_uiFont != null) return s_uiFont;
        s_uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (s_uiFont == null) s_uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (s_uiFont == null) s_uiFont = Font.CreateDynamicFontFromOSFont("Arial", 16);
        return s_uiFont;
    }

    void Start()
    {
        power01 = 1f;
        pipeHitsTaken = 0;
        matchTimer = 60f;
        pipesPassed = 0;
        coinsCollected = 0;
        shieldTimeRemaining = 0f;
        isGameOver = false;
        hasWon = false;
        Time.timeScale = 1f;
        EnsureHud();
        EnsureGameOverStats();
        EnsureWinScreen();
        if (winScreen != null) winScreen.SetActive(false);
        if (gameOverScreen != null) gameOverScreen.SetActive(false);
        RefreshAllHud();
    }

    void Update()
    {
        if (isGameOver || hasWon) return;
        if (Time.timeScale <= 0f) return;

        matchTimer -= Time.deltaTime;
        if (matchTimer < 0f) matchTimer = 0f;
        if (shieldTimeRemaining > 0f) shieldTimeRemaining -= Time.deltaTime;

        if (matchTimer <= 0f)
        {
            TriggerWin();
            return;
        }

        RefreshTimerHud();
        RefreshPowerHud();
        RefreshShieldHud();
    }

    public bool CanScore() => !isGameOver && !hasWon;
    public bool CanCollectCoin() => !isGameOver && !hasWon;
    public bool HasShieldActive() => shieldTimeRemaining > 0f;

    public void RegisterPipePassed()
    {
        if (!CanScore()) return;
        pipesPassed++;
        playerScore = pipesPassed;
        RefreshScoreHud();
    }

    public void RegisterCoinCollected()
    {
        if (!CanCollectCoin()) return;
        coinsCollected++;
        int prevMilestone = (coinsCollected - 1) / CoinsPerShield;
        int newMilestone = coinsCollected / CoinsPerShield;
        if (newMilestone > prevMilestone && coinsCollected > 0)
            shieldTimeRemaining = ShieldDurationSeconds;
        RefreshScoreHud();
    }

    public void ApplyPipeCollisionHit()
    {
        if (!CanScore()) return;
        if (HasShieldActive()) return;
        pipeHitsTaken++;
        power01 = Mathf.Clamp01(1f - pipeHitsTaken / (float)MaxPipeHitsBeforeGameOver);
        RefreshPowerHud();
        if (pipeHitsTaken >= MaxPipeHitsBeforeGameOver)
            gameOver();
    }

    public void restartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("main menu");
    }

    [ContextMenu("Increase Score")]
    public void addScore(int scoreToAdd)
    {
        playerScore += scoreToAdd;
        if (scoreText != null) scoreText.text = playerScore.ToString();
    }

    public void gameOver()
    {
        if (hasWon) return;
        isGameOver = true;
        var bird = Object.FindFirstObjectByType<bird_script>();
        if (bird != null) bird.isAlive = false;
        if (gameOverStatsText != null)
            gameOverStatsText.text = $"Pipes passed: {pipesPassed}\nCoins: {coinsCollected}";
        if (gameOverScreen != null) gameOverScreen.SetActive(true);
        Time.timeScale = 0f;
    }

    void TriggerWin()
    {
        if (isGameOver || hasWon) return;
        hasWon = true;
        var bird = Object.FindFirstObjectByType<bird_script>();
        if (bird != null) bird.isAlive = false;
        if (winStatsText != null)
            winStatsText.text = $"You survived 60s!\nPipes: {pipesPassed}\nCoins: {coinsCollected}";
        if (winScreen != null) winScreen.SetActive(true);
        Time.timeScale = 0f;
    }

    void RefreshScoreHud()
    {
        if (pipesHudText != null) pipesHudText.text = $"Pipes: {pipesPassed}";
        if (coinsHudText != null) coinsHudText.text = $"Coins: {coinsCollected}";
        if (scoreText != null) scoreText.text = pipesPassed.ToString();
    }

    void RefreshTimerHud()
    {
        if (timerHudText != null) timerHudText.text = $"Time: {matchTimer:0}";
    }

    void RefreshPowerHud()
    {
        if (powerBarFill == null) return;
        EnsurePowerBarSprites();
        powerBarFill.type = Image.Type.Filled;
        powerBarFill.fillMethod = Image.FillMethod.Horizontal;
        powerBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        powerBarFill.fillAmount = Mathf.Clamp01(power01);
    }

    /// <summary>Unity UI Filled images need a sprite; without one, fillAmount often appears stuck at full.</summary>
    static Sprite WhiteUISprite()
    {
        if (s_uiWhiteSprite != null) return s_uiWhiteSprite;
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.hideFlags = HideFlags.HideAndDontSave;
        tex.SetPixel(0, 0, Color.white);
        tex.Apply(false, true);
        s_uiWhiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
        s_uiWhiteSprite.name = "LogicHudWhiteSprite";
        return s_uiWhiteSprite;
    }

    void EnsurePowerBarSprites()
    {
        var spr = WhiteUISprite();
        if (powerBarFill != null && powerBarFill.sprite == null)
        {
            powerBarFill.sprite = spr;
            powerBarFill.type = Image.Type.Filled;
        }
        if (powerBarBackground != null && powerBarBackground.sprite == null)
        {
            powerBarBackground.sprite = spr;
            powerBarBackground.type = Image.Type.Simple;
        }
    }

    void RefreshShieldHud()
    {
        if (shieldHudText == null) return;
        shieldHudText.text = HasShieldActive() ? $"Shield: {shieldTimeRemaining:0.0}s" : "";
    }

    void RefreshAllHud()
    {
        RefreshScoreHud();
        RefreshTimerHud();
        RefreshPowerHud();
        RefreshShieldHud();
    }

    void EnsureHud()
    {
        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        if (pipesHudText != null && coinsHudText != null && timerHudText != null && powerBarFill != null && shieldHudText != null)
            return;

        Transform root = canvas.transform.Find("GameplayHUD");
        if (root == null)
        {
            var go = new GameObject("GameplayHUD");
            go.transform.SetParent(canvas.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            root = go.transform;
        }

        if (pipesHudText == null)
            pipesHudText = CreateHudText(root, "PipesHud", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(120f, -28f), TextAnchor.MiddleLeft);
        if (coinsHudText == null)
            coinsHudText = CreateHudText(root, "CoinsHud", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-120f, -28f), TextAnchor.MiddleRight);
        if (timerHudText == null)
            timerHudText = CreateHudText(root, "TimerHud", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), TextAnchor.MiddleCenter);
        if (shieldHudText == null)
            shieldHudText = CreateHudText(root, "ShieldHud", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f), TextAnchor.MiddleCenter);
        if (powerBarFill == null || powerBarBackground == null)
            CreatePowerBar(root);
    }

    static Text CreateHudText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, TextAnchor alignment)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(anchorMax.x, anchorMax.y);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(420f, 44f);
        var text = go.GetComponent<Text>();
        if (text == null) text = go.AddComponent<Text>();
        text.font = UiFont();
        text.fontSize = 26;
        text.color = Color.white;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    void CreatePowerBar(Transform parent)
    {
        Transform barRoot = parent.Find("PowerBar");
        GameObject go = barRoot != null ? barRoot.gameObject : new GameObject("PowerBar");
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -100f);
        rt.sizeDelta = new Vector2(300f, 26f);

        if (powerBarBackground == null)
        {
            var bg = new GameObject("PowerBg");
            bg.transform.SetParent(go.transform, false);
            var bgRt = bg.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            powerBarBackground = bg.AddComponent<Image>();
            powerBarBackground.sprite = WhiteUISprite();
            powerBarBackground.type = Image.Type.Simple;
            powerBarBackground.color = new Color(0.12f, 0.12f, 0.12f, 0.92f);
        }

        if (powerBarFill == null)
        {
            var fill = new GameObject("PowerFill");
            fill.transform.SetParent(go.transform, false);
            var fRt = fill.AddComponent<RectTransform>();
            fRt.anchorMin = Vector2.zero;
            fRt.anchorMax = Vector2.one;
            fRt.offsetMin = new Vector2(3f, 3f);
            fRt.offsetMax = new Vector2(-3f, -3f);
            powerBarFill = fill.AddComponent<Image>();
            powerBarFill.sprite = WhiteUISprite();
            powerBarFill.color = new Color(1f, 0.4f, 0.25f, 1f);
            powerBarFill.type = Image.Type.Filled;
            powerBarFill.fillMethod = Image.FillMethod.Horizontal;
            powerBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            powerBarFill.fillAmount = 1f;
        }

        EnsurePowerBarSprites();
    }

    void EnsureGameOverStats()
    {
        if (gameOverScreen == null || gameOverStatsText != null) return;
        Transform t = gameOverScreen.transform.Find("GameOverStats");
        if (t != null)
        {
            gameOverStatsText = t.GetComponent<Text>();
            return;
        }
        var go = new GameObject("GameOverStats");
        go.transform.SetParent(gameOverScreen.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.55f);
        rt.anchorMax = new Vector2(0.5f, 0.55f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(700f, 120f);
        gameOverStatsText = go.AddComponent<Text>();
        gameOverStatsText.font = UiFont();
        gameOverStatsText.fontSize = 32;
        gameOverStatsText.color = Color.white;
        gameOverStatsText.alignment = TextAnchor.MiddleCenter;
        gameOverStatsText.horizontalOverflow = HorizontalWrapMode.Wrap;
        gameOverStatsText.verticalOverflow = VerticalWrapMode.Overflow;
    }

    void EnsureWinScreen()
    {
        if (winScreen != null) return;
        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        var go = new GameObject("WinScreen");
        go.transform.SetParent(canvas.transform, false);
        var rootRt = go.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        var dim = new GameObject("Dim");
        dim.transform.SetParent(go.transform, false);
        var dimRt = dim.AddComponent<RectTransform>();
        dimRt.anchorMin = Vector2.zero;
        dimRt.anchorMax = Vector2.one;
        dimRt.offsetMin = Vector2.zero;
        dimRt.offsetMax = Vector2.zero;
        var dimImg = dim.AddComponent<Image>();
        dimImg.color = new Color(0f, 0.15f, 0.1f, 0.75f);
        dimImg.raycastTarget = true;

        var titleGo = new GameObject("WinTitle");
        titleGo.transform.SetParent(go.transform, false);
        var titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.62f);
        titleRt.anchorMax = new Vector2(0.5f, 0.62f);
        titleRt.sizeDelta = new Vector2(800f, 100f);
        titleRt.anchoredPosition = Vector2.zero;
        var title = titleGo.AddComponent<Text>();
        title.font = UiFont();
        title.fontSize = 72;
        title.fontStyle = FontStyle.Bold;
        title.color = Color.white;
        title.alignment = TextAnchor.MiddleCenter;
        title.text = "YOU WIN";

        var statsGo = new GameObject("WinStats");
        statsGo.transform.SetParent(go.transform, false);
        var statsRt = statsGo.AddComponent<RectTransform>();
        statsRt.anchorMin = new Vector2(0.5f, 0.45f);
        statsRt.anchorMax = new Vector2(0.5f, 0.45f);
        statsRt.sizeDelta = new Vector2(700f, 140f);
        statsRt.anchoredPosition = Vector2.zero;
        winStatsText = statsGo.AddComponent<Text>();
        winStatsText.font = UiFont();
        winStatsText.fontSize = 30;
        winStatsText.color = Color.white;
        winStatsText.alignment = TextAnchor.MiddleCenter;

        CreateWinButton(go.transform, "RestartBtn", new Vector2(-120f, -180f), "Restart", restartGame);
        CreateWinButton(go.transform, "MenuBtn", new Vector2(120f, -180f), "Menu", LoadMainMenu);

        winScreen = go;
        winScreen.SetActive(false);
    }

    static void CreateWinButton(Transform parent, string name, Vector2 pos, string label, UnityEngine.Events.UnityAction onClick)
    {
        var btnGo = new GameObject(name);
        btnGo.transform.SetParent(parent, false);
        var rt = btnGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.35f);
        rt.anchorMax = new Vector2(0.5f, 0.35f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(180f, 50f);
        var img = btnGo.AddComponent<Image>();
        img.color = new Color(0.2f, 0.55f, 0.3f, 1f);
        var btn = btnGo.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(btnGo.transform, false);
        var trt = textGo.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        var tx = textGo.AddComponent<Text>();
        tx.font = UiFont();
        tx.fontSize = 22;
        tx.color = Color.white;
        tx.alignment = TextAnchor.MiddleCenter;
        tx.text = label;
    }
}
