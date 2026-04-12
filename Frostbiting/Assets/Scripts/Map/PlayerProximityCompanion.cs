using UnityEngine;

/// <summary>
/// 플레이어 뒤를 따라다니는 스프라이트. 맵에 있는(수집 전) 에카운터와의 거리에 따라 깜빡임·효과음 간격이 빨라짐.
/// </summary>
public class PlayerProximityCompanion : MonoBehaviour
{
    [Header("Refs (비워도 부모/씬에서 찾음)")]
    public PlayerMapController player;
    public GameManager gameManager;
    [Tooltip("비우면 같은 오브젝트 또는 자식에서 SpriteRenderer 검색")]
    public SpriteRenderer spriteRenderer;

    [Header("감지")]
    [Tooltip("이 거리 안에 수집 전 에카운터가 하나라도 있으면 반응")]
    public float warningRadius = 6f;
    [Tooltip("켜면: 영역 밖에서 트레일을 그리는 동안에만 반응. 끄면 이동 중에도 근처 에카운터에 반응")]
    public bool onlyWhileDrawingOutsideOwned = false;

    [Header("깜빡임")]
    public float minBlinkSpeed = 2f;
    public float maxBlinkSpeed = 20f;

    [Header("효과음")]
    public AudioClip proximityClip;
    public float minBeepInterval = 0.07f;
    public float maxBeepInterval = 0.45f;
    [Range(0.05f, 1f)] public float beepVolume = 0.55f;

    [Header("플레이어 뒤 오프셋")]
    public float behindDistance = 0.28f;

    AudioSource _audio;
    Color _baseColor = Color.white;
    float _blinkPhase;
    float _nextBeepTime;

    void Awake()
    {
        if (!spriteRenderer)
            spriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>(true);
        if (spriteRenderer)
        {
            _baseColor = spriteRenderer.color;
            spriteRenderer.enabled = false;
            EnsureSpriteMaterial(spriteRenderer);
        }

        _audio = GetComponent<AudioSource>();
        if (!_audio)
            _audio = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake = false;
        _audio.spatialBlend = 0f;

        if (!player)
            player = GetComponentInParent<PlayerMapController>();
        if (!gameManager)
            gameManager = GameManager.Instance;

        SortBehindPlayer();
    }

    static void EnsureSpriteMaterial(SpriteRenderer sr)
    {
        if (!sr || sr.sharedMaterial && sr.sharedMaterial.shader &&
            sr.sharedMaterial.shader.name != "Hidden/InternalErrorShader")
            return;
        Shader s = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                   ?? Shader.Find("Sprites/Default");
        if (s != null)
            sr.sharedMaterial = new Material(s);
    }

    void SortBehindPlayer()
    {
        if (!spriteRenderer || !player) return;
        var psr = player.GetComponent<SpriteRenderer>();
        if (psr)
            spriteRenderer.sortingLayerID = psr.sortingLayerID;
        spriteRenderer.sortingOrder = psr ? psr.sortingOrder - 2 : 0;
    }

    void LateUpdate()
    {
        if (!player || !gameManager || gameManager.RunEnded || spriteRenderer == null ||
            gameManager.encounterManager == null)
            return;

        TerritoryGrid grid = TerritoryGrid.Instance;
        if (grid == null || !grid.IsReady)
            return;

        if (onlyWhileDrawingOutsideOwned && !player.IsDrawingOutsideOwned)
        {
            HideCompanion();
            return;
        }

        Vector2 p = player.transform.position;
        float minD = float.MaxValue;
        foreach (EncounterObject e in gameManager.encounterManager.GetEncounters())
        {
            if (e.Collected) continue;
            float d = Vector2.Distance(p, e.transform.position);
            if (d < minD) minD = d;
        }

        if (minD >= warningRadius)
        {
            HideCompanion();
            return;
        }

        float urgency = 1f - Mathf.Clamp01(minD / warningRadius);
        spriteRenderer.enabled = true;
        float speed = Mathf.Lerp(minBlinkSpeed, maxBlinkSpeed, urgency);
        _blinkPhase += Time.deltaTime * speed;
        float a = Mathf.Lerp(0.25f, 1f, (Mathf.Sin(_blinkPhase) * 0.5f + 0.5f) * urgency + (1f - urgency) * 0.35f);
        Color c = _baseColor;
        c.a = Mathf.Clamp01(a);
        spriteRenderer.color = c;

        Vector2 back = -player.LastFacingDirection.normalized;
        if (back.sqrMagnitude < 0.01f)
            back = Vector2.down;
        transform.localPosition = new Vector3(back.x * behindDistance, back.y * behindDistance, 0.02f);

        if (proximityClip && Time.time >= _nextBeepTime)
        {
            float interval = Mathf.Lerp(maxBeepInterval, minBeepInterval, urgency);
            _audio.PlayOneShot(proximityClip, Mathf.Lerp(0.2f, beepVolume, urgency));
            _nextBeepTime = Time.time + interval;
        }
    }

    void HideCompanion()
    {
        if (!spriteRenderer) return;
        spriteRenderer.enabled = false;
        spriteRenderer.color = _baseColor;
        _nextBeepTime = Time.time;
    }
}
