using UnityEngine;

// ArenaSceneRegistry
// Это реестр арен, которые существуют в текущей сцене.
//
// ВАЖНО:
// - он НЕ считает волны
// - он НЕ спавнит врагов
// - он НЕ является директором режима
//
// Его задача:
// - знать, какие арены есть в сцене
// - по выбранному ArenaConfig найти нужную арену
// - включить её root-объект
// - выключить остальные арены
// - передать правильные SpawnZone[] в ArenaWaveSpawner
//
// То есть это именно "связка сцены" и логики арены.
[DisallowMultipleComponent]
public class ArenaSceneRegistry : MonoBehaviour
{
    [Header("Арены в текущей сцене")]
    [SerializeField] private ArenaSceneEntry[] arenaEntries;

    [Header("Отладка")]
    [SerializeField] private bool showDebugLogs = true;

    // Попробовать активировать нужную арену по ArenaConfig
    public bool TryActivateArena(ArenaConfig selectedArenaConfig, ArenaWaveSpawner arenaWaveSpawner)
    {
        if (selectedArenaConfig == null)
        {
            Debug.LogWarning("ArenaSceneRegistry: selectedArenaConfig = null.");
            return false;
        }

        if (arenaWaveSpawner == null)
        {
            Debug.LogWarning("ArenaSceneRegistry: arenaWaveSpawner = null.");
            return false;
        }

        if (arenaEntries == null || arenaEntries.Length == 0)
        {
            Debug.LogWarning("ArenaSceneRegistry: список арен пуст.");
            return false;
        }

        ArenaSceneEntry matchedEntry = null;

        // Ищем подходящую арену
        for (int i = 0; i < arenaEntries.Length; i++)
        {
            ArenaSceneEntry entry = arenaEntries[i];

            if (entry == null || entry.arenaConfig == null)
                continue;

            bool isMatch = entry.arenaConfig == selectedArenaConfig;

            // Включаем только совпавшую арену, остальные выключаем
            if (entry.arenaRoot != null)
                entry.arenaRoot.SetActive(isMatch);

            if (isMatch)
                matchedEntry = entry;
        }

        if (matchedEntry == null)
        {
            Debug.LogWarning(
                $"ArenaSceneRegistry: не найдена сцена-арена для config {selectedArenaConfig.arenaId}."
            );
            return false;
        }

        if (matchedEntry.spawnZones == null || matchedEntry.spawnZones.Length == 0)
        {
            Debug.LogWarning(
                $"ArenaSceneRegistry: у арены {selectedArenaConfig.arenaId} нет SpawnZone."
            );
            return false;
        }

        // Передаём зоны выбранной арены в спавнер
        arenaWaveSpawner.SetSpawnZones(matchedEntry.spawnZones);

        if (showDebugLogs)
        {
            Debug.Log(
                $"ArenaSceneRegistry: activated arena {selectedArenaConfig.arenaId}. " +
                $"Zones = {matchedEntry.spawnZones.Length}"
            );
        }

        return true;
    }
}

// ArenaSceneEntry
// Это одна запись в реестре арен.
//
// Она связывает:
// - ArenaConfig
// - root-объект арены в сцене
// - зоны спавна этой арены
[System.Serializable]
public class ArenaSceneEntry
{
    [Tooltip("Какому ArenaConfig соответствует эта арена в сцене.")]
    public ArenaConfig arenaConfig;

    [Tooltip("Главный root-объект этой арены в сцене.")]
    public GameObject arenaRoot;

    [Tooltip("Зоны спавна, которые принадлежат этой арене.")]
    public SpawnZone[] spawnZones;
}
