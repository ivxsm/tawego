using UnityEngine;

public class bird_script : MonoBehaviour
{
    public Rigidbody2D myRigidbody;
    [Tooltip("How much upward speed each press adds. Repeated presses climb faster, but maxUpwardSpeed caps the height feel.")]
    public float flapStrength = 3.1f;
    [Tooltip("Maximum upward speed after one or more jump presses. Keep this moderate so jumps are fast, not too high.")]
    public float maxUpwardSpeed = 7.4f;
    public bool isAlive = true;
    public LogicScript logic;

    [Tooltip("How much falling speed is cancelled on jump. Higher feels more responsive without raising max height.")]
    [Range(0f, 1f)] public float fallCancelFactor = 0.6f;
    [Tooltip("Overall gravity boost so the bird comes down fast instead of floating.")]
    public float baseGravityMultiplier = 1.18f;

    [Header("Easy mode (Difficulty = 1)")]
    [Tooltip("Stronger flap on Easy.")]
    public float easyFlapMultiplier = 1.0f;
    [Tooltip("Faster fall on Easy.")]
    public float easyGravityMultiplier = 1.05f;

    [Header("Hard mode (Difficulty = 2)")]
    [Tooltip("Slightly stronger per-press boost on Hard, still capped by maxUpwardSpeed.")]
    public float hardFlapMultiplier = 1.2f;
    [Tooltip("Extra fall speed on Hard so the character does not hang upward.")]
    public float hardGravityMultiplier = 1.02f;

    [Header("Bounds (instant game over)")]
    public float fallDeathY = -22f;
    public float ceilingDeathY = 24f;
    [Tooltip("Die if bird is this far past the left edge of the camera view.")]
    public float behindCameraMargin = 2.5f;

    [Header("Audio Settings")]
    public AudioSource collisionSound;

    SpriteRenderer _spriteRenderer;
    Sprite _defaultSprite;
    GameObject _shieldRing;

    void Start()
    {
        logic = GameObject.FindGameObjectWithTag("Logic").GetComponent<LogicScript>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer != null)
            _defaultSprite = _spriteRenderer.sprite;

        if (myRigidbody != null)
        {
            myRigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
            myRigidbody.angularVelocity = 0f;
            myRigidbody.gravityScale *= baseGravityMultiplier;
            int diff = PlayerPrefs.GetInt("Difficulty", 1);
            if (diff == 1)
            {
                flapStrength *= easyFlapMultiplier;
                myRigidbody.gravityScale *= easyGravityMultiplier;
            }
            else if (diff == 2)
            {
                flapStrength *= hardFlapMultiplier;
                myRigidbody.gravityScale *= hardGravityMultiplier;
            }
        }

        transform.rotation = Quaternion.identity;
        ApplySkinFromPrefs();
        EnsureShieldRing();
    }

    void ApplySkinFromPrefs()
    {
        if (_spriteRenderer == null) return;
        int i = PlayerPrefs.GetInt(MainMenuScript.SkinPrefKey, 0);
        if (i < 0) i = 0;

        Sprite skin = i switch
        {
            1 => LoadFirstSprite("Skins/green"),
            2 => LoadFirstSprite("Skins/blue"),
            _ => LoadFirstSprite("Skins/default") ?? _defaultSprite
        };

        if (skin != null)
            _spriteRenderer.sprite = skin;

        _spriteRenderer.color = Color.white;
    }

    static Sprite LoadFirstSprite(string resourcePath)
    {
        var direct = Resources.Load<Sprite>(resourcePath);
        if (direct != null) return direct;
        var all = Resources.LoadAll<Sprite>(resourcePath);
        return all.Length > 0 ? all[0] : null;
    }

    void EnsureShieldRing()
    {
        var existing = transform.Find("ShieldRing");
        if (existing != null)
        {
            _shieldRing = existing.gameObject;
            return;
        }
        _shieldRing = new GameObject("ShieldRing");
        _shieldRing.transform.SetParent(transform, false);
        _shieldRing.transform.localScale = Vector3.one * 2.6f;
        var sr = _shieldRing.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.35f, 0.92f, 1f, 0.75f);
        if (_spriteRenderer != null)
        {
            sr.sortingLayerID = _spriteRenderer.sortingLayerID;
            sr.sortingLayerName = _spriteRenderer.sortingLayerName;
            sr.sortingOrder = _spriteRenderer.sortingOrder + 25;
        }
        else
            sr.sortingOrder = 50;

        const int s = 64;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float cx = s * 0.5f, cy = s * 0.5f, rOuter = s * 0.46f, rInner = s * 0.38f;
        for (int y = 0; y < s; y++)
        for (int x = 0; x < s; x++)
        {
            float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            if (d <= rOuter && d >= rInner)
                tex.SetPixel(x, y, new Color(0.5f, 0.95f, 1f, 0.9f));
            else if (d < rInner && d > rInner - 2.2f)
                tex.SetPixel(x, y, new Color(0.2f, 0.5f, 1f, 0.35f));
            else
                tex.SetPixel(x, y, Color.clear);
        }
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
        _shieldRing.SetActive(false);
    }

    void LateUpdate()
    {
        if (myRigidbody != null)
            myRigidbody.angularVelocity = 0f;
        var e = transform.eulerAngles;
        if (Mathf.Abs(e.z) > 0.01f)
            transform.rotation = Quaternion.identity;
    }

    void Update()
    {
        if (logic != null && (logic.isGameOver || logic.hasWon))
            isAlive = false;

        if (isAlive && logic != null && logic.CanScore())
        {
            if (transform.position.y < fallDeathY || transform.position.y > ceilingDeathY)
                DieInstant();
            if (IsTooFarBehindCamera())
                DieInstant();
        }

        if (_shieldRing != null && logic != null)
            _shieldRing.SetActive(logic.HasShieldActive());

        if (Input.GetKeyDown(KeyCode.Space) && isAlive && myRigidbody != null && Time.timeScale > 0f)
            ApplyFlap();
    }

    void ApplyFlap()
    {
        float vx = myRigidbody.linearVelocity.x;
        float vy = myRigidbody.linearVelocity.y;

        if (vy < 0f)
            vy *= 1f - fallCancelFactor;

        float up = Mathf.Min(vy + flapStrength, maxUpwardSpeed);
        myRigidbody.linearVelocity = new Vector2(vx, up);
    }

    bool IsTooFarBehindCamera()
    {
        var cam = Camera.main;
        if (cam == null) return false;
        float halfW = cam.orthographicSize * cam.aspect;
        float leftWorld = cam.transform.position.x - halfW;
        return transform.position.x < leftWorld - behindCameraMargin;
    }

    void DieInstant()
    {
        if (!isAlive) return;
        if (collisionSound != null) collisionSound.Play();
        isAlive = false;
        if (logic != null) logic.gameOver();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isAlive || logic == null) return;

        if (collision.gameObject.CompareTag("PipeDamage"))
        {
            if (logic.HasShieldActive()) return;
            if (collisionSound != null) collisionSound.Play();
            logic.ApplyPipeCollisionHit();
            if (logic.isGameOver) isAlive = false;
            return;
        }

        if (collision.gameObject.CompareTag("KillZone"))
        {
            DieInstant();
            return;
        }

        DieInstant();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isAlive) return;
        if (collision.gameObject.CompareTag("ScoreZone")) return;
        if (collision.GetComponent<Coin>() != null) return;
    }
}
