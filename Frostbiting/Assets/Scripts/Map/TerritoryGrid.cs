using System.Collections.Generic;
using UnityEngine;

public class TerritoryGrid : MonoBehaviour
{
    public static TerritoryGrid Instance { get; private set; }

    [SerializeField] float cellWorldSize = 0.25f;
    [SerializeField] int trailDilateRadius = 2;

    BoxCollider2D _mapBounds;
    int _w, _h;
    Vector2 _worldMin;
    bool[] _owned;

    public float CellSize => cellWorldSize;
    public Vector2 WorldMin => _worldMin;
    public int Width => _w;
    public int Height => _h;
    public bool IsReady => _owned != null && _w > 0 && _h > 0;

    void Awake()
    {
        Instance = this;
    }

    public void Init(BoxCollider2D mapBounds)
    {
        _mapBounds = mapBounds;
        Bounds b = mapBounds.bounds;
        _worldMin = b.min;
        cellWorldSize = Mathf.Max(0.05f, cellWorldSize);
        _w = Mathf.Max(8, Mathf.CeilToInt(b.size.x / cellWorldSize));
        _h = Mathf.Max(8, Mathf.CeilToInt(b.size.y / cellWorldSize));
        _owned = new bool[_w * _h];
        EnsureTerritoryVisual();
    }

    void EnsureTerritoryVisual()
    {
        if (FindFirstObjectByType<TerritoryGridVisual>(FindObjectsInactive.Include) != null)
            return;

        var go = new GameObject("TerritoryFill");
        // 부모에 scale 이 크게 걸려 있으면(예: MapBound) 스프라이트 월드 크기가 같이 줄어듦 → 루트에 둠
        go.transform.SetParent(null, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.color = Color.white;
        sr.sortingOrder = 1;
        go.AddComponent<TerritoryGridVisual>();
    }

    int Idx(int x, int y) => y * _w + x;

    public Vector2Int WorldToCell(Vector2 world)
    {
        if (!IsReady)
            return Vector2Int.zero;
        Vector2 local = world - _worldMin;
        int x = Mathf.FloorToInt(local.x / cellWorldSize);
        int y = Mathf.FloorToInt(local.y / cellWorldSize);
        return new Vector2Int(x, y);
    }

    public Vector2 CellToWorldCenter(Vector2Int c)
    {
        return _worldMin + new Vector2((c.x + 0.5f) * cellWorldSize, (c.y + 0.5f) * cellWorldSize);
    }

    public bool InBounds(Vector2Int c) =>
        IsReady && c.x >= 0 && c.y >= 0 && c.x < _w && c.y < _h;

    public bool IsOwnedWorld(Vector2 world)
    {
        if (!IsReady) return false;
        Vector2Int c = WorldToCell(world);
        if (!InBounds(c)) return false;
        return _owned[Idx(c.x, c.y)];
    }

    public bool IsOwnedCell(Vector2Int c)
    {
        if (!InBounds(c)) return false;
        return _owned[Idx(c.x, c.y)];
    }

    public void SeedRectAroundWorld(Vector2 worldCenter, int halfW, int halfH)
    {
        if (!IsReady) return;
        Vector2Int center = WorldToCell(worldCenter);
        for (int y = center.y - halfH; y <= center.y + halfH; y++)
        {
            for (int x = center.x - halfW; x <= center.x + halfW; x++)
            {
                Vector2Int c = new Vector2Int(x, y);
                if (InBounds(c))
                    _owned[Idx(c.x, c.y)] = true;
            }
        }

        TerritoryGridVisual.MarkDirtyStatic();
    }

    public bool TryApplyCapture(Vector2Int seed, IReadOnlyList<Vector2Int> trailCells, out List<Vector2Int> addedCells)
    {
        addedCells = new List<Vector2Int>();
        if (!IsReady || trailCells == null || trailCells.Count < 2 || !InBounds(seed))
            return false;

        bool[] oldOwned = (bool[])_owned.Clone();
        bool[] dilatedTrail = BuildDilatedSet(trailCells, trailDilateRadius);
        bool[] wall = new bool[_w * _h];
        for (int i = 0; i < wall.Length; i++)
            wall[i] = oldOwned[i] || dilatedTrail[i];

        bool[] outside = new bool[_w * _h];
        Queue<Vector2Int> q = new Queue<Vector2Int>();

        void TryEnqueueBorder(int x, int y)
        {
            Vector2Int c = new Vector2Int(x, y);
            if (!InBounds(c)) return;
            int id = Idx(c.x, c.y);
            if (wall[id] || outside[id]) return;
            outside[id] = true;
            q.Enqueue(c);
        }

        for (int x = 0; x < _w; x++)
        {
            TryEnqueueBorder(x, 0);
            TryEnqueueBorder(x, _h - 1);
        }

        for (int y = 0; y < _h; y++)
        {
            TryEnqueueBorder(0, y);
            TryEnqueueBorder(_w - 1, y);
        }

        while (q.Count > 0)
        {
            Vector2Int c = q.Dequeue();
            int id = Idx(c.x, c.y);
            void N(int dx, int dy)
            {
                Vector2Int n = new Vector2Int(c.x + dx, c.y + dy);
                if (!InBounds(n)) return;
                int nid = Idx(n.x, n.y);
                if (wall[nid] || outside[nid]) return;
                outside[nid] = true;
                q.Enqueue(n);
            }

            N(1, 0);
            N(-1, 0);
            N(0, 1);
            N(0, -1);
        }

        int sid = Idx(seed.x, seed.y);
        if (oldOwned[sid] || outside[sid])
            return false;

        bool[] captured = new bool[_w * _h];
        q.Clear();
        captured[sid] = true;
        q.Enqueue(seed);

        while (q.Count > 0)
        {
            Vector2Int c = q.Dequeue();
            addedCells.Add(c);
            void N(int dx, int dy)
            {
                Vector2Int n = new Vector2Int(c.x + dx, c.y + dy);
                if (!InBounds(n)) return;
                int nid = Idx(n.x, n.y);
                if (oldOwned[nid] || outside[nid] || captured[nid]) return;
                captured[nid] = true;
                q.Enqueue(n);
            }

            N(1, 0);
            N(-1, 0);
            N(0, 1);
            N(0, -1);
        }

        if (addedCells.Count == 0)
            return false;

        for (int i = 0; i < _owned.Length; i++)
        {
            if (captured[i])
                _owned[i] = true;
        }

        TerritoryGridVisual.MarkDirtyStatic();
        return true;
    }

    public bool TryApplyCaptureWithFallback(Vector2Int preferredSeed, IReadOnlyList<Vector2Int> trailCells,
        out List<Vector2Int> addedCells)
    {
        if (TryApplyCapture(preferredSeed, trailCells, out addedCells))
            return true;

        var tried = new HashSet<Vector2Int> { preferredSeed };
        for (int i = trailCells.Count - 1; i >= 0; i--)
        {
            Vector2Int s = trailCells[i];
            if (!tried.Add(s))
                continue;
            if (TryApplyCapture(s, trailCells, out addedCells))
                return true;
        }

        addedCells = new List<Vector2Int>();
        return false;
    }

    bool[] BuildDilatedSet(IReadOnlyList<Vector2Int> trail, int radius)
    {
        bool[] set = new bool[_w * _h];
        foreach (Vector2Int t in trail)
        {
            if (!InBounds(t)) continue;
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) > radius)
                        continue;
                    Vector2Int c = new Vector2Int(t.x + dx, t.y + dy);
                    if (InBounds(c))
                        set[Idx(c.x, c.y)] = true;
                }
            }
        }

        return set;
    }
}
