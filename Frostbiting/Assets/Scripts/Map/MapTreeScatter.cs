using System.Collections.Generic;
using UnityEngine;

public class MapTreeScatter : MonoBehaviour
{
    [Header("Area")]
    [SerializeField] BoxCollider2D mapBounds;

    [Header("Trees")]
    [SerializeField] Sprite[] treeSprites;
    [Tooltip("How many trees to try to place (may be lower if minSpacing blocks many attempts).")]
    [SerializeField] int treeCount = 150;
    [SerializeField] float minSpacing = 0.4f;
    [SerializeField] int sortingOrder = 8;

    [Header("Optional")]
    [SerializeField] Transform container;
    [Tooltip("Slight scale variation for variety.")]
    [SerializeField] Vector2 scaleRange = new Vector2(0.85f, 1.15f);
    [SerializeField] bool randomFlipX = true;

    void Start()
    {
        Scatter();
    }

    public void Scatter()
    {
        if (!mapBounds || treeSprites == null || treeSprites.Length == 0)
        {
            Debug.LogWarning($"{nameof(MapTreeScatter)}: assign mapBounds and at least one sprite.", this);
            return;
        }

        Transform root = container ? container : transform;
        Bounds b = mapBounds.bounds;
        var used = new List<Vector2>(treeCount);
        int maxTries = Mathf.Max(treeCount * 50, 500);
        int placed = 0;

        for (int t = 0; t < maxTries && placed < treeCount; t++)
        {
            Vector2 p = new Vector2(Random.Range(b.min.x, b.max.x), Random.Range(b.min.y, b.max.y));
            if (!IsFarEnough(p, used, minSpacing))
                continue;

            GameObject go = new GameObject("Tree");
            go.transform.SetParent(root, false);
            go.transform.position = p;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = treeSprites[Random.Range(0, treeSprites.Length)];
            sr.sortingOrder = sortingOrder;

            float s = Random.Range(scaleRange.x, scaleRange.y);
            float sx = randomFlipX && Random.value > 0.5f ? -s : s;
            go.transform.localScale = new Vector3(sx, s, 1f);

            used.Add(p);
            placed++;
        }
    }

    static bool IsFarEnough(Vector2 p, List<Vector2> others, float minDist)
    {
        float sq = minDist * minDist;
        for (int i = 0; i < others.Count; i++)
        {
            if ((others[i] - p).sqrMagnitude < sq)
                return false;
        }

        return true;
    }
}
