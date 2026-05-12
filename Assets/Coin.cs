using UnityEngine;

/// <summary>
/// Collectible coin between pipes; awards score and may contribute to shield milestones via LogicScript.
/// </summary>
public class Coin : MonoBehaviour
{
    bool _taken;
    static Sprite s_coinSprite;

    void Awake()
    {
        BuildSharedCoinSprite();
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = s_coinSprite;
        sr.color = Color.white;
        sr.sortingOrder = 3;
    }

    static void BuildSharedCoinSprite()
    {
        if (s_coinSprite != null) return;
        const int s = 32;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        var c = Color.clear;
        var gold = new Color(1f, 0.85f, 0.2f);
        var ring = new Color(0.9f, 0.7f, 0.05f);
        float r = s * 0.42f;
        float cx = s * 0.5f;
        float cy = s * 0.5f;
        for (int y = 0; y < s; y++)
        for (int x = 0; x < s; x++)
        {
            float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            if (d < r - 2f) tex.SetPixel(x, y, gold);
            else if (d < r) tex.SetPixel(x, y, ring);
            else tex.SetPixel(x, y, c);
        }
        tex.Apply();
        s_coinSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_taken) return;
        if (other.GetComponent<bird_script>() == null) return;
        var logic = LogicScript.Instance;
        if (logic == null || !logic.CanCollectCoin()) return;
        _taken = true;
        logic.RegisterCoinCollected();
        Destroy(gameObject);
    }
}
