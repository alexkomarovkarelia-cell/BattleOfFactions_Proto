using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ArenaWaveSpawner
// Новый спавнер новой системы арены.
//
// Теперь он спавнит не из точек,
// а из ЗОН.
[DisallowMultipleComponent]
public class ArenaWaveSpawner : MonoBehaviour
{
    [Header("Зоны спавна")]
    [SerializeField] private SpawnZone[] spawnZones;

    [Header("Таблица доступных врагов")]
    [SerializeField] private List<ArenaEnemyPrefabEntry> enemyPrefabEntries = new List<ArenaEnemyPrefabEntry>();

    [Header("Ограничение по живым врагам")]
    [SerializeField] private int maxAliveEnemiesAtOnce = 4;

    [Header("Отладка")]
    [SerializeField] private bool showDebugLogs = true;

    [Header("Безопасная дистанция от игрока")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float minSpawnDistanceFromPlayer = 10f;
    [SerializeField] private int maxSpawnPositionAttempts = 8;

    public event Action EnemyKilled;
    public event Action<float, bool> WaveCompleted;

    public bool IsWaveRunning => isWaveRunning;

    private Coroutine activeWaveRoutine;
    private bool isWaveRunning = false;
    private bool stopRequested = false;

    private int executedSpawnCommands = 0;
    private int aliveEnemies = 0;
    private float waveStartTime = 0f;

    private WaveExecutionPlan currentPlan;
    private readonly List<TrackedArenaEnemy> trackedEnemies = new List<TrackedArenaEnemy>();

    public bool StartWave(WaveExecutionPlan plan)
    {
        if (plan == null)
        {
            Debug.LogWarning("ArenaWaveSpawner: StartWave получил null plan.");
            return false;
        }

        if (!plan.IsValid())
        {
            Debug.LogWarning("ArenaWaveSpawner: StartWave получил невалидный WaveExecutionPlan.");
            return false;
        }

        if (spawnZones == null || spawnZones.Length == 0)
        {
            Debug.LogError("ArenaWaveSpawner: не назначены spawnZones.");
            return false;
        }

        if (isWaveRunning)
        {
            Debug.LogWarning("ArenaWaveSpawner: волна уже идёт.");
            return false;
        }

        StopCurrentWaveInternal(clearStateOnly: true);

        currentPlan = plan;
        stopRequested = false;
        activeWaveRoutine = StartCoroutine(RunWaveRoutine(plan));

        return true;
    }

    public void StopCurrentWave()
    {
        StopCurrentWaveInternal(clearStateOnly: false);
    }

    private IEnumerator RunWaveRoutine(WaveExecutionPlan plan)
    {
        isWaveRunning = true;
        executedSpawnCommands = 0;
        aliveEnemies = 0;
        trackedEnemies.Clear();

        if (showDebugLogs)
        {
            Debug.Log(
                $"ArenaWaveSpawner: старт исполнения zone-based плана. " +
                $"Wave = {plan.waveNumber}, Commands = {plan.TotalSpawnCommands}"
            );
        }

        if (plan.startDelay > 0f)
        {
            float delayLeft = plan.startDelay;

            while (delayLeft > 0f)
            {
                if (stopRequested)
                {
                    StopCurrentWaveInternal(clearStateOnly: false);
                    yield break;
                }

                delayLeft -= Time.deltaTime;
                yield return null;
            }
        }

        waveStartTime = Time.time;

        List<WaveSpawnCommand> sortedCommands = new List<WaveSpawnCommand>(plan.spawnCommands);
        sortedCommands.Sort((a, b) => a.spawnDelay.CompareTo(b.spawnDelay));

        float waveElapsed = 0f;
        int nextCommandIndex = 0;

        while (true)
        {
            if (stopRequested)
            {
                StopCurrentWaveInternal(clearStateOnly: false);
                yield break;
            }

            waveElapsed = Time.time - waveStartTime;

            RefreshTrackedEnemies();

            while (nextCommandIndex < sortedCommands.Count)
            {
                WaveSpawnCommand command = sortedCommands[nextCommandIndex];

                if (command.spawnDelay > waveElapsed)
                    break;

                if (aliveEnemies >= maxAliveEnemiesAtOnce)
                    break;

                ExecuteSpawnCommand(command);
                executedSpawnCommands++;
                nextCommandIndex++;
            }

            bool allCommandsExecuted = nextCommandIndex >= sortedCommands.Count;
            bool allEnemiesDead = aliveEnemies <= 0;

            if (allCommandsExecuted && allEnemiesDead)
            {
                float waveDuration = Time.time - waveStartTime;
                bool wasVeryEasy = CalculateVeryEasyFlag(plan, waveDuration);

                if (showDebugLogs)
                {
                    Debug.Log(
                        $"ArenaWaveSpawner: волна завершена. " +
                        $"Wave = {plan.waveNumber}, Duration = {waveDuration:F2}, VeryEasy = {wasVeryEasy}"
                    );
                }

                isWaveRunning = false;
                activeWaveRoutine = null;
                currentPlan = null;

                WaveCompleted?.Invoke(waveDuration, wasVeryEasy);
                yield break;
            }

            yield return null;
        }
    }

    private Vector3 ResolveSpawnPosition(SpawnZone zone)
    {
        if (zone == null)
            return transform.position;

        // Если игрок не назначен — работаем как раньше.
        if (playerTransform == null)
            return zone.GetRandomPointInside();

        Vector3 bestPosition = zone.GetRandomPointInside();
        float bestDistance = Vector3.Distance(bestPosition, playerTransform.position);

        for (int i = 0; i < maxSpawnPositionAttempts; i++)
        {
            Vector3 candidate = zone.GetRandomPointInside();
            float distanceToPlayer = Vector3.Distance(candidate, playerTransform.position);

            // Если нашли точку на безопасной дистанции — сразу берём её.
            if (distanceToPlayer >= minSpawnDistanceFromPlayer)
                return candidate;

            // Иначе просто запоминаем лучшую из плохих,
            // чтобы в крайнем случае взять самую далёкую.
            if (distanceToPlayer > bestDistance)
            {
                bestDistance = distanceToPlayer;
                bestPosition = candidate;
            }
        }

        // Если идеальную не нашли — берём самую далёкую найденную.
        return bestPosition;
    }

    private void ExecuteSpawnCommand(WaveSpawnCommand command)
    {
        if (command == null)
        {
            Debug.LogWarning("ArenaWaveSpawner: команда спавна = null.");
            return;
        }

        ArenaEnemyPrefabEntry prefabEntry = FindEnemyPrefabEntry(command.enemyTypeId);

        if (prefabEntry == null || prefabEntry.enemyPrefab == null)
        {
            Debug.LogWarning(
                $"ArenaWaveSpawner: не найден prefab для enemyTypeId = {command.enemyTypeId}"
            );
            return;
        }

        int clampedZoneIndex = Mathf.Clamp(command.spawnZoneIndex, 0, spawnZones.Length - 1);
        SpawnZone zone = spawnZones[clampedZoneIndex];

        if (zone == null)
        {
            Debug.LogWarning($"ArenaWaveSpawner: зона спавна с индексом {clampedZoneIndex} = null.");
            return;
        }

        Vector3 spawnPosition = ResolveSpawnPosition(zone);

        GameObject spawnedObject = Instantiate(
            prefabEntry.enemyPrefab,
            spawnPosition,
            Quaternion.identity
        );

        if (command.isElite)
            spawnedObject.name += "_Elite";

        TrackedArenaEnemy trackedEnemy = new TrackedArenaEnemy
        {
            instanceId = spawnedObject.GetInstanceID(),
            gameObject = spawnedObject,
            enemyTypeId = command.enemyTypeId,
            isElite = command.isElite
        };

        trackedEnemies.Add(trackedEnemy);
        aliveEnemies++;

        if (showDebugLogs)
        {
            Debug.Log(
                $"ArenaWaveSpawner: spawn выполнен. " +
                $"EnemyType = {command.enemyTypeId}, " +
                $"Zone = {clampedZoneIndex}, " +
                $"Elite = {command.isElite}, " +
                $"Alive = {aliveEnemies}"
            );
        }
    }

    private void RefreshTrackedEnemies()
    {
        if (trackedEnemies.Count == 0)
            return;

        for (int i = trackedEnemies.Count - 1; i >= 0; i--)
        {
            TrackedArenaEnemy trackedEnemy = trackedEnemies[i];

            if (trackedEnemy == null || trackedEnemy.gameObject == null)
            {
                trackedEnemies.RemoveAt(i);

                aliveEnemies = Mathf.Max(0, aliveEnemies - 1);
                EnemyKilled?.Invoke();

                if (showDebugLogs)
                    Debug.Log($"ArenaWaveSpawner: один враг убит/удалён. Alive = {aliveEnemies}");
            }
        }
    }

    private ArenaEnemyPrefabEntry FindEnemyPrefabEntry(string enemyTypeId)
    {
        if (string.IsNullOrWhiteSpace(enemyTypeId))
            return null;

        for (int i = 0; i < enemyPrefabEntries.Count; i++)
        {
            ArenaEnemyPrefabEntry entry = enemyPrefabEntries[i];

            if (entry == null)
                continue;

            if (string.Equals(entry.enemyTypeId, enemyTypeId, StringComparison.OrdinalIgnoreCase))
                return entry;
        }

        return null;
    }

    private bool CalculateVeryEasyFlag(WaveExecutionPlan plan, float waveDuration)
    {
        if (plan == null)
            return false;

        int effectiveEnemyCount = Mathf.Max(1, plan.TotalSpawnCommands);
        float averageSecondsPerEnemy = waveDuration / effectiveEnemyCount;

        return averageSecondsPerEnemy <= 1.25f;
    }

    private void StopCurrentWaveInternal(bool clearStateOnly)
    {
        stopRequested = true;
        isWaveRunning = false;

        if (activeWaveRoutine != null)
        {
            StopCoroutine(activeWaveRoutine);
            activeWaveRoutine = null;
        }

        currentPlan = null;
        executedSpawnCommands = 0;
        aliveEnemies = 0;
        trackedEnemies.Clear();

        if (!clearStateOnly && showDebugLogs)
            Debug.Log("ArenaWaveSpawner: текущая волна принудительно остановлена.");
    }
}

[System.Serializable]
public class ArenaEnemyPrefabEntry
{
    public string enemyTypeId = "melee_basic";
    public GameObject enemyPrefab;
}

[System.Serializable]
public class TrackedArenaEnemy
{
    public int instanceId;
    public GameObject gameObject;
    public string enemyTypeId;
    public bool isElite;
}