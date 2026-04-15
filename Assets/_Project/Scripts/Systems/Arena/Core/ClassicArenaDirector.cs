using UnityEngine;

// ClassicArenaDirector
// Это первый реальный "мозг режима" для классической арены.
//
// ВАЖНО:
// - это НЕ спавнер
// - это НЕ менеджер всего проекта
// - это директор КОНКРЕТНОГО режима
//
// Его задача:
// 1) получить контекст текущего забега
// 2) проанализировать текущее состояние
// 3) собрать решение для следующей волны
// 4) отдать решение наружу через ArenaDirectorDecision
//
// Позже часть логики отсюда мы спокойно вынесем отдельно:
// - расчёт бюджета
// - анализ прохождения
// - правила ранних элитников
// - условия окружения
//
// Но сначала нам нужен первый рабочий director.
[DisallowMultipleComponent]
public class ClassicArenaDirector : MonoBehaviour, IArenaDirector
{
    [Header("ID режима")]
    [SerializeField] private string directorModeId = "classic";

    [Header("Базовые бюджеты по сложности")]
    [SerializeField] private int easyStartBudget = 8;
    [SerializeField] private int normalStartBudget = 10;
    [SerializeField] private int hardStartBudget = 12;

    [Header("Рост бюджета по сложности")]
    [SerializeField] private int easyBudgetGrowthPerWave = 2;
    [SerializeField] private int normalBudgetGrowthPerWave = 3;
    [SerializeField] private int hardBudgetGrowthPerWave = 4;

    [Header("Правила активных зон спавна")]
    [SerializeField] private int wavesPerExtraSpawnZone = 5;
    // Каждые N волн можно открывать ещё одну активную зону спавна.

    [Header("Правила раннего давления")]
    [SerializeField] private int minimumWaveForEarlyElite = 5;
    [SerializeField] private int minimumWaveForEnvironmentIntervention = 7;

    [Header("Отладка")]
    [SerializeField] private bool showDebugLogs = true;

    // Текущий контекст забега.
    private ArenaRunContext runContext;

    // Этот директор обслуживает только classic.
    public string DirectorModeId => directorModeId;

    // =========================================================
    // ИНИЦИАЛИЗАЦИЯ
    // =========================================================

    public void Initialize(ArenaRunContext runContext)
    {
        this.runContext = runContext;

        if (showDebugLogs && this.runContext != null)
        {
            Debug.Log(
                $"ClassicArenaDirector: initialized. " +
                $"Arena = {this.runContext.CurrentArena?.arenaId}, " +
                $"Difficulty = {this.runContext.CurrentDifficultyProfile?.id}, " +
                $"Mode = {this.runContext.CurrentGameMode?.modeId}"
            );
        }
    }

    // =========================================================
    // ГЛАВНЫЙ МЕТОД: СОБРАТЬ РЕШЕНИЕ
    // =========================================================

    public bool TryBuildDecision(out ArenaDirectorDecision decision)
    {
        decision = null;

        // Защита от пустого контекста.
        if (runContext == null)
        {
            decision = ArenaDirectorDecision.CreateInvalid("RunContext is null");
            return false;
        }

        // Если забег уже завершён — новую волну собирать нельзя.
        if (runContext.IsRunFinished)
        {
            decision = ArenaDirectorDecision.CreateInvalid("Run is already finished");
            return false;
        }

        // Если активного режима нет — тоже нельзя.
        if (!runContext.IsRunActive)
        {
            decision = ArenaDirectorDecision.CreateInvalid("Run is not active");
            return false;
        }

        // Защита: этот директор должен работать только с classic-режимом.
        if (runContext.CurrentGameMode == null || runContext.CurrentGameMode.modeId != "classic")
        {
            decision = ArenaDirectorDecision.CreateInvalid("ClassicArenaDirector received non-classic mode");
            return false;
        }

        int nextWaveNumber = runContext.CurrentWave + 1;
        int maxWaveCount = ResolveMaxWaveCount();

        // Если волны уже закончились — дальше не идём.
        if (maxWaveCount > 0 && nextWaveNumber > maxWaveCount)
        {
            decision = ArenaDirectorDecision.CreateInvalid("Classic mode completed: wave limit reached");
            return false;
        }

        decision = new ArenaDirectorDecision();
        decision.targetWaveNumber = nextWaveNumber;
        decision.targetWaveBudget = CalculateWaveBudget(nextWaveNumber);
        decision.activeSpawnZoneCount = CalculateActiveSpawnZoneCount(nextWaveNumber);
        decision.nextWaveDelay = CalculateNextWaveDelay();

        // Логика давления:
        // если прошлую волну прошли слишком легко,
        // директор может раньше включить дополнительные меры.
        bool lastWaveWasVeryEasy = runContext.LastWaveWasVeryEasy;

        if (lastWaveWasVeryEasy && nextWaveNumber >= minimumWaveForEarlyElite)
        {
            decision.requestEarlyElite = true;
        }

        if (lastWaveWasVeryEasy &&
            nextWaveNumber >= minimumWaveForEnvironmentIntervention &&
            IsEnvironmentInterventionAllowed())
        {
            decision.requestEnvironmentIntervention = true;
        }

        decision.requestLootIntervention = false;
        decision.requestServiceIntervention = false;

        decision.debugReason = BuildDebugReason(decision, lastWaveWasVeryEasy);
        decision.ValidateData();

        if (showDebugLogs)
        {
            Debug.Log(
                $"ClassicArenaDirector: built decision. " +
                $"Wave = {decision.targetWaveNumber}, " +
                $"Budget = {decision.targetWaveBudget}, " +
                $"SpawnZones = {decision.activeSpawnZoneCount}, " +
                $"EarlyElite = {decision.requestEarlyElite}, " +
                $"Env = {decision.requestEnvironmentIntervention}"
            );
        }

        return true;
    }

    // =========================================================
    // СОБЫТИЯ ОТ КОНТРОЛЛЕРА
    // =========================================================

    public void NotifyWaveCompleted(float waveDuration, bool wasVeryEasy)
    {
        if (showDebugLogs)
        {
            Debug.Log(
                $"ClassicArenaDirector: wave completed. " +
                $"Duration = {waveDuration}, VeryEasy = {wasVeryEasy}"
            );
        }
    }

    public void NotifyEnemyKilled()
    {
        // Пока отдельной логики не делаем.
        // Позже сюда можно добавить:
        // - реакцию режима на темп убийств
        // - быстрый рост давления
        // - статистику по волне
    }

    public void Tick(float deltaTime)
    {
        // Пока специальной логики по Tick не делаем.
        // Но интерфейс уже поддерживает это на будущее.
    }

    public void FinishRun()
    {
        if (showDebugLogs)
            Debug.Log("ClassicArenaDirector: run finished.");
    }

    // =========================================================
    // ВНУТРЕННЯЯ ЛОГИКА
    // =========================================================

    private int ResolveMaxWaveCount()
    {
        // Если профиль сложности явно переопределяет число волн —
        // используем его.
        if (runContext.CurrentDifficultyProfile != null &&
            runContext.CurrentDifficultyProfile.overrideTotalWaves)
        {
            return Mathf.Max(0, runContext.CurrentDifficultyProfile.totalWaves);
        }

        // Иначе берём ограничение режима.
        if (runContext.CurrentGameMode != null)
        {
            return Mathf.Max(0, runContext.CurrentGameMode.maxWaves);
        }

        return 0;
    }

    private int CalculateWaveBudget(int waveNumber)
    {
        int startBudget = GetStartBudgetForDifficulty();
        int growthPerWave = GetBudgetGrowthForDifficulty();

        // Рост идёт с первой волны аккуратно:
        // волна 1 = базовый бюджет
        // волна 2 = базовый + growth
        // волна 3 = базовый + growth * 2
        int budget = startBudget + Mathf.Max(0, waveNumber - 1) * growthPerWave;

        // Если режим запрещает рост бюджета —
        // оставляем базовое значение.
        if (runContext.CurrentGameMode != null && !runContext.CurrentGameMode.useBudgetGrowth)
        {
            budget = startBudget;
        }

        return Mathf.Max(0, budget);
    }

    private int CalculateActiveSpawnZoneCount(int waveNumber)
    {
        if (runContext.CurrentArena == null)
            return 1;

        int startZones = runContext.CurrentArena.startActiveSpawnZones;
        int maxZones = runContext.CurrentArena.maxActiveSpawnZones;

        if (wavesPerExtraSpawnZone <= 0)
            return Mathf.Clamp(startZones, 1, maxZones);

        int extraZones = Mathf.Max(0, (waveNumber - 1) / wavesPerExtraSpawnZone);
        int totalZones = startZones + extraZones;

        return Mathf.Clamp(totalZones, 1, maxZones);
    }

    private float CalculateNextWaveDelay()
    {
        float arenaDelay = 0f;
        float modeDelay = 0f;

        if (runContext.CurrentArena != null)
            arenaDelay = runContext.CurrentArena.baseTimeBetweenWaves;

        if (runContext.CurrentGameMode != null)
            modeDelay = runContext.CurrentGameMode.minTimeBetweenWaves;

        // Берём большее значение, чтобы не запускать волну слишком рано.
        return Mathf.Max(arenaDelay, modeDelay);
    }

    private bool IsEnvironmentInterventionAllowed()
    {
        bool arenaAllows = runContext.CurrentArena != null &&
                           runContext.CurrentArena.allowEnvironmentInterventions;

        bool modeAllows = runContext.CurrentGameMode != null &&
                          runContext.CurrentGameMode.allowEnvironmentInterventions;

        return arenaAllows && modeAllows;
    }

    private int GetStartBudgetForDifficulty()
    {
        if (runContext.CurrentDifficultyProfile == null)
            return normalStartBudget;

        switch (runContext.CurrentDifficultyProfile.id)
        {
            case ArenaDifficultyId.Easy:
                return easyStartBudget;

            case ArenaDifficultyId.Hard:
                return hardStartBudget;

            case ArenaDifficultyId.Normal:
            default:
                return normalStartBudget;
        }
    }

    private int GetBudgetGrowthForDifficulty()
    {
        if (runContext.CurrentDifficultyProfile == null)
            return normalBudgetGrowthPerWave;

        switch (runContext.CurrentDifficultyProfile.id)
        {
            case ArenaDifficultyId.Easy:
                return easyBudgetGrowthPerWave;

            case ArenaDifficultyId.Hard:
                return hardBudgetGrowthPerWave;

            case ArenaDifficultyId.Normal:
            default:
                return normalBudgetGrowthPerWave;
        }
    }

    private string BuildDebugReason(ArenaDirectorDecision decision, bool lastWaveWasVeryEasy)
    {
        string reason =
            $"Classic decision: wave {decision.targetWaveNumber}, budget {decision.targetWaveBudget}, zones {decision.activeSpawnZoneCount}.";

        if (lastWaveWasVeryEasy)
            reason += " Previous wave was very easy.";

        if (decision.requestEarlyElite)
            reason += " Requested early elite.";

        if (decision.requestEnvironmentIntervention)
            reason += " Requested environment intervention.";

        return reason;
    }
}