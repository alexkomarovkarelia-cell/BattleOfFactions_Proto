using System.Collections.Generic;
using UnityEngine;

// WavePlanBuilder
// Это планировщик конкретной волны.
//
// ВАЖНО:
// Теперь он не просто берёт "первые N зон",
// а на КАЖДУЮ волну случайно выбирает набор активных зон.
//
// Пример:
// если доступно 4 зоны, а активных нужно 2,
// то волна может выбрать:
// - 0 и 3
// - 1 и 2
// - 0 и 2
// и т.д.
//
// После этого команды спавна создаются только внутри выбранного набора.
[DisallowMultipleComponent]
public class WavePlanBuilder : MonoBehaviour
{
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

    // Запоминаем последнюю зону спавна,
    // чтобы внутри волны не бить подряд в одну и ту же.
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

        // =====================================================
        // НОВОЕ:
        // Выбираем активные зоны СЛУЧАЙНО ДЛЯ ЭТОЙ ВОЛНЫ
        // =====================================================
        wavePlan.activeZoneIndices = BuildRandomActiveZoneSet(
            availableZoneCount,
            activeZoneCount
        );

        // На всякий случай: если что-то пошло не так
        if (wavePlan.activeZoneIndices == null || wavePlan.activeZoneIndices.Count == 0)
        {
            wavePlan = WaveExecutionPlan.CreateEmpty("Failed to build active zone set");
            return false;
        }

        // Сбрасываем lastUsed, чтобы каждая новая волна начиналась чисто.
        lastUsedSpawnZoneIndex = -1;

        // Создаём конкретные команды спавна.
        for (int i = 0; i < totalSpawnCount; i++)
        {
            int spawnZoneIndex = ResolveSpawnZoneIndexFromActiveSet(wavePlan.activeZoneIndices);

            WaveSpawnCommand command = new WaveSpawnCommand
            {
                enemyTypeId = defaultBasicEnemyTypeId,
                spawnZoneIndex = spawnZoneIndex,
                spawnDelay = i * defaultSpawnInterval,
                isElite = false,
                debugNote = $"Base spawn #{i + 1}"
            };

            wavePlan.spawnCommands.Add(command);
        }

        // Если директор попросил раннего элитника —
        // заменяем одну команду на элитную.
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

    // =====================================================
    // Собираем СЛУЧАЙНЫЙ набор активных зон на волну
    // =====================================================
    private List<int> BuildRandomActiveZoneSet(int availableZoneCount, int activeZoneCount)
    {
        List<int> allZoneIndices = new List<int>();

        for (int i = 0; i < availableZoneCount; i++)
        {
            allZoneIndices.Add(i);
        }

        // Перемешиваем список
        ShuffleList(allZoneIndices);

        // Берём только нужное количество активных зон
        List<int> result = new List<int>();

        for (int i = 0; i < activeZoneCount && i < allZoneIndices.Count; i++)
        {
            result.Add(allZoneIndices[i]);
        }

        return result;
    }

    // =====================================================
    // Выбираем одну зону из уже активного набора этой волны
    // =====================================================
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
                // Очень простая защита от повтора:
                // сдвигаемся на следующую зону в списке.
                int shiftedLocalIndex = (randomLocalIndex + 1) % activeZoneIndices.Count;
                resultZone = activeZoneIndices[shiftedLocalIndex];
            }
        }
        else
        {
            // Если случайность выключена —
            // идём по активным зонам по кругу.
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
        // Пока MVP:
        // используем запасное число зон.
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