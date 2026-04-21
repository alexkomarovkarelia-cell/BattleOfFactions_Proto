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
// Он работает как слой выбора доступных врагов.
// Теперь у него есть ещё и pipeline модификаторов пула.
[DisallowMultipleComponent]
public class EnemyPoolResolver : MonoBehaviour
{
    [Header("Профили врагов для волн")]
    [SerializeField] private List<ArenaWaveEnemyProfile> enemyProfiles = new List<ArenaWaveEnemyProfile>();

    [Header("Модификаторы пула")]
    [SerializeField] private ArenaEnemyPoolModifierPipeline enemyPoolModifierPipeline;

    [Header("Запасной тип врага")]
    [SerializeField] private string fallbackEnemyTypeId = "melee_basic";

    [Header("Отладка")]
    [SerializeField] private bool showDebugLogs = false;

    public string GetRandomEnemyTypeIdForWave(ArenaRunContext runContext, int waveNumber)
    {
        if (runContext == null)
            return fallbackEnemyTypeId;

        List<ArenaEnemyPoolCandidate> candidates = BuildAvailableCandidates(runContext, waveNumber);

        // Пропускаем через pipeline модификаторов
        if (enemyPoolModifierPipeline != null)
        {
            enemyPoolModifierPipeline.ApplyPoolModifiers(runContext, waveNumber, candidates);
        }

        if (candidates == null || candidates.Count == 0)
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

        ArenaEnemyPoolCandidate selectedCandidate = PickWeightedRandomCandidate(candidates);

        if (selectedCandidate == null || string.IsNullOrWhiteSpace(selectedCandidate.enemyTypeId))
            return fallbackEnemyTypeId;

        if (showDebugLogs)
        {
            Debug.Log(
                $"EnemyPoolResolver: selected enemyTypeId = {selectedCandidate.enemyTypeId} " +
                $"for wave {waveNumber}"
            );
        }

        return selectedCandidate.enemyTypeId;
    }

    private List<ArenaEnemyPoolCandidate> BuildAvailableCandidates(ArenaRunContext runContext, int waveNumber)
    {
        List<ArenaEnemyPoolCandidate> result = new List<ArenaEnemyPoolCandidate>();

        for (int i = 0; i < enemyProfiles.Count; i++)
        {
            ArenaWaveEnemyProfile profile = enemyProfiles[i];

            if (profile == null)
                continue;

            if (!profile.CanAppear(runContext.CurrentGameMode, waveNumber))
                continue;

            ArenaEnemyPoolCandidate candidate = new ArenaEnemyPoolCandidate
            {
                sourceProfile = profile,
                enemyTypeId = profile.enemyTypeId,
                effectiveWeight = Mathf.Max(1, profile.spawnWeight),
                isEnabled = true,
                debugNote = $"Base candidate from profile {profile.name}"
            };

            result.Add(candidate);
        }

        return result;
    }

    private ArenaEnemyPoolCandidate PickWeightedRandomCandidate(List<ArenaEnemyPoolCandidate> candidates)
    {
        if (candidates == null || candidates.Count == 0)
            return null;

        int totalWeight = 0;

        for (int i = 0; i < candidates.Count; i++)
        {
            ArenaEnemyPoolCandidate candidate = candidates[i];

            if (candidate == null || !candidate.isEnabled)
                continue;

            totalWeight += Mathf.Max(1, candidate.effectiveWeight);
        }

        if (totalWeight <= 0)
            return null;

        int roll = Random.Range(0, totalWeight);
        int currentWeight = 0;

        for (int i = 0; i < candidates.Count; i++)
        {
            ArenaEnemyPoolCandidate candidate = candidates[i];

            if (candidate == null || !candidate.isEnabled)
                continue;

            currentWeight += Mathf.Max(1, candidate.effectiveWeight);

            if (roll < currentWeight)
                return candidate;
        }

        return candidates[candidates.Count - 1];
    }
}