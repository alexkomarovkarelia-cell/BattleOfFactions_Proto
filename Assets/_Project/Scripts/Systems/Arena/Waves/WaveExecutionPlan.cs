using System.Collections.Generic;
using UnityEngine;

// WaveExecutionPlan
// Это ГОТОВЫЙ план одной волны,
// который исполняет ArenaWaveSpawner.
[System.Serializable]
public class WaveExecutionPlan
{
    [Header("Общая информация о волне")]
    public int waveNumber = 1;
    public int sourceBudget = 0;
    public float startDelay = 0f;
    [Header("Активные зоны этой волны")]
    [Tooltip("Какие зоны реально разрешены для этой конкретной волны.")]
    public List<int> activeZoneIndices = new List<int>();

    [Header("Структура плана")]
    public int activeSpawnZoneCount = 1;
    public bool containsEarlyElite = false;
    public bool requestEnvironmentIntervention = false;
    public bool requestLootIntervention = false;
    public bool requestServiceIntervention = false;

    [Header("Команды спавна")]
    public List<WaveSpawnCommand> spawnCommands = new List<WaveSpawnCommand>();

    [Header("Отладка")]
    [TextArea]
    public string debugSummary = "";

    public int TotalSpawnCommands => spawnCommands != null ? spawnCommands.Count : 0;

    public bool IsValid()
    {
        return spawnCommands != null && spawnCommands.Count > 0;
    }

    public void ValidateData()
    {
        if (waveNumber < 1)
            waveNumber = 1;

        if (sourceBudget < 0)
            sourceBudget = 0;

        if (startDelay < 0f)
            startDelay = 0f;

        if (activeSpawnZoneCount < 1)
            activeSpawnZoneCount = 1;

        if (spawnCommands == null)
            spawnCommands = new List<WaveSpawnCommand>();

        if (activeZoneIndices == null)
            activeZoneIndices = new List<int>();

        for (int i = 0; i < spawnCommands.Count; i++)
        {
            if (spawnCommands[i] == null)
                continue;

            spawnCommands[i].ValidateData();
        }
    }

    public static WaveExecutionPlan CreateEmpty(string reason = "")
    {
        return new WaveExecutionPlan
        {
            waveNumber = 1,
            sourceBudget = 0,
            startDelay = 0f,
            activeSpawnZoneCount = 1,
            debugSummary = string.IsNullOrWhiteSpace(reason) ? "Empty plan" : reason,
            spawnCommands = new List<WaveSpawnCommand>(),
            activeZoneIndices = new List<int>()
        };
    }
}

// WaveSpawnCommand
// Одна команда спавна внутри волны.
[System.Serializable]
public class WaveSpawnCommand
{
    [Tooltip("ID типа врага.")]
    public string enemyTypeId = "melee_basic";

    [Tooltip("Индекс зоны спавна, а не конкретной точки.")]
    public int spawnZoneIndex = 0;

    [Tooltip("Задержка от старта волны до этого спавна.")]
    public float spawnDelay = 0f;

    [Tooltip("Элитный ли это вариант.")]
    public bool isElite = false;

    [Tooltip("Заметка для отладки.")]
    public string debugNote = "";

    public void ValidateData()
    {
        if (string.IsNullOrWhiteSpace(enemyTypeId))
            enemyTypeId = "melee_basic";

        if (spawnZoneIndex < 0)
            spawnZoneIndex = 0;

        if (spawnDelay < 0f)
            spawnDelay = 0f;
    }
}