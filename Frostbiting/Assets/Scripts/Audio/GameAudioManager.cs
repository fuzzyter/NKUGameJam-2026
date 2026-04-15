using UnityEngine;
using UnityEngine.SceneManagement;

public class GameAudioManager : MonoBehaviour
{
    public static GameAudioManager I { get; private set; }

    [Header("BGM (loop, all scenes)")]
    public AudioClip bgmClip;
    [Range(0f, 1f)] public float bgmVolume = 0.45f;

    [Header("Scene stingers (when scene finishes loading)")]
    public string winSceneName = "WinScene";
    public string loseSceneName = "GameoverScene";
    public AudioClip winSceneStinger;
    public AudioClip loseSceneStinger;
    [Range(0f, 1f)] public float stingerVolume = 0.9f;

    [Header("Player move (loop while moving on map)")]
    public AudioClip footstepLoopClip;
    [Range(0f, 1f)] public float footstepVolume = 0.35f;

    [Header("Territory — area capture confirmed (Y)")]
    public AudioClip areaCaptureCompleteClip;
    [Range(0f, 1f)] public float areaCaptureVolume = 0.9f;

    [Header("Pickups — one per EncounterType")]
    public AudioClip pickupTreasure;
    public AudioClip pickupGood;
    public AudioClip pickupBad;
    public AudioClip pickupHunter;
    [Range(0f, 1f)] public float pickupVolume = 0.85f;

    [Header("UI buttons")]
    public AudioClip uiButtonClick;
    [Range(0f, 1f)] public float uiSfxVolume = 0.75f;

    AudioSource _bgm;
    AudioSource _footsteps;
    AudioSource _sfx;
    bool _bgmStarted;

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);

        _bgm = gameObject.AddComponent<AudioSource>();
        _bgm.playOnAwake = false;
        _bgm.loop = true;
        _bgm.spatialBlend = 0f;

        _footsteps = gameObject.AddComponent<AudioSource>();
        _footsteps.playOnAwake = false;
        _footsteps.loop = true;
        _footsteps.spatialBlend = 0f;

        _sfx = gameObject.AddComponent<AudioSource>();
        _sfx.playOnAwake = false;
        _sfx.loop = false;
        _sfx.spatialBlend = 0f;
    }

    void OnDestroy()
    {
        if (I == this)
            I = null;
    }

    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;

    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void Start() => TryStartBgm();

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryStartBgm();

        if (!scene.IsValid()) return;

        string n = scene.name;
        if (n == winSceneName && winSceneStinger)
            _sfx.PlayOneShot(winSceneStinger, stingerVolume);
        else if (n == loseSceneName && loseSceneStinger)
            _sfx.PlayOneShot(loseSceneStinger, stingerVolume);
    }

    void TryStartBgm()
    {
        if (_bgmStarted || !bgmClip || !_bgm) return;
        _bgm.clip = bgmClip;
        _bgm.volume = bgmVolume;
        _bgm.Play();
        _bgmStarted = true;
    }

    public void SetFootstepsActive(bool active)
    {
        if (!footstepLoopClip || !_footsteps) return;

        if (active)
        {
            if (!_footsteps.isPlaying)
            {
                _footsteps.clip = footstepLoopClip;
                _footsteps.volume = footstepVolume;
                _footsteps.Play();
            }
        }
        else
            _footsteps.Stop();
    }

    public void PlayAreaCaptureComplete()
    {
        if (areaCaptureCompleteClip && _sfx)
            _sfx.PlayOneShot(areaCaptureCompleteClip, areaCaptureVolume);
    }

    public void PlayPickup(EncounterType type)
    {
        if (!_sfx) return;
        AudioClip c = null;
        switch (type)
        {
            case EncounterType.Treasure: c = pickupTreasure; break;
            case EncounterType.Good: c = pickupGood; break;
            case EncounterType.Bad: c = pickupBad; break;
            case EncounterType.Hunter: c = pickupHunter; break;
        }

        if (c)
            _sfx.PlayOneShot(c, pickupVolume);
    }

    public void PlayUiClick()
    {
        if (uiButtonClick && _sfx)
            _sfx.PlayOneShot(uiButtonClick, uiSfxVolume);
    }
}
