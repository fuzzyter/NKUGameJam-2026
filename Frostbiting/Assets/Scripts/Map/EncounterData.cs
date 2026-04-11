using UnityEngine;

[CreateAssetMenu(fileName = "EncounterData", menuName = "Encounter/Data")]
public class EncounterData : ScriptableObject
{
    public string encounterName;
    public EncounterType type;

    public GameObject prefab;

    public int spawnCount = 5;

    public float minDistance = 2f;

    public bool spawnNearOther = false;
    public EncounterType targetType;


    public bool useRingSpawn = false;
    public float nearRadius = 5f;

    public float minRadius = 2f;
    public float maxRadius = 6f;
}