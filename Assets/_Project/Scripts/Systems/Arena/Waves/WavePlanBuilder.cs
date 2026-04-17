using System.Collections.Generic;
using UnityEngine;

// WavePlanBuilder
// Это планировщик конкретной волны.
//
// Что он делает:
// 1) получает решение директора
// 2) выбирает активные зоны на волну
// 3) выбирает типы врагов через EnemyPoolResolver
// 4) собирает конкретный WaveExecutionPlan
//
// ВАЖНО:
// - он не спавнит
// - он не считает режим целиком
// - он не решает, когда закончить забег
[DisallowMultipleComponent]
public class WavePlanBuilder : MonoBehaviour
{
    [Header("Исполнители")]
    [SerializeField] private EnemyPoolResolver enemyPoolResolver;

    [Header("Общая логика планирования")]
    [SerializeField] private bool useRandomSpawnZones = true;
    [SerializeField] private float defaultSpawnInterval = 0.5f;
    [SerializeField] private int fallbackSpawnZoneCount = 4;

    [Header("Элитные правила")]
    [SerializeField] private string defaultBasicEnemyTypeId = "melee_basic";
    [SerializeField] private string defaultEliteEnemyTypeId = "melee_elite";
    [SerializeField] private bool convertOneSpawnToElite = true;

    [Header("Анти-повтор")]
    [SerializeField] private bool avoidImmediateSameSpawnZone = true;

    [Header("Отладка")]
    [SerializeField] private bool showDebugLogs = true;

    private int lastUsedSpawnZoneIndex = -1;

    public bool TryBuildWavePlan(
        ArenaDirectorDecision directorDecision,
        ArenaRunContext runContext,
        out WaveExecutionPlan wavePlan)
    {
        wavePlan = null;

        if (directorDecision == null)
        {
            wavePlan = WaveExecutionPlan.CreateEmpty("DirectorDecision is null");
            return false;
        }

        if (!directorDecision.isValidDecision)
        {
            wavePlan = WaveExecutionPlan.CreateEmpty("DirectorDecision is invalid");
            return false;
        }

        if (runContext == null)
        {
            wavePlan = WaveExecutionPlan.CreateEmpty("RunContext is null");
            return false;
        }

        wavePlan = new WaveExecutionPlan
        {
            waveNumber = directorDecision.targetWaveNumber,
            sourceBudget = directorDecision.targetWaveBudget,
            startDelay = directorDecision.nextWaveDelay,
            activeSpawnZoneCount = directorDecision.activeSpawnZoneCount,
            containsEarlyElite = directorDecision.requestEarlyElite,
            requestEnvironmentIntervention = directorDecision.requestEnvironmentIntervention,
            requestLootIntervention = directorDecision.requestLootIntervention,
            requestServiceIntervention = directorDecision.requestServiceIntervention,
            spawnCommands = new List<WaveSpawnCommand>(),
            activeZoneIndices = new List<int>()
        };

        // Пока MVP:
        // 1 бюджет = 1 враг
        int totalSpawnCount = Mathf.Max(1, directorDecision.targetWaveBudget);

        int availableZoneCount = ResolveAvailableSpawnZoneCount(runContext);
        int activeZoneCount = Mathf.Clamp(
            directorDecision.activeSpawnZoneCount,
            1,
            Mathf.Max(1, availableZoneCount)
        );

        // Случайный набор активных зон на эту волну
        wavePlan.activeZoneIndices = BuildRandomActiveZoneSet(
            availableZoneCount,
            activeZoneCount
        );

        if (wavePlan.activeZoneIndices == null || wavePlan.activeZoneIndices.Count == 0)
        {
            wavePlan = WaveExecutionPlan.CreateEmpty("Failed to build active zone set");
            return false;
        }

        // Каждая новая волна начинает с чистого состояния анти-повтора
        lastUsedSpawnZoneIndex = -1;

        // Собираем команды спавна
        for (int i = 0; i < totalSpawnCount; i++)
        {
            int spawnZoneIndex = ResolveSpawnZoneIndexFromActiveSet(wavePlan.activeZoneIndices);

            string enemyTypeId = ResolveEnemyTypeIdForWave(runContext, directorDecision.targetWaveNumber);

            WaveSpawnCommand command = new WaveSpawnCommand
            {
                enemyTypeId = enemyTypeId,
                spawnZoneIndex = spawnZoneIndex,
                spawnDelay = i * defaultSpawnInterval,
                isElite = false,
                debugNote = $"Spawn #{i + 1}"
            };

            wavePlan.spawnCommands.Add(command);
        }

        // Ранний элитник пока просто заменяет одну команду на elite type id
        if (directorDecision.requestEarlyElite &&
            convertOneSpawnToElite &&
            wavePlan.spawnCommands.Count > 0)
        {
            int eliteIndex = Mathf.Clamp(
                wavePlan.spawnCommands.Count - 1,
                0,
                wavePlan.spawnCommands.Count - 1
            );

            wavePlan.spawnCommands[eliteIndex].enemyTypeId = defaultEliteEnemyTypeId;
            wavePlan.spawnCommands[eliteIndex].isElite = true;
            wavePlan.spawnCommands[eliteIndex].debugNote = "Converted to early elite";
        }

        wavePlan.debugSummary = BuildDebugSummary(
            directorDecision,
            totalSpawnCount,
            wavePlan.activeZoneIndices
        );

        wavePlan.ValidateData();

        if (showDebugLogs)
        {
            Debug.Log(
                $"WavePlanBuilder: built RANDOM zone plan. " +
                $"Wave = {wavePlan.waveNumber}, " +
                $"Budget = {wavePlan.sourceBudget}, " +
                $"Commands = {wavePlan.TotalSpawnCommands}, " +
                $"ActiveZones = [{string.Join(", ", wavePlan.activeZoneIndices)}]"
            );
        }

        return wavePlan.IsValid();
    }

    private string ResolveEnemyTypeIdForWave(ArenaRunContext runContext, int waveNumber)
    {
        if (enemyPoolResolver == null)
            return defaultBasicEnemyTypeId;

        string resolvedEnemyTypeId = enemyPoolResolver.GetRandomEnemyTypeIdForWave(runContext, waveNumber);

        if (string.IsNullOrWhiteSpace(resolvedEnemyTypeId))
            return defaultBasicEnemyTypeId;

        return resolvedEnemyTypeId;
    }

    private List<int> BuildRandomActiveZoneSet(int availableZoneCount, int activeZoneCount)
    {
        List<int> allZoneIndices = new List<int>();

        for (int i = 0; i < availableZoneCount; i++)
        {
            allZoneIndices.Add(i);
        }

        ShuffleList(allZoneIndices);

        List<int> result = new List<int>();

        for (int i = 0; i < activeZoneCount && i < allZoneIndices.Count; i++)
        {
            result.Add(allZoneIndices[i]);
        }

        return result;
    }

    private int ResolveSpawnZoneIndexFromActiveSet(List<int> activeZoneIndices)
    {
        if (activeZoneIndices == null || activeZoneIndices.Count == 0)
            return 0;

        int resultZone;

        if (useRandomSpawnZones)
        {
            int randomLocalIndex = Random.Range(0, activeZoneIndices.Count);
            resultZone = activeZoneIndices[randomLocalIndex];

            if (avoidImmediateSameSpawnZone &&
                activeZoneIndices.Count > 1 &&
                resultZone == lastUsedSpawnZoneIndex)
            {
                int shiftedLocalIndex = (randomLocalIndex + 1) % activeZoneIndices.Count;
                resultZone = activeZoneIndices[shiftedLocalIndex];
            }
        }
        else
        {
            int startIndex = 0;

            if (lastUsedSpawnZoneIndex != -1)
            {
                int foundIndex = activeZoneIndices.IndexOf(lastUsedSpawnZoneIndex);
                if (foundIndex >= 0)
                    startIndex = (foundIndex + 1) % activeZoneIndices.Count;
            }

            resultZone = activeZoneIndices[startIndex];
        }

        lastUsedSpawnZoneIndex = resultZone;
        return resultZone;
    }

    private int ResolveAvailableSpawnZoneCount(ArenaRunContext runContext)
    {
        return Mathf.Max(1, fallbackSpawnZoneCount);
    }

    private void ShuffleList<T>(List<T> list)
    {
        if (list == null || list.Count <= 1)
            return;

        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    private string BuildDebugSummary(
        ArenaDirectorDecision directorDecision,
        int totalSpawnCount,
        List<int> activeZoneIndices)
    {
        string zonesText = activeZoneIndices != null
            ? string.Join(", ", activeZoneIndices)
            : "none";

        string summary =
            $"WavePlan built from decision. " +
            $"Wave = {directorDecision.targetWaveNumber}, " +
            $"Budget = {directorDecision.targetWaveBudget}, " +
            $"SpawnCount = {totalSpawnCount}, " +
            $"ActiveZones = [{zonesText}].";

        if (directorDecision.requestEarlyElite)
            summary += " Early elite requested.";

        if (directorDecision.requestEnvironmentIntervention)
            summary += " Environment intervention requested.";

        if (directorDecision.requestLootIntervention)
            summary += " Loot intervention requested.";

        if (directorDecision.requestServiceIntervention)
            summary += " Service intervention requested.";

        return summary;
    }
}