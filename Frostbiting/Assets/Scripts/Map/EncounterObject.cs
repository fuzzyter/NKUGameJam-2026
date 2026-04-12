using UnityEngine;

public class EncounterObject : MonoBehaviour
{
    public EncounterData data;
    public bool Collected { get; private set; }

    SpriteRenderer _sr;
    Color _baseColor = Color.white;

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
}
