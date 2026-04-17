using UnityEngine;

// SurvivalArenaDirector
// Это мозг режима Выживание.
//
// ВАЖНО:
// - это НЕ спавнер
// - это НЕ планировщик волны
// - это НЕ общий менеджер всех режимов
//
// Его задача:
// - работать только с modeId = "survival"
// - считать следующую волну
// - не иметь жёсткого лимита волн
// - постепенно усиливать давление
// - позже уметь подключать элитников, окружение и другие вмешательства
[DisallowMultipleComponent]
public class SurvivalArenaDirector : MonoBehaviour, IArenaDirector
{
    [Header("ID режима")]
    [SerializeField] private string directorModeId = "survival";

    [Header("Базовые бюджеты после фазы разогрева")]
    [SerializeField] private int easyStartBudget = 2;
    [SerializeField] private int normalStartBudget = 3;
    [SerializeField] private int hardStartBudget = 4;

    [Header("Рост бюджета после разогрева")]
    [SerializeField] private int easyBudgetGrowthPerWave = 1;
    [SerializeField] private int normalBudgetGrowthPerWave = 2;
    [SerializeField] private int hardBudgetGrowthPerWave = 3;

    [Header("Открытие новых зон")]
    [SerializeField] private int wavesPerExtraSpawnZone = 3;

    [Header("Давление режима")]
    [SerializeField] private int minimumWaveForEarlyElite = 6;
    [SerializeField] private int minimumWaveForEnvironmentIntervention = 8;

    [Header("Отладка")]
    [SerializeField] private bool showDebugLogs = true;

    private ArenaRunContext runContext;

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
                $"SurvivalArenaDirector: initialized. " +
                $"Arena = {this.runContext.CurrentArena?.arenaId}, " +
                $"Difficulty = {this.runContext.CurrentDifficultyProfile?.id}, " +
                $"Mode = {this.runContext.CurrentGameMode?.modeId}, " +
                $"Rules = {this.runContext.CurrentModeRulesProfile?.rulesId}"
            );
        }
    }

    // =========================================================
    // ГЛАВНЫЙ МЕТОД
    // =========================================================

    public bool TryBuildDecision(out ArenaDirectorDecision decision)
    {
        decision = null;

        if (runContext == null)
        {
            decision = ArenaDirectorDecision.CreateInvalid("RunContext is null");
            return false;
        }

        if (runContext.IsRunFinished)
        {
            decision = ArenaDirectorDecision.CreateInvalid("Run is already finished");
            return false;
        }

        if (!runContext.IsRunActive)
        {
            decision = ArenaDirectorDecision.CreateInvalid("Run is not active");
            return false;
        }

        if (runContext.CurrentGameMode == null || runContext.CurrentGameMode.modeId != "survival")
        {
            decision = ArenaDirectorDecision.CreateInvalid("SurvivalArenaDirector received non-survival mode");
            return false;
        }

        int nextWaveNumber = runContext.CurrentWave + 1;

        decision = new ArenaDirectorDecision();
        decision.targetWaveNumber = nextWaveNumber;
        decision.targetWaveBudget = CalculateWaveBudget(nextWaveNumber);
        decision.activeSpawnZoneCount = CalculateActiveSpawnZoneCount(nextWaveNumber);
        decision.nextWaveDelay = CalculateNextWaveDelay();

        bool lastWaveWasVeryEasy = runContext.LastWaveWasVeryEasy;

        // В выживании директор может раньше давить, если игрок идёт слишком легко.
        if (lastWaveWasVeryEasy &&
            nextWaveNumber >= minimumWaveForEarlyElite &&
            IsEarlyElitePressureAllowed())
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
                $"SurvivalArenaDirector: built decision. " +
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
    // СОБЫТИЯ
    // =========================================================

    public void NotifyWaveCompleted(float waveDuration, bool wasVeryEasy)
    {
        if (showDebugLogs)
        {
            Debug.Log(
                $"SurvivalArenaDirector: wave completed. " +
                $"Duration = {waveDuration:F2}, VeryEasy = {wasVeryEasy}"
            );
        }
    }

    public void NotifyEnemyKilled()
    {
        // Пока отдельной логики не делаем.
        // Позже сюда можно будет добавить реакцию на темп убийств.
    }

    public void Tick(float deltaTime)
    {
        // Пока без отдельной логики.
        // Оставляем на будущее.
    }

    public void FinishRun()
    {
        if (showDebugLogs)
            Debug.Log("SurvivalArenaDirector: run finished.");
    }

    // =========================================================
    // ВНУТРЕННЯЯ ЛОГИКА
    // =========================================================

    private int CalculateWaveBudget(int waveNumber)
    {
        ArenaModeRulesProfile rules = runContext.CurrentModeRulesProfile;

        // 1. Если включён разогрев — первые волны считаем по отдельным правилам.
        if (rules != null &&
            rules.useWarmupStart &&
            waveNumber <= rules.warmupWaveCount)
        {
            int warmupBudget = rules.warmupStartBudget +
                               Mathf.Max(0, waveNumber - 1) * rules.warmupBudgetGrowthPerWave;

            return Mathf.Max(1, warmupBudget);
        }

        // 2. После разогрева переходим к обычному росту Survival.
        int startBudget = GetStartBudgetForDifficulty();
        int growthPerWave = GetBudgetGrowthForDifficulty();

        int effectiveWaveIndex = waveNumber - 1;

        if (rules != null && rules.useWarmupStart)
        {
            effectiveWaveIndex = Mathf.Max(0, waveNumber - rules.warmupWaveCount - 1);
        }

        int budget = startBudget + effectiveWaveIndex * growthPerWave;

        if (runContext.CurrentGameMode != null && !runContext.CurrentGameMode.useBudgetGrowth)
        {
            budget = startBudget;
        }

        return Mathf.Max(1, budget);
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

        return Mathf.Max(arenaDelay, modeDelay);
    }

    private bool IsEnvironmentInterventionAllowed()
    {
        bool arenaAllows = runContext.CurrentArena != null &&
                           runContext.CurrentArena.allowEnvironmentInterventions;

        bool modeAllows = runContext.CurrentGameMode != null &&
                          runContext.CurrentGameMode.allowEnvironmentInterventions;

        bool rulesAllow = runContext.CurrentModeRulesProfile != null &&
                          runContext.CurrentModeRulesProfile.allowEnvironmentPressure;

        return arenaAllows && modeAllows && rulesAllow;
    }

    private bool IsEarlyElitePressureAllowed()
    {
        bool rulesAllow = runContext.CurrentModeRulesProfile != null &&
                          runContext.CurrentModeRulesProfile.allowEarlyElitePressure;

        return rulesAllow;
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
            $"Survival decision: wave {decision.targetWaveNumber}, budget {decision.targetWaveBudget}, zones {decision.activeSpawnZoneCount}.";

        if (lastWaveWasVeryEasy)
            reason += " Previous wave was very easy.";

        if (decision.requestEarlyElite)
            reason += " Requested early elite.";

        if (decision.requestEnvironmentIntervention)
            reason += " Requested environment intervention.";

        return reason;
    }
}