using UnityEngine;


public class PlayerProximityCompanion : MonoBehaviour
{
    [Header("Refs")]
    public PlayerMapController player;
    public GameManager gameManager;
    public SpriteRenderer spriteRenderer;

    [Header("Detection")]
    public float warningRadius = 6f;
    public bool onlyWhileDrawingOutsideOwned = false;

    [Header("Blinking")]
    public float minBlinkSpeed = 2f;
    public float maxBlinkSpeed = 20f;

    [Header("Sound")]
    public AudioClip proximityClip;
    public float minBeepInterval = 0.07f;
    public float maxBeepInterval = 0.45f;
    [Range(0.05f, 1f)] public float beepVolume = 0.55f;
    [Tooltip("If true, headphones give left–right cues.")]
    public bool spatialProximityBeeps = true;
    [Tooltip("distance from the listener")]
    public float spatialBeepMinDistance = 48f;
    [Tooltip("spread of the sound")]
    [Range(0f, 180f)] public float spatialSpread = 0f;
    [Tooltip("stereo exaggeration")]
    [Range(1f, 4f)] public float spatialStereoExaggeration = 1.85f;
    [Tooltip("spatial listener override")]
    public Transform spatialListenerOverride;

    [Header("Player behind offset")]
    public float behindDistance = 0.28f;

    AudioSource _beepAudio;
    SpriteRenderer _playerSpriteForFlip;
    Transform _spatialListener;
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

        SetupBeepAudioSource();
        CacheSpatialListener();

        if (!player)
            player = GetComponentInParent<PlayerMapController>();
        if (player)
            _playerSpriteForFlip = player.GetComponent<SpriteRenderer>();
        if (!gameManager)
            gameManager = GameManager.Instance;

        SortBehindPlayer();
    }

    void SetupBeepAudioSource()
    {
        var holder = new GameObject("ProximitySpatialBeep");
        holder.transform.SetParent(transform, false);
        _beepAudio = holder.AddComponent<AudioSource>();
        _beepAudio.playOnAwake = false;
        _beepAudio.dopplerLevel = 0f;
        ApplySpatialBeepSettings();

        var redundant = GetComponent<AudioSource>();
        if (redundant)
            Destroy(redundant);
    }

    void ApplySpatialBeepSettings()
    {
        if (!_beepAudio) return;
        if (spatialProximityBeeps)
        {
            _beepAudio.spatialBlend = 1f;
            _beepAudio.spread = spatialSpread;
            _beepAudio.rolloffMode = AudioRolloffMode.Logarithmic;
            _beepAudio.minDistance = Mathf.Max(0.01f, spatialBeepMinDistance);
            _beepAudio.maxDistance = _beepAudio.minDistance * 4f;
        }
        else
        {
            _beepAudio.spatialBlend = 0f;
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (_beepAudio)
            ApplySpatialBeepSettings();
    }
#endif

    void CacheSpatialListener()
    {
        if (spatialListenerOverride)
        {
            _spatialListener = spatialListenerOverride;
            return;
        }

        var al = Object.FindFirstObjectByType<AudioListener>(FindObjectsInactive.Exclude);
        _spatialListener = al ? al.transform : Camera.main ? Camera.main.transform : null;
    }

    Vector3 WorldPositionForSpatialBeep(EncounterObject nearest)
    {
        Vector3 source = nearest.transform.position;
        if (!spatialProximityBeeps || spatialStereoExaggeration <= 1.001f)
            return source;

        if (!_spatialListener)
            CacheSpatialListener();
        if (!_spatialListener)
            return source;

        Transform lt = _spatialListener;
        Vector3 L = lt.position;
        Vector3 d = source - L;
        return L
               + lt.right * (Vector3.Dot(d, lt.right) * spatialStereoExaggeration)
               + lt.up * (Vector3.Dot(d, lt.up) * spatialStereoExaggeration)
               + lt.forward * Vector3.Dot(d, lt.forward);
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
        var gm = gameManager ?? GameManager.Instance;
        spriteRenderer.sortingOrder = gm != null ? gm.orderInLayerCompanion : 40;
    }

    void LateUpdate()
    {
        if (!player || !gameManager || gameManager.RunEnded || spriteRenderer == null ||
            gameManager.encounterManager == null)
            return;

        TerritoryGrid grid = TerritoryGrid.Instance;
        if (grid == null || !grid.IsReady)
            return;

        if (spriteRenderer && _playerSpriteForFlip)
            spriteRenderer.flipX = _playerSpriteForFlip.flipX;

        if (onlyWhileDrawingOutsideOwned && !player.IsDrawingOutsideOwned)
        {
            HideCompanion();
            return;
        }

        Vector2 p = player.transform.position;
        float minD = float.MaxValue;
        EncounterObject nearest = null;
        foreach (EncounterObject e in gameManager.encounterManager.GetEncounters())
        {
            if (e.Collected) continue;
            float d = Vector2.Distance(p, e.transform.position);
            if (d < minD)
            {
                minD = d;
                nearest = e;
            }
        }

        if (minD >= warningRadius)
        {
            HideCompanion();
            return;
        }

        float urgency = 1f - Mathf.Clamp01(minD / warningRadius);
        spriteRenderer.enabled = true;
        SortBehindPlayer();
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

        if (proximityClip && nearest && _beepAudio && Time.time >= _nextBeepTime)
        {
            if (spatialProximityBeeps)
                _beepAudio.transform.position = WorldPositionForSpatialBeep(nearest);
            else
                _beepAudio.transform.localPosition = Vector3.zero;

            float interval = Mathf.Lerp(maxBeepInterval, minBeepInterval, urgency);
            _beepAudio.PlayOneShot(proximityClip, Mathf.Lerp(0.2f, beepVolume, urgency));
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
