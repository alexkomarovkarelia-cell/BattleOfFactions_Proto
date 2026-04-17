using System.Collections.Generic;
using UnityEngine;

// EnemyPoolResolver
// Это модуль, который решает:
//
// "Какие враги доступны на этой волне
//  и кого именно выбрать сейчас?"
//
// ВАЖНО:
// - это не спавнер
// - это не директор режима
// - это не планировщик волны
//
// Он просто работает как "слой выбора доступных врагов".
[DisallowMultipleComponent]
public class EnemyPoolResolver : MonoBehaviour
{
    [Header("Профили врагов для волн")]
    [SerializeField] private List<ArenaWaveEnemyProfile> enemyProfiles = new List<ArenaWaveEnemyProfile>();

    [Header("Запасной тип врага")]
    [SerializeField] private string fallbackEnemyTypeId = "melee_basic";

    [Header("Отладка")]
    [SerializeField] private bool showDebugLogs = false;

    public string GetRandomEnemyTypeIdForWave(ArenaRunContext runContext, int waveNumber)
    {
        if (runContext == null)
            return fallbackEnemyTypeId;

        List<ArenaWaveEnemyProfile> availableProfiles = GetAvailableProfiles(runContext, waveNumber);

        if (availableProfiles.Count == 0)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning(
                    $"EnemyPoolResolver: нет доступных врагов для wave {waveNumber}. " +
                    $"Использую fallback = {fallbackEnemyTypeId}"
                );
            }

            return fallbackEnemyTypeId;
        }

        ArenaWaveEnemyProfile selectedProfile = PickWeightedRandomProfile(availableProfiles);

        if (selectedProfile == null || string.IsNullOrWhiteSpace(selectedProfile.enemyTypeId))
            return fallbackEnemyTypeId;

        if (showDebugLogs)
        {
            Debug.Log(
                $"EnemyPoolResolver: selected enemyTypeId = {selectedProfile.enemyTypeId} " +
                $"for wave {waveNumber}"
            );
        }

        return selectedProfile.enemyTypeId;
    }

    private List<ArenaWaveEnemyProfile> GetAvailableProfiles(ArenaRunContext runContext, int waveNumber)
    {
        List<ArenaWaveEnemyProfile> result = new List<ArenaWaveEnemyProfile>();

        for (int i = 0; i < enemyProfiles.Count; i++)
        {
            ArenaWaveEnemyProfile profile = enemyProfiles[i];

            if (profile == null)
                continue;

            if (profile.CanAppear(runContext.CurrentGameMode, waveNumber))
                result.Add(profile);
        }

        return result;
    }

    private ArenaWaveEnemyProfile PickWeightedRandomProfile(List<ArenaWaveEnemyProfile> availableProfiles)
    {
        if (availableProfiles == null || availableProfiles.Count == 0)
            return null;

        int totalWeight = 0;

        for (int i = 0; i < availableProfiles.Count; i++)
        {
            totalWeight += Mathf.Max(1, availableProfiles[i].spawnWeight);
        }

        int roll = Random.Range(0, totalWeight);
        int currentWeight = 0;

        for (int i = 0; i < availableProfiles.Count; i++)
        {
            currentWeight += Mathf.Max(1, availableProfiles[i].spawnWeight);

            if (roll < currentWeight)
                return availableProfiles[i];
        }

        return availableProfiles[availableProfiles.Count - 1];
    }
}