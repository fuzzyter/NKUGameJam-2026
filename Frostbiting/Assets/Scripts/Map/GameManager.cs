using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Refs")]
    public EncounterManager encounterManager;
    public TerritoryGrid territoryGrid;
    public PlayerMapController player;
    public AreaConfirmUI areaConfirmUi;

    [Header("Player / map")]
    public BoxCollider2D mapBounds;
    public int startOwnedHalfW = 3;
    public int startOwnedHalfH = 3;
    public float playerSpawnMinEncounterDist = 2f;

    [Header("2D Order in Layer")]
    public int orderInLayerTerritory = 10;
    public int orderInLayerEncounter = 20;
    public int orderInLayerPlayerSprite = 30;
    public int orderInLayerPlayerTrail = 35;
    public int orderInLayerCompanion = 40;

    [Header("Stats")]
    public float staminaMax = 100f;
    public float stamina = 100f;
    public int treasureCount;
    public int hunterCount;
    public int maxTreasure = 5;
    public int maxHunter = 3;

    [Header("Scenes (add to Build Settings)")]
    public string winSceneName = "EndWin";
    public string loseSceneName = "EndLose";

    public bool RunEnded { get; private set; }

    /// <summary>Good/Bad 획득 시 적용된 스태미나 변화량 (UI 피드백용).</summary>
    public event System.Action<float> OnStaminaDeltaFromPickup;

    public float RunElapsedSeconds => RunEnded ? _frozenRunElapsed : Time.timeSinceLevelLoad;

    float _frozenRunElapsed;

    void Awake()
    {
        Instance = this;
    }

    IEnumerator Start()
    {
        yield return null;
        yield return null;

        if (!encounterManager)
            encounterManager = FindFirstObjectByType<EncounterManager>();
        if (!mapBounds && encounterManager)
            mapBounds = encounterManager.mapBounds;
        if (!territoryGrid)
            territoryGrid = FindFirstObjectByType<TerritoryGrid>();
        if (!player)
            player = FindFirstObjectByType<PlayerMapController>();
        if (!areaConfirmUi)
            areaConfirmUi = FindFirstObjectByType<AreaConfirmUI>(FindObjectsInactive.Include);

        if (territoryGrid && mapBounds)
        {
            territoryGrid.Init(mapBounds);
            Vector2 spawn = encounterManager
                ? encounterManager.GetSuggestedPlayerSpawn(playerSpawnMinEncounterDist)
                : mapBounds.bounds.center;
            territoryGrid.SeedRectAroundWorld(spawn, startOwnedHalfW, startOwnedHalfH);
            if (player)
                player.WarpTo(spawn);
        }

        stamina = staminaMax;
        ApplyAllEncounterVisibility();
    }

    void Update()
    {
        if (RunEnded) return;
        if (stamina <= 0f)
            EndRun(false);
    }

    public void AddStamina(float delta)
    {
        stamina = Mathf.Clamp(stamina + delta, 0f, staminaMax);
    }

    public void ConsumeStamina(float amount)
    {
        if (RunEnded) return;
        stamina = Mathf.Max(0f, stamina - amount);
    }

    public void ApplyAllEncounterVisibility()
    {
        if (!encounterManager) return;
        TerritoryGrid grid = territoryGrid ? territoryGrid : TerritoryGrid.Instance;
        foreach (EncounterObject e in encounterManager.GetEncounters())
            e.RefreshVisibility(grid);
    }

    public void ProcessCapture(IReadOnlyList<Vector2Int> addedCells)
    {
        if (addedCells == null || addedCells.Count == 0 || encounterManager == null || territoryGrid == null)
            return;

        HashSet<Vector2Int> set = new HashSet<Vector2Int>(addedCells);
        foreach (EncounterObject e in encounterManager.GetEncounters())
        {
            if (e.Collected || e.data == null) continue;
            Vector2Int c = territoryGrid.WorldToCell(e.transform.position);
            if (set.Contains(c))
                ApplyEncounter(e);
        }

        ApplyAllEncounterVisibility();
        CheckGameState();
    }

    void ApplyEncounter(EncounterObject e)
    {
        if (e.Collected || e.data == null) return;
        e.MarkCollected();

        switch (e.data.type)
        {
            case EncounterType.Treasure:
                treasureCount++;
                break;
            case EncounterType.Good:
                AddStamina(20f);
                OnStaminaDeltaFromPickup?.Invoke(20f);
                break;
            case EncounterType.Bad:
                AddStamina(-15f);
                OnStaminaDeltaFromPickup?.Invoke(-15f);
                break;
            case EncounterType.Hunter:
                hunterCount++;
                break;
        }

        if (stamina <= 0f)
            EndRun(false);
    }

    void CheckGameState()
    {
        if (RunEnded) return;
        if (hunterCount >= maxHunter)
            EndRun(false);
        if (treasureCount >= maxTreasure)
            EndRun(true);
    }

    public void EndRun(bool victory)
    {
        if (RunEnded) return;
        RunEnded = true;
        float t = Time.timeSinceLevelLoad;
        _frozenRunElapsed = t;
        PlayerPrefs.SetFloat("LastRunTime", t);
        PlayerPrefs.SetInt("LastRunVictory", victory ? 1 : 0);
        PlayerPrefs.Save();

        string scene = victory ? winSceneName : loseSceneName;
        if (!string.IsNullOrEmpty(scene))
        {
            try
            {
                SceneManager.LoadScene(scene);
            }
            catch
            {
                Debug.LogWarning($"Could not load scene '{scene}'. Add it to Build Settings and name it correctly.");
            }
        }
    }
}
