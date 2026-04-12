using UnityEngine;
using UnityEngine.UI;

public class MinimapController : MonoBehaviour
{
    [Header("World")]
    public Camera worldCamera;
    public BoxCollider2D mapBounds;
    public Transform player;

    [Header("Minimap render")]
    public Camera minimapCamera;
    public RenderTexture renderTexture;
    public RawImage minimapRawImage;

    [Header("UI overlay")]
    public RectTransform overlayRoot;
    public RectTransform viewportRect;
    public RectTransform playerDot;

    [Header("RenderTexture")]
    public int renderTextureSize = 512;
    public int renderTextureDepth = 16;

    [Header("Viewport outline")]
    public Color viewportOutlineColor = new Color(1f, 1f, 1f, 0.35f);
    public float viewportOutlineThickness = 2f;

    Image _viewportImage;

    void Awake()
    {
        if (!worldCamera)
            worldCamera = Camera.main;

        if (!mapBounds && GameManager.Instance)
            mapBounds = GameManager.Instance.mapBounds;

        if (!player && GameManager.Instance && GameManager.Instance.player)
            player = GameManager.Instance.player.transform;

        EnsureRenderTexture();
        EnsureMinimapCamera();

        if (minimapRawImage && renderTexture)
            minimapRawImage.texture = renderTexture;

        if (viewportRect)
            _viewportImage = viewportRect.GetComponent<Image>();

        if (_viewportImage)
        {
            _viewportImage.color = viewportOutlineColor;
            _viewportImage.type = Image.Type.Simple;
        }
    }

    void OnDestroy()
    {
        if (minimapCamera && minimapCamera.targetTexture == renderTexture)
            minimapCamera.targetTexture = null;

        if (renderTexture && renderTexture.IsCreated())
            renderTexture.Release();
    }

    void LateUpdate()
    {
        if (!IsReady())
            return;

        FitMinimapCameraToBounds();
        UpdateOverlay();
    }

    bool IsReady()
    {
        return minimapCamera && renderTexture && mapBounds && worldCamera && minimapRawImage;
    }

    void EnsureRenderTexture()
    {
        if (renderTexture)
            return;

        int s = Mathf.Clamp(renderTextureSize, 64, 2048);
        renderTexture = new RenderTexture(s, s, renderTextureDepth, RenderTextureFormat.ARGB32)
        {
            name = "MinimapRT",
            antiAliasing = 1,
            filterMode = FilterMode.Bilinear
        };
        renderTexture.Create();
    }

    void EnsureMinimapCamera()
    {
        if (minimapCamera)
        {
            minimapCamera.targetTexture = renderTexture;
            minimapCamera.forceIntoRenderTexture = true;
            return;
        }

        var go = new GameObject("MinimapCamera");
        go.transform.SetParent(transform, false);
        minimapCamera = go.AddComponent<Camera>();
        minimapCamera.orthographic = true;
        minimapCamera.clearFlags = CameraClearFlags.SolidColor;
        minimapCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        minimapCamera.depth = worldCamera ? worldCamera.depth - 1f : -10f;
        minimapCamera.cullingMask = worldCamera ? worldCamera.cullingMask : ~0;
        minimapCamera.allowMSAA = false;
        minimapCamera.allowHDR = false;
        minimapCamera.targetTexture = renderTexture;
        minimapCamera.forceIntoRenderTexture = true;
    }

    void FitMinimapCameraToBounds()
    {
        Bounds b = mapBounds.bounds;
        float mapW = Mathf.Max(b.size.x, 0.001f);
        float mapH = Mathf.Max(b.size.y, 0.001f);
        float aspect = Mathf.Max(minimapCamera.aspect, 0.001f);
        float ortho = Mathf.Max(mapH * 0.5f, mapW / (2f * aspect));
        minimapCamera.orthographicSize = ortho;

        Vector3 c = b.center;
        if (worldCamera)
            c.z = worldCamera.transform.position.z;
        minimapCamera.transform.position = c;
    }

    void UpdateOverlay()
    {
        RectTransform rawRt = minimapRawImage.rectTransform;
        if (overlayRoot && overlayRoot.parent != rawRt)
            overlayRoot.SetParent(rawRt, false);

        if (overlayRoot)
        {
            overlayRoot.anchorMin = Vector2.zero;
            overlayRoot.anchorMax = Vector2.one;
            overlayRoot.offsetMin = Vector2.zero;
            overlayRoot.offsetMax = Vector2.zero;
            overlayRoot.pivot = new Vector2(0.5f, 0.5f);
        }

        Bounds b = mapBounds.bounds;

        if (playerDot)
        {
            Vector2 p = player ? (Vector2)player.position : Vector2.zero;
            Vector2 n = WorldToNormalized01(b, p);
            SetPointAnchors(playerDot, n);
        }

        if (viewportRect && worldCamera.orthographic)
        {
            float halfH = worldCamera.orthographicSize;
            float halfW = halfH * worldCamera.aspect;
            Vector2 cp = worldCamera.transform.position;

            Vector2 nMin = WorldToNormalized01(b, new Vector2(cp.x - halfW, cp.y - halfH));
            Vector2 nMax = WorldToNormalized01(b, new Vector2(cp.x + halfW, cp.y + halfH));

            float u0 = Mathf.Min(nMin.x, nMax.x);
            float u1 = Mathf.Max(nMin.x, nMax.x);
            float v0 = Mathf.Min(nMin.y, nMax.y);
            float v1 = Mathf.Max(nMin.y, nMax.y);

            viewportRect.anchorMin = new Vector2(u0, v0);
            viewportRect.anchorMax = new Vector2(u1, v1);
            viewportRect.pivot = new Vector2(0.5f, 0.5f);
            viewportRect.offsetMin = new Vector2(-viewportOutlineThickness, -viewportOutlineThickness);
            viewportRect.offsetMax = new Vector2(viewportOutlineThickness, viewportOutlineThickness);

            if (_viewportImage)
                _viewportImage.color = viewportOutlineColor;
        }
    }

    static Vector2 WorldToNormalized01(Bounds map, Vector2 world)
    {
        float nx = Mathf.InverseLerp(map.min.x, map.max.x, world.x);
        float ny = Mathf.InverseLerp(map.min.y, map.max.y, world.y);
        return new Vector2(Mathf.Clamp01(nx), Mathf.Clamp01(ny));
    }

    static void SetPointAnchors(RectTransform rt, Vector2 normalized01)
    {
        rt.anchorMin = normalized01;
        rt.anchorMax = normalized01;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
    }
}
