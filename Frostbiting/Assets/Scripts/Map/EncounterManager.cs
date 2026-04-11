using System.Collections.Generic;
using UnityEngine;

public class EncounterManager : MonoBehaviour
{
    public BoxCollider2D mapBounds;

    public List<EncounterData> encounterDatas;

    private readonly List<Vector2> spawnedPositions = new List<Vector2>();
    private readonly List<EncounterObject> allEncounters = new List<EncounterObject>();

    public IReadOnlyList<EncounterObject> GetEncounters() => allEncounters;

    void Start()
    {
        SpawnAll();
    }

    public Vector2 GetSuggestedPlayerSpawn(float minDistToEncounter)
    {
        if (!mapBounds)
            return Vector2.zero;

        for (int i = 0; i < 80; i++)
        {
            Vector2 p = GetRandomPosition();
            if (IsFarEnough(p, minDistToEncounter))
                return p;
        }

        return mapBounds.bounds.center;
    }

    void SpawnAll()
    {
        SpawnByType(EncounterType.Treasure);
        SpawnByType(EncounterType.Hunter);

        SpawnAroundType(EncounterType.Good);
        SpawnAroundType(EncounterType.Bad);
    }

    void SpawnByType(EncounterType type)
    {
        foreach (var data in encounterDatas)
        {
            if (data.type == type)
            {
                for (int i = 0; i < data.spawnCount; i++)
                {
                    Vector2 pos = GetValidPosition(data);

                    GameObject obj = Instantiate(data.prefab, pos, Quaternion.identity);
                    obj.transform.SetParent(transform, true);

                    var encounter = obj.GetComponent<EncounterObject>();
                    encounter.data = data;

                    allEncounters.Add(encounter);
                    spawnedPositions.Add(pos);
                }
            }
        }
    }

    void SpawnAroundType(EncounterType type)
    {
        foreach (var data in encounterDatas)
        {
            if (data.type == type)
                SpawnAroundAnchors(data);
        }
    }

    Vector2 GetRandomPosition()
    {
        Bounds bounds = mapBounds.bounds;

        float x = Random.Range(bounds.min.x, bounds.max.x);
        float y = Random.Range(bounds.min.y, bounds.max.y);

        return new Vector2(x, y);
    }

    bool IsFarEnough(Vector2 pos, float minDist)
    {
        foreach (var p in spawnedPositions)
        {
            if (Vector2.Distance(pos, p) < minDist)
                return false;
        }

        return true;
    }

    Vector2 GetNearPosition(EncounterData data)
    {
        List<Vector2> candidates = new List<Vector2>();

        foreach (var e in allEncounters)
        {
            if (e.data.type == data.targetType)
                candidates.Add(e.transform.position);
        }

        if (candidates.Count == 0)
            return GetRandomPosition();

        Vector2 basePos = candidates[Random.Range(0, candidates.Count)];
        Vector2 offset = Random.insideUnitCircle * data.nearRadius;

        return basePos + offset;
    }

    Vector2 GetRingPosition(Vector2 center, float minR, float maxR)
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float radius = Random.Range(minR, maxR);

        Vector2 offset = new Vector2(
            Mathf.Cos(angle),
            Mathf.Sin(angle)
        ) * radius;

        return center + offset;
    }

    Vector2 GetValidPosition(EncounterData data)
    {
        int maxTries = 30;

        for (int i = 0; i < maxTries; i++)
        {
            Vector2 pos;

            if (data.spawnNearOther)
                pos = GetNearPosition(data);
            else
                pos = GetRandomPosition();

            if (IsFarEnough(pos, data.minDistance))
                return pos;
        }

        return GetRandomPosition();
    }

    void SpawnAroundAnchors(EncounterData data)
    {
        List<Vector2> anchors = new List<Vector2>();

        foreach (var e in allEncounters)
        {
            if (e.data.type == data.targetType)
                anchors.Add(e.transform.position);
        }

        if (anchors.Count == 0) return;

        for (int i = 0; i < data.spawnCount; i++)
        {
            Vector2 pos = Vector2.zero;

            int tries = 20;

            for (int t = 0; t < tries; t++)
            {
                Vector2 center = anchors[Random.Range(0, anchors.Count)];

                if (data.useRingSpawn)
                    pos = GetRingPosition(center, data.minRadius, data.maxRadius);
                else
                    pos = center + Random.insideUnitCircle * data.nearRadius;

                if (IsFarEnough(pos, data.minDistance))
                    break;
            }

            GameObject obj = Instantiate(data.prefab, pos, Quaternion.identity);
            obj.transform.SetParent(transform, true);

            var encounter = obj.GetComponent<EncounterObject>();
            encounter.data = data;

            allEncounters.Add(encounter);
            spawnedPositions.Add(pos);
        }
    }
}
