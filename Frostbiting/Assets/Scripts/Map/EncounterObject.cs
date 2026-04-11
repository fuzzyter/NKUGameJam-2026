using UnityEngine;

public class EncounterObject : MonoBehaviour
{
    public EncounterData data;
    public bool Collected { get; private set; }

    SpriteRenderer _sr;
    Color _baseColor = Color.white;
    float _blinkPhase;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (_sr)
            _baseColor = _sr.color;
    }

    public void RefreshVisibility(TerritoryGrid grid)
    {
        if (Collected || !_sr || grid == null) return;
        bool inTerritory = grid.IsOwnedWorld(transform.position);
        _sr.enabled = inTerritory;
        if (inTerritory)
            _sr.color = _baseColor;
    }

    public void MarkCollected()
    {
        Collected = true;
        if (_sr)
            _sr.enabled = false;
    }

    public void SetDrawingWarning(float normalizedDanger)
    {
        if (!_sr || Collected) return;
        bool inTerritory = TerritoryGrid.Instance &&
                           TerritoryGrid.Instance.IsOwnedWorld(transform.position);
        if (inTerritory)
            return;

        normalizedDanger = Mathf.Clamp01(normalizedDanger);
        if (normalizedDanger <= 0f)
        {
            _sr.enabled = false;
            return;
        }

        _sr.enabled = true;
        float speed = Mathf.Lerp(2f, 18f, normalizedDanger);
        _blinkPhase += Time.deltaTime * speed;
        float a = (Mathf.Sin(_blinkPhase) * 0.5f + 0.5f) * Mathf.Lerp(0.25f, 0.95f, normalizedDanger);
        _sr.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, a);
    }

    public void ClearDrawingWarning()
    {
        if (!_sr || Collected) return;
        RefreshVisibility(TerritoryGrid.Instance);
    }
}
