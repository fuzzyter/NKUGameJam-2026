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
        if (!_sr)
            _sr = GetComponentInChildren<SpriteRenderer>(true);
        if (_sr)
            _baseColor = _sr.color;
    }

    public void RefreshVisibility(TerritoryGrid grid)
    {
        if (grid == null) return;
        bool inTerritory = grid.IsOwnedWorld(transform.position);
        var gm = GameManager.Instance;
        int orderEnc = gm != null ? gm.orderInLayerEncounter : 20;
        foreach (SpriteRenderer r in GetComponentsInChildren<SpriteRenderer>(true))
        {
            r.enabled = inTerritory;
            if (inTerritory)
            {
                r.sortingOrder = orderEnc;
                if (r == _sr)
                    r.color = _baseColor;
            }
        }
    }

    public void MarkCollected()
    {
        Collected = true;
    }
}
