using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns coins at random heights inside the actual pipe gap (from PipeDamage colliders).
/// </summary>
public class PipeCoinSpawner : MonoBehaviour
{
    [Tooltip("Extra vertical padding so coins never touch pipe bodies.")]
    public float verticalMargin = 1.15f;

    [Tooltip("How many coins to spawn for this pipe pair.")]
    [Min(1)] public int coinsPerPipe = 3;

    [Tooltip("Minimum vertical gap between two coins (local units).")]
    public float minSeparationBetweenCoins = 1.4f;

    void Start()
    {
        var prefab = Resources.Load<GameObject>("Coin");
        if (prefab == null) return;

        if (!TryGetGapLocalYRange(out float minY, out float maxY))
            return;

        float lo = Mathf.Min(minY, maxY) + verticalMargin;
        float hi = Mathf.Max(minY, maxY) - verticalMargin;
        if (hi <= lo)
            lo = hi = (minY + maxY) * 0.5f;

        int count = Mathf.Clamp(coinsPerPipe, 1, 8);
        var placedY = new List<float>(count);

        for (int i = 0; i < count; i++)
        {
            float y = PickNonOverlappingY(lo, hi, placedY);
            placedY.Add(y);
            var coin = Instantiate(prefab, transform);
            coin.transform.localPosition = new Vector3(0f, y, 0f);
            coin.transform.localRotation = Quaternion.identity;
            coin.transform.localScale = Vector3.one * 0.35f;
        }
    }

    float PickNonOverlappingY(float lo, float hi, List<float> existing)
    {
        for (int attempt = 0; attempt < 24; attempt++)
        {
            float y = Random.Range(lo, hi);
            bool ok = true;
            foreach (float e in existing)
            {
                if (Mathf.Abs(y - e) < minSeparationBetweenCoins)
                {
                    ok = false;
                    break;
                }
            }
            if (ok) return y;
        }
        return (lo + hi) * 0.5f;
    }

    bool TryGetGapLocalYRange(out float minY, out float maxY)
    {
        minY = 0f;
        maxY = 0f;
        var bodies = new List<Transform>();
        foreach (Transform c in transform)
        {
            if (c == null) continue;
            if (!c.gameObject.CompareTag("PipeDamage")) continue;
            if (c.GetComponent<BoxCollider2D>() == null) continue;
            bodies.Add(c);
        }

        if (bodies.Count < 2)
            return false;

        bodies.Sort((a, b) => a.localPosition.y.CompareTo(b.localPosition.y));
        Transform lower = bodies[0];
        Transform upper = bodies[bodies.Count - 1];

        var lowCol = lower.GetComponent<BoxCollider2D>();
        var upCol = upper.GetComponent<BoxCollider2D>();

        float lowerTopLocal = lower.localPosition.y + (lowCol.offset.y + lowCol.size.y * 0.5f) * lower.localScale.y;
        float upperBottomLocal = upper.localPosition.y + (upCol.offset.y - upCol.size.y * 0.5f) * upper.localScale.y;

        minY = lowerTopLocal;
        maxY = upperBottomLocal;
        return true;
    }
}
