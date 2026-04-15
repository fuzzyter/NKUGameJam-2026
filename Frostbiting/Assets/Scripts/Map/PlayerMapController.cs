using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LineRenderer))]
public class PlayerMapController : MonoBehaviour
{
    public TerritoryGrid territory;
    public GameManager gameManager;
    public AreaConfirmUI areaUi;
    public BoxCollider2D mapBounds;
    public LineRenderer trailLine;

    [Header("Move")]
    public float moveSpeed = 10f;

    [Header("Draw / stamina")]
    public float staminaCostPerWorldUnit = 0.35f;
    public int minTrailCells = 8;

    public Vector2 LastFacingDirection { get; private set; } = Vector2.down;
    public bool IsDrawingOutsideOwned =>
        _isDrawing && territory != null && !territory.IsOwnedWorld(transform.position);

    readonly List<Vector2Int> _trail = new List<Vector2Int>();
    SpriteRenderer _playerSprite;
    Vector2Int _lastTrailCell = new Vector2Int(int.MinValue, int.MinValue);
    Vector2 _lastWorldOutside;
    bool _wasInside;
    bool _isDrawing;
    bool _waitingChoice;
    Vector2Int _pendingSeed;
    readonly List<Vector2Int> _trailSnapshot = new List<Vector2Int>();

    void Awake()
    {
        if (!trailLine)
            trailLine = GetComponent<LineRenderer>();
        trailLine.positionCount = 0;
        trailLine.useWorldSpace = true;
        trailLine.sortingOrder = 35;
        trailLine.widthMultiplier = 0.12f;
        trailLine.startColor = new Color(0.1f, 0.95f, 1f, 0.95f);
        trailLine.endColor = new Color(0.5f, 1f, 1f, 0.85f);
        AssignDefaultLineMaterial(trailLine);
        _playerSprite = GetComponent<SpriteRenderer>();
    }

    static void AssignDefaultLineMaterial(LineRenderer lr)
    {
        if (!lr) return;
        if (lr.sharedMaterial && lr.sharedMaterial.shader &&
            lr.sharedMaterial.shader.name != "Hidden/InternalErrorShader")
            return;

        Shader s = Shader.Find("Sprites/Default")
                   ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                   ?? Shader.Find("Universal Render Pipeline/Particles/Unlit")
                   ?? Shader.Find("Universal Render Pipeline/Unlit")
                   ?? Shader.Find("Unlit/Color");
        if (s != null)
            lr.sharedMaterial = new Material(s);
    }

    void Start()
    {
        if (!territory)
            territory = TerritoryGrid.Instance;
        if (!gameManager)
            gameManager = GameManager.Instance;
        if (!areaUi)
            areaUi = FindFirstObjectByType<AreaConfirmUI>(FindObjectsInactive.Include);
        if (!mapBounds && gameManager && gameManager.mapBounds)
            mapBounds = gameManager.mapBounds;

        _wasInside = territory && territory.IsOwnedWorld(transform.position);
        ApplySortingOrders();
    }

    void ApplySortingOrders()
    {
        var gm = gameManager ?? GameManager.Instance;
        if (gm == null) return;
        var psr = GetComponent<SpriteRenderer>();
        if (psr)
            psr.sortingOrder = gm.orderInLayerPlayerSprite;
        if (trailLine)
        {
            if (psr)
                trailLine.sortingLayerID = psr.sortingLayerID;
            trailLine.sortingOrder = gm.orderInLayerPlayerTrail;
        }
    }

    void Update()
    {
        if (!territory || !gameManager || gameManager.RunEnded)
        {
            GameAudioManager.I?.SetFootstepsActive(false);
            return;
        }

        if (_waitingChoice)
        {
            GameAudioManager.I?.SetFootstepsActive(false);
            Keyboard kb = Keyboard.current;
            bool yes = Input.GetKeyDown(KeyCode.Y) || (kb != null && kb.yKey.wasPressedThisFrame);
            bool no = Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.Escape) ||
                      (kb != null && (kb.nKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame));
            if (yes)
                PendingYes();
            else if (no)
                PendingNo();
            return;
        }

        MoveAndClamp(Time.deltaTime);
        GameAudioManager.I?.SetFootstepsActive(GetMoveInput().sqrMagnitude > 0.0001f);

        Vector2 pos = transform.position;
        bool inside = territory.IsOwnedWorld(pos);

        if (!_wasInside && inside && _isDrawing && _trail.Count >= minTrailCells)
        {
            _pendingSeed = territory.WorldToCell(_lastWorldOutside);
            _trailSnapshot.Clear();
            _trailSnapshot.AddRange(_trail);
            _waitingChoice = true;
            _isDrawing = false;
            GameAudioManager.I?.SetFootstepsActive(false);
            if (areaUi)
                areaUi.Show(PendingYes, PendingNo);
            Debug.Log("Yes[Y] or No[N] or Escape[Esc]");
            _wasInside = inside;
            UpdateTrailVisual();
            return;
        }

        if (!_wasInside && inside && _isDrawing && _trail.Count < minTrailCells)
        {
            _trail.Clear();
            _lastTrailCell = new Vector2Int(int.MinValue, int.MinValue);
            _isDrawing = false;
        }

        if (_wasInside && !inside)
        {
            _isDrawing = true;
            _trail.Clear();
            _lastTrailCell = new Vector2Int(int.MinValue, int.MinValue);
            _lastWorldOutside = pos;
            AppendTrailForPosition(pos);
        }
        else if (_isDrawing && !inside)
        {
            float dist = Vector2.Distance(pos, _lastWorldOutside);
            if (dist > 0.0001f)
                gameManager.ConsumeStamina(dist * staminaCostPerWorldUnit);
            AppendTrailForPosition(pos);
            _lastWorldOutside = pos;
        }
        else if (_isDrawing && inside)
        {
            _lastWorldOutside = pos;
        }

        UpdateTrailVisual();
        _wasInside = inside;
    }

    Vector2 GetMoveInput()
    {
        Vector2 input = Vector2.zero;
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed) input.y += 1f;
            if (kb.sKey.isPressed) input.y -= 1f;
            if (kb.aKey.isPressed) input.x -= 1f;
            if (kb.dKey.isPressed) input.x += 1f;
        }

        if (Input.GetKey(KeyCode.W)) input.y += 1f;
        if (Input.GetKey(KeyCode.S)) input.y -= 1f;
        if (Input.GetKey(KeyCode.A)) input.x -= 1f;
        if (Input.GetKey(KeyCode.D)) input.x += 1f;
        if (input.sqrMagnitude > 1f)
            input.Normalize();
        return input;
    }

    void MoveAndClamp(float dt)
    {
        Vector2 input = GetMoveInput();

        if (input.sqrMagnitude > 0.0001f)
            LastFacingDirection = input.normalized;

        if (_playerSprite && Mathf.Abs(input.x) > 0.01f)
            _playerSprite.flipX = input.x < 0f;

        Vector2 p = transform.position;
        p += input * (moveSpeed * dt);

        if (mapBounds)
        {
            Bounds b = mapBounds.bounds;
            const float pad = 0.2f;
            p.x = Mathf.Clamp(p.x, b.min.x + pad, b.max.x - pad);
            p.y = Mathf.Clamp(p.y, b.min.y + pad, b.max.y - pad);
        }

        transform.position = p;
    }

    void AppendTrailForPosition(Vector2 world)
    {
        Vector2Int c = territory.WorldToCell(world);
        if (!territory.InBounds(c))
            return;
        if (c == _lastTrailCell)
            return;
        _lastTrailCell = c;
        if (_trail.Count == 0 || _trail[_trail.Count - 1] != c)
            _trail.Add(c);
    }

    void UpdateTrailVisual()
    {
        if (!trailLine) return;
        int n = _trail.Count;
        trailLine.positionCount = n + 1;
        for (int i = 0; i < n; i++)
            trailLine.SetPosition(i, territory.CellToWorldCenter(_trail[i]));
        trailLine.SetPosition(n, transform.position);
    }

    void PendingYes()
    {
        if (!_waitingChoice) return;
        _waitingChoice = false;
        if (areaUi)
            areaUi.Hide();

        if (territory && gameManager)
        {
            if (territory.TryApplyCaptureWithFallback(_pendingSeed, _trailSnapshot, out List<Vector2Int> added))
                gameManager.ProcessCapture(added);
            else
                Debug.LogWarning("failed to apply capture");
        }

        _trail.Clear();
        _lastTrailCell = new Vector2Int(int.MinValue, int.MinValue);
        _isDrawing = false;
        AfterChoiceCleanup();
    }

    void PendingNo()
    {
        if (!_waitingChoice) return;
        _waitingChoice = false;
        if (areaUi)
            areaUi.Hide();

        _trail.Clear();
        _lastTrailCell = new Vector2Int(int.MinValue, int.MinValue);
        _isDrawing = false;
        AfterChoiceCleanup();
    }

    void AfterChoiceCleanup()
    {
        UpdateTrailVisual();
        if (gameManager?.encounterManager == null) return;
        TerritoryGrid g = gameManager.territoryGrid ?? TerritoryGrid.Instance;
        foreach (EncounterObject e in gameManager.encounterManager.GetEncounters())
            e.RefreshVisibility(g);
    }

    public void WarpTo(Vector2 world)
    {
        transform.position = world;
        _wasInside = territory && territory.IsOwnedWorld(world);
        _trail.Clear();
        _isDrawing = false;
        _waitingChoice = false;
        _lastTrailCell = new Vector2Int(int.MinValue, int.MinValue);
        if (trailLine)
            trailLine.positionCount = 0;
    }
}
