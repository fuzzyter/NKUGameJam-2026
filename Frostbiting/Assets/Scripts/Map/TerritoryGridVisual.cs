using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class TerritoryGridVisual : MonoBehaviour
{
    [SerializeField] Color ownedColor = new Color(0.25f, 0.75f, 1f, 0.55f);
    [SerializeField] Color emptyColor = new Color(0, 0, 0, 0f);

    TerritoryGrid _grid;
    SpriteRenderer _sr;
    Texture2D _tex;
    Sprite _sprite;
    static bool _dirty;

    public static void MarkDirtyStatic() => _dirty = true;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _sr.color = Color.white;
        EnsureSpriteMaterial(_sr);
        _grid = TerritoryGrid.Instance;
        ApplySortingOrder();
    }

    void Start()
    {
        ApplySortingOrder();
    }

    void ApplySortingOrder()
    {
        if (_sr == null) return;
        var gm = GameManager.Instance;
        _sr.sortingOrder = gm != null ? gm.orderInLayerTerritory : 10;
    }

    static void EnsureSpriteMaterial(SpriteRenderer sr)
    {
        if (sr.sharedMaterial && sr.sharedMaterial.shader &&
            sr.sharedMaterial.shader.name != "Hidden/InternalErrorShader")
            return;

        Shader s = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                   ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default")
                   ?? Shader.Find("Sprites/Default");
        if (s != null)
            sr.sharedMaterial = new Material(s);
    }

    void LateUpdate()
    {
        if (_grid == null)
            _grid = TerritoryGrid.Instance;
        if (_grid == null || !_grid.IsReady)
            return;

        bool sizeMismatch = _tex == null || _tex.width != _grid.Width || _tex.height != _grid.Height;
        if (sizeMismatch)
            BuildTexture();

        if (sizeMismatch || _dirty)
        {
            _dirty = false;
            if (_tex != null)
            {
                FillTexture();
                if (sizeMismatch)
                    RebuildSprite();
                else
                {
                    _tex.Apply(false, false);
                    RebuildSprite();
                }
            }
        }
    }

    void BuildTexture()
    {
        if (_grid == null) return;
        if (_sprite != null)
        {
            Destroy(_sprite);
            _sprite = null;
        }

        if (_tex != null)
            Destroy(_tex);
        _tex = new Texture2D(_grid.Width, _grid.Height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
    }

    void FillTexture()
    {
        int w = _grid.Width;
        int h = _grid.Height;
        Color32 o = ownedColor;
        Color32 e = emptyColor;
        Color32[] px = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                bool owned = _grid.IsOwnedCell(new Vector2Int(x, y));
                px[i] = owned ? o : e;
            }
        }

        _tex.SetPixels32(px);
        _tex.Apply(false, false);
    }

    void RebuildSprite()
    {
        if (_tex == null || _grid == null) return;
        DetachFromScaledParents();
        if (_sprite != null)
        {
            Destroy(_sprite);
            _sprite = null;
        }

        Vector2 pivot = new Vector2(0f, 0f);
        _sprite = Sprite.Create(_tex, new Rect(0, 0, _tex.width, _tex.height), pivot, 1f / _grid.CellSize);
        _sr.sprite = _sprite;
        transform.SetPositionAndRotation(new Vector3(_grid.WorldMin.x, _grid.WorldMin.y, 0f), Quaternion.identity);
        transform.localScale = Vector3.one;
        EnsureSpriteMaterial(_sr);
        ApplySortingOrder();
    }

    void DetachFromScaledParents()
    {
        Transform p = transform.parent;
        if (p == null) return;
        Vector3 ls = p.lossyScale;
        if (Mathf.Abs(ls.x - 1f) < 0.001f && Mathf.Abs(ls.y - 1f) < 0.001f)
            return;
        transform.SetParent(null, true);
    }
}
