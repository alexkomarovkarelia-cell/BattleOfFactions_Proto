using System.Collections.Generic;
using UnityEngine;

// WavePlanBuilder
// Это "зам-директор" по конкретной волне.
//
// ВАЖНО:
// - директор режима решает ОБЩЕЕ направление:
//   какой бюджет, сколько зон, нужен ли ранний элитник,
//   нужно ли вмешательство окружения и т.д.
//
// - WavePlanBuilder решает КОНКРЕТИКУ:
//   сколько будет spawn-команд,
//   в какие точки,
//   с какими задержками,
//   какие из них будут элитными.
//
// То есть:
// Director -> "что хотим получить"
// WavePlanBuilder -> "как именно собрать волну"
// Spawner -> "исполнить готовый план"
[DisallowMultipleComponent]
public class WavePlanBuilder : MonoBehaviour
{
    [Header("Общая логика планирования")]
    [SerializeField] private bool useRandomSpawnPoints = true;

    [SerializeField] private float defaultSpawnInterval = 0.5f;
    // Базовый интервал между спавнами внутри волны.

    [SerializeField] private int fallbackSpawnPointCount = 4;
    // Если пока у арены нет отдельного списка точек,
    // можем использовать запасное число.
    // Позже это уйдёт в ArenaConfig / SpawnPoint-структуру.

    [Header("Элитные правила")]
    [SerializeField] private string defaultBasicEnemyTypeId = "melee_basic";
    [SerializeField] private string defaultEliteEnemyTypeId = "melee_elite";
    [SerializeField] private bool convertOneSpawnToElite = true;

    [Header("Безопасность / анти-повтор")]
    [SerializeField] private bool avoidImmediateSameSpawnPoint = true;

    [Header("Отладка")]
    [SerializeField] private bool showDebugLogs = true;

    // Запоминаем последнюю использованную точку,
    // чтобы не долбить одну и ту же точку подряд.
    private int lastUsedSpawnPointIndex = -1;

    /// <summary>
    /// Собрать конкретный план волны из решения директора.
    /// </summary>
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

        // Создаём контейнер плана.
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
            spawnCommands = new List<WaveSpawnCommand>()
        };

        // Пока MVP-логика простая:
        // 1 бюджет = 1 базовый враг.
        //
        // Позже мы это заменим на нормальную систему стоимости врагов:
        // - дальник может стоить 2
        // - элитник 5
        // - босс 20
        //
        // Но сейчас нам нужен рабочий фундамент.
        int totalSpawnCount = Mathf.Max(1, directorDecision.targetWaveBudget);

        int availableSpawnPointCount = ResolveAvailableSpawnPointCount(runContext);
        int activeZoneCount = Mathf.Clamp(
            directorDecision.activeSpawnZoneCount,
            1,
            Mathf.Max(1, availableSpawnPointCount)
        );

        for (int i = 0; i < totalSpawnCount; i++)
        {
            int spawnPointIndex = ResolveSpawnPointIndex(availableSpawnPointCount, activeZoneCount);

            WaveSpawnCommand command = new WaveSpawnCommand
            {
                enemyTypeId = defaultBasicEnemyTypeId,
                spawnPointIndex = spawnPointIndex,
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
            int eliteIndex = Mathf.Clamp(wavePlan.spawnCommands.Count - 1, 0, wavePlan.spawnCommands.Count - 1);

            wavePlan.spawnCommands[eliteIndex].enemyTypeId = defaultEliteEnemyTypeId;
            wavePlan.spawnCommands[eliteIndex].isElite = true;
            wavePlan.spawnCommands[eliteIndex].debugNote = "Converted to early elite";
        }

        wavePlan.debugSummary = BuildDebugSummary(directorDecision, totalSpawnCount, activeZoneCount);
        wavePlan.ValidateData();

        if (showDebugLogs)
        {
            Debug.Log(
                $"WavePlanBuilder: built plan. " +
                $"Wave = {wavePlan.waveNumber}, " +
                $"Budget = {wavePlan.sourceBudget}, " +
                $"Commands = {wavePlan.TotalSpawnCommands}, " +
                $"Zones = {wavePlan.activeSpawnZoneCount}, " +
                $"EarlyElite = {wavePlan.containsEarlyElite}"
            );
        }

        return wavePlan.IsValid();
    }

    // =========================================================
    // ВНУТРЕННЯЯ ЛОГИКА
    // =========================================================

    private int ResolveAvailableSpawnPointCount(ArenaRunContext runContext)
    {
        // Пока у нас ещё нет полноценного слоя SpawnPoint-конфигов,
        // поэтому берём запасное количество.
        //
        // Позже это будет идти из ArenaConfig + SpawnPoint-компонентов на сцене.
        return Mathf.Max(1, fallbackSpawnPointCount);
    }

    private int ResolveSpawnPointIndex(int availableSpawnPointCount, int activeZoneCount)
    {
        int usableCount = Mathf.Clamp(activeZoneCount, 1, Mathf.Max(1, availableSpawnPointCount));

        // Пока MVP-подход такой:
        // используем первые usableCount точек.
        // Позже можно будет делать:
        // - реальные зоны
        // - фильтр по типам
        // - безопасную дистанцию от игрока
        // - более умный anti-repeat
        int resultIndex;

        if (useRandomSpawnPoints)
        {
            resultIndex = Random.Range(0, usableCount);

            if (avoidImmediateSameSpawnPoint &&
                usableCount > 1 &&
                resultIndex == lastUsedSpawnPointIndex)
            {
                // Очень простой anti-repeat:
                // если случайно попали в ту же точку, пробуем сдвинуть.
                resultIndex = (resultIndex + 1) % usableCount;
            }
        }
        else
        {
            resultIndex = (lastUsedSpawnPointIndex + 1 + usableCount) % usableCount;
        }

        lastUsedSpawnPointIndex = resultIndex;
        return resultIndex;
    }

    private string BuildDebugSummary(
        ArenaDirectorDecision directorDecision,
        int totalSpawnCount,
        int activeZoneCount)
    {
        string summary =
            $"WavePlan built from decision. " +
            $"Wave = {directorDecision.targetWaveNumber}, " +
            $"Budget = {directorDecision.targetWaveBudget}, " +
            $"SpawnCount = {totalSpawnCount}, " +
            $"Zones = {activeZoneCount}.";

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
