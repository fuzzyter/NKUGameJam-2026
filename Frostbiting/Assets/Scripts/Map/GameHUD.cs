using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class GameHUD : MonoBehaviour
{
    public GameManager gameManager;
    public Slider staminaSlider;

    [Tooltip("current / max stamina")]
    public TextMeshProUGUI staminaNumericText;

    [Tooltip("Camera viewport inside Treasure + Hunter count (no distinction)")]
    public TextMeshProUGUI viewportTreasureHunterCountTMP;

    [Tooltip("run time (MM:SS)")]
    public TextMeshProUGUI runTimeText;

    [Tooltip("collected treasures")]
    public TextMeshProUGUI treasureCountText;

    public Camera worldCamera;
    public Image[] hunterHearts;

    [Header("Stamina pickup float")]
    public TextMeshProUGUI staminaDeltaFloatTemplate;

    public Color staminaGainColor = new Color(0.4f, 0.95f, 0.5f, 1f);
    public Color staminaLossColor = new Color(1f, 0.4f, 0.4f, 1f);
    public float staminaFloatDuration = 0.85f;
    public float staminaFloatStartYOffset = -36f;
    public float staminaFloatEndYOffset = 72f;

    [Tooltip("distance between stamina pickup float")]
    public float staminaFloatRadialSeparation = 22f;

    [Tooltip("additional random offset")]
    public Vector2 staminaFloatRandomJitter = new Vector2(16f, 12f);

    const float GoldenAngleRad = 2.39996322972865332f;

    int _staminaFloatSpawnId;
    GameManager _pickupEventSubscribedTo;

    void SubscribePickupDeltaIfNeeded()
    {
        var gm = gameManager ? gameManager : GameManager.Instance;
        if (gm == null || _pickupEventSubscribedTo == gm) return;
        if (_pickupEventSubscribedTo != null)
            _pickupEventSubscribedTo.OnStaminaDeltaFromPickup -= OnStaminaDeltaFromPickup;
        _pickupEventSubscribedTo = gm;
        _pickupEventSubscribedTo.OnStaminaDeltaFromPickup += OnStaminaDeltaFromPickup;
    }

    void UnsubscribePickupDelta()
    {
        if (_pickupEventSubscribedTo == null) return;
        _pickupEventSubscribedTo.OnStaminaDeltaFromPickup -= OnStaminaDeltaFromPickup;
        _pickupEventSubscribedTo = null;
    }

    void OnEnable() => SubscribePickupDeltaIfNeeded();

    void OnDisable() => UnsubscribePickupDelta();

    void OnStaminaDeltaFromPickup(float delta)
    {
        if (!staminaDeltaFloatTemplate || Mathf.Approximately(delta, 0f)) return;
        StartCoroutine(StaminaFloatRoutine(delta));
    }

    IEnumerator StaminaFloatRoutine(float delta)
    {
        RectTransform templateRt = staminaDeltaFloatTemplate.rectTransform;
        Transform parent = templateRt.parent;
        GameObject go = Instantiate(staminaDeltaFloatTemplate.gameObject, parent);
        var rt = go.GetComponent<RectTransform>();
        var tmp = go.GetComponent<TextMeshProUGUI>();
        go.SetActive(true);
        go.transform.SetAsLastSibling();

        bool gain = delta > 0f;
        int amount = Mathf.RoundToInt(Mathf.Abs(delta));
        tmp.text = gain ? $"+{amount}" : $"-{amount}";
        Color c = gain ? staminaGainColor : staminaLossColor;
        tmp.color = c;

        rt.anchorMin = templateRt.anchorMin;
        rt.anchorMax = templateRt.anchorMax;
        rt.pivot = templateRt.pivot;
        rt.sizeDelta = templateRt.sizeDelta;
        rt.localScale = Vector3.one;

        Vector2 baseAnchored = templateRt.anchoredPosition;
        Vector2 planeOffset = ComputeStaminaFloatPlaneOffset();
        Vector2 start = baseAnchored + planeOffset + new Vector2(0f, staminaFloatStartYOffset);
        Vector2 end = baseAnchored + planeOffset + new Vector2(0f, staminaFloatEndYOffset);
        rt.anchoredPosition = start;

        float dur = Mathf.Max(0.05f, staminaFloatDuration);
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / dur);
            float ease = 1f - (1f - u) * (1f - u);
            rt.anchoredPosition = Vector2.LerpUnclamped(start, end, ease);
            c.a = 1f - u;
            tmp.color = c;
            yield return null;
        }

        Destroy(go);
    }

    Vector2 ComputeStaminaFloatPlaneOffset()
    {
        Vector2 radial = Vector2.zero;
        if (staminaFloatRadialSeparation > 0.001f)
        {
            float angle = _staminaFloatSpawnId++ * GoldenAngleRad;
            radial = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * staminaFloatRadialSeparation;
        }

        float jx = staminaFloatRandomJitter.x;
        float jy = staminaFloatRandomJitter.y;
        Vector2 jitter = new Vector2(
            jx > 0f ? Random.Range(-jx, jx) : 0f,
            jy > 0f ? Random.Range(-jy, jy) : 0f);

        return radial + jitter;
    }

    void Update()
    {
        if (!gameManager)
            gameManager = GameManager.Instance;
        SubscribePickupDeltaIfNeeded();
        if (!gameManager) return;

        if (runTimeText)
            runTimeText.text = RunTimeFormat.AsMinutesSeconds(gameManager.RunElapsedSeconds);

        if (treasureCountText)
            treasureCountText.text = $"{gameManager.treasureCount}/{gameManager.maxTreasure}";

        if (staminaSlider)
        {
            staminaSlider.maxValue = gameManager.staminaMax;
            staminaSlider.value = gameManager.stamina;
        }

        if (staminaNumericText)
        {
            int cur = Mathf.RoundToInt(gameManager.stamina);
            int max = Mathf.RoundToInt(gameManager.staminaMax);
            staminaNumericText.text = $"{cur}/{max}";
        }

        Camera cam = worldCamera ? worldCamera : Camera.main;
        if (viewportTreasureHunterCountTMP && cam && gameManager.encounterManager != null)
        {
            int n = 0;
            foreach (EncounterObject e in gameManager.encounterManager.GetEncounters())
            {
                if (e.Collected || e.data == null) continue;
                if (e.data.type != EncounterType.Treasure && e.data.type != EncounterType.Hunter)
                    continue;

                Vector3 v = cam.WorldToViewportPoint(e.transform.position);
                if (v.z > 0f && v.x > 0f && v.x < 1f && v.y > 0f && v.y < 1f)
                    n++;
            }

            viewportTreasureHunterCountTMP.text = n.ToString();
        }

        if (hunterHearts == null || hunterHearts.Length == 0)
            return;

        int lost = Mathf.Clamp(gameManager.hunterCount, 0, gameManager.maxHunter);
        int visible = Mathf.Clamp(gameManager.maxHunter - lost, 0, hunterHearts.Length);
        for (int i = 0; i < hunterHearts.Length; i++)
        {
            if (!hunterHearts[i]) continue;
            hunterHearts[i].enabled = i < visible;
        }
    }
}
