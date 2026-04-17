using System;
using UnityEngine;

// ArenaModeController
// Это общий управляющий слой выбора режима.
//
// Его задача:
// - хранить выбранные config-данные
// - создать ArenaRunContext
// - выбрать нужного директора
// - запросить у директора решение
// - передать решение в WavePlanBuilder
// - передать готовый план волны в ArenaWaveSpawner
//
// ВАЖНО:
// Это не мозг режима.
// Мозг режима = конкретный директор (например ClassicArenaDirector).
[DisallowMultipleComponent]
public class ArenaModeController : MonoBehaviour
{
    [Header("Выбранные данные забега")]
    [SerializeField] private ArenaConfig selectedArenaConfig;
    [SerializeField] private ArenaDifficultyProfile selectedDifficultyProfile;
    [SerializeField] private GameModeConfig selectedGameMode;
    [SerializeField] private ArenaModeRulesProfile selectedModeRulesProfile;

    [Header("Исполнители")]
    [SerializeField] private WavePlanBuilder wavePlanBuilder;
    [SerializeField] private ArenaWaveSpawner arenaWaveSpawner;

    [Header("Директоры режимов")]
    [Tooltip("Сюда подключаем ClassicArenaDirector.")]
    [SerializeField] private MonoBehaviour classicDirectorBehaviour;

    [Tooltip("Сюда потом подключим SurvivalArenaDirector.")]
    [SerializeField] private MonoBehaviour survivalDirectorBehaviour;

    [Tooltip("Сюда потом подключим ConstructorArenaDirector.")]
    [SerializeField] private MonoBehaviour constructorDirectorBehaviour;

    [Tooltip("Сюда потом подключим PvPArenaDirector.")]
    [SerializeField] private MonoBehaviour pvpDirectorBehaviour;

    [Header("Отладка")]
    [SerializeField] private bool autoTickRunTime = true;
    [SerializeField] private bool showDebugLogs = true;

    private IArenaDirector classicDirector;
    private IArenaDirector survivalDirector;
    private IArenaDirector constructorDirector;
    private IArenaDirector pvpDirector;

    private IArenaDirector activeDirector;
    private ArenaRunContext currentRunContext;

    public ArenaRunContext CurrentRunContext => currentRunContext;
    public IArenaDirector ActiveDirector => activeDirector;

    private void Awake()
    {
        CacheDirectors();
    }

    private void OnEnable()
    {
        SubscribeSpawnerEvents();
    }

    private void OnDisable()
    {
        UnsubscribeSpawnerEvents();
    }

    private void Update()
    {
        if (autoTickRunTime &&
            currentRunContext != null &&
            currentRunContext.IsRunActive &&
            !currentRunContext.IsRunFinished)
        {
            currentRunContext.TickRunTime(Time.deltaTime);
        }

        activeDirector?.Tick(Time.deltaTime);
    }

    public void BeginArenaRun()
    {
        if (currentRunContext != null &&
            currentRunContext.IsRunActive &&
            !currentRunContext.IsRunFinished)
        {
            if (showDebugLogs)
                Debug.Log("ArenaModeController: забег уже активен.");
            return;
        }

        bool prepared = PrepareRun();
        if (!prepared)
        {
            Debug.LogWarning("ArenaModeController: не удалось подготовить забег.");
            return;
        }

        RequestAndStartNextWave();
    }

    public void StopArenaRun()
    {
        if (showDebugLogs)
            Debug.Log("ArenaModeController: принудительная остановка забега.");

        arenaWaveSpawner?.StopCurrentWave();
        FinishRun();
    }

    public bool PrepareRun()
    {
        if (!ValidateSelectedConfigs())
            return false;

        if (wavePlanBuilder == null)
        {
            Debug.LogWarning("ArenaModeController: не назначен WavePlanBuilder.");
            return false;
        }

        if (arenaWaveSpawner == null)
        {
            Debug.LogWarning("ArenaModeController: не назначен ArenaWaveSpawner.");
            return false;
        }

        CacheDirectors();

        activeDirector = ResolveDirectorForSelectedMode();

        if (activeDirector == null)
        {
            Debug.LogWarning("ArenaModeController: не найден директор для выбранного режима.");
            return false;
        }

        // ВАЖНО:
        // создаём новый контекст перед стартом забега
        currentRunContext = new ArenaRunContext();

        currentRunContext.StartNewRun(
            selectedArenaConfig,
            selectedDifficultyProfile,
            selectedGameMode,
            selectedModeRulesProfile
        );

        activeDirector.Initialize(currentRunContext);

        if (showDebugLogs)
        {
            Debug.Log(
                $"ArenaModeController: подготовлен забег. " +
                $"Mode = {selectedGameMode.modeId}, " +
                $"Arena = {selectedArenaConfig.arenaId}, " +
                $"Difficulty = {selectedDifficultyProfile.id}, " +
                $"Rules = {selectedModeRulesProfile.rulesId}"
            );
        }

        return true;
    }

    private void RequestAndStartNextWave()
    {
        if (activeDirector == null)
        {
            Debug.LogWarning("ArenaModeController: активный директор отсутствует.");
            FinishRun();
            return;
        }

        bool success = activeDirector.TryBuildDecision(out ArenaDirectorDecision directorDecision);

        if (!success || directorDecision == null || !directorDecision.isValidDecision || !directorDecision.shouldStartWave)
        {
            if (showDebugLogs)
            {
                string reason = directorDecision != null ? directorDecision.debugReason : "No decision";
                Debug.Log($"ArenaModeController: директор не дал новую волну. Причина: {reason}");
            }

            FinishRun();
            return;
        }

        if (!wavePlanBuilder.TryBuildWavePlan(directorDecision, currentRunContext, out WaveExecutionPlan wavePlan))
        {
            Debug.LogWarning("ArenaModeController: WavePlanBuilder не смог собрать WaveExecutionPlan.");
            FinishRun();
            return;
        }

        if (wavePlan == null || !wavePlan.IsValid())
        {
            Debug.LogWarning("ArenaModeController: получен невалидный WaveExecutionPlan.");
            FinishRun();
            return;
        }

        currentRunContext.AdvanceToNextWave();

        bool started = arenaWaveSpawner.StartWave(wavePlan);
        if (!started)
        {
            Debug.LogWarning("ArenaModeController: ArenaWaveSpawner не смог запустить волну.");
            FinishRun();
            return;
        }

        if (showDebugLogs)
        {
            Debug.Log(
                $"ArenaModeController: запущена волна {wavePlan.waveNumber}, " +
                $"commands = {wavePlan.TotalSpawnCommands}"
            );
        }
    }

    private void SubscribeSpawnerEvents()
    {
        if (arenaWaveSpawner == null)
            return;

        arenaWaveSpawner.WaveCompleted -= HandleWaveCompleted;
        arenaWaveSpawner.EnemyKilled -= HandleEnemyKilled;

        arenaWaveSpawner.WaveCompleted += HandleWaveCompleted;
        arenaWaveSpawner.EnemyKilled += HandleEnemyKilled;
    }

    private void UnsubscribeSpawnerEvents()
    {
        if (arenaWaveSpawner == null)
            return;

        arenaWaveSpawner.WaveCompleted -= HandleWaveCompleted;
        arenaWaveSpawner.EnemyKilled -= HandleEnemyKilled;
    }

    private void HandleWaveCompleted(float waveDuration, bool wasVeryEasy)
    {
        NotifyWaveCompleted(waveDuration, wasVeryEasy);
        RequestAndStartNextWave();
    }

    private void HandleEnemyKilled()
    {
        RegisterEnemyKill();
    }

    public void NotifyWaveCompleted(float waveDuration, bool wasVeryEasy)
    {
        if (currentRunContext == null)
            return;

        currentRunContext.MarkWaveCompleted(waveDuration, wasVeryEasy);
        activeDirector?.NotifyWaveCompleted(waveDuration, wasVeryEasy);
    }

    public void RegisterEnemyKill()
    {
        if (currentRunContext == null)
            return;

        currentRunContext.RegisterEnemyKill();
        activeDirector?.NotifyEnemyKilled();
    }

    public void FinishRun()
    {
        if (currentRunContext != null && !currentRunContext.IsRunFinished)
        {
            currentRunContext.FinishRun();
        }

        activeDirector?.FinishRun();

        if (showDebugLogs)
            Debug.Log("ArenaModeController: забег завершён.");
    }

    private bool ValidateSelectedConfigs()
    {
        if (selectedArenaConfig == null)
        {
            Debug.LogWarning("ArenaModeController: не назначен ArenaConfig.");
            return false;
        }

        if (selectedDifficultyProfile == null)
        {
            Debug.LogWarning("ArenaModeController: не назначен ArenaDifficultyProfile.");
            return false;
        }

        if (selectedGameMode == null)
        {
            Debug.LogWarning("ArenaModeController: не назначен GameModeConfig.");
            return false;
        }

        if (selectedModeRulesProfile == null)
        {
            Debug.LogWarning("ArenaModeController: не назначен ArenaModeRulesProfile.");
            return false;
        }

        return true;
    }

    private void CacheDirectors()
    {
        classicDirector = CastDirector(classicDirectorBehaviour, "classic");
        survivalDirector = CastDirector(survivalDirectorBehaviour, "survival");
        constructorDirector = CastDirector(constructorDirectorBehaviour, "constructor");
        pvpDirector = CastDirector(pvpDirectorBehaviour, "pvp");
    }

    private IArenaDirector CastDirector(MonoBehaviour behaviour, string expectedModeId)
    {
        if (behaviour == null)
            return null;

        IArenaDirector director = behaviour as IArenaDirector;

        if (director == null)
        {
            Debug.LogWarning($"ArenaModeController: объект {behaviour.name} не реализует IArenaDirector.");
            return null;
        }

        if (!string.Equals(director.DirectorModeId, expectedModeId, StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogWarning(
                $"ArenaModeController: директор {behaviour.name} имеет modeId = {director.DirectorModeId}, " +
                $"а ожидался {expectedModeId}."
            );
        }

        return director;
    }

    private IArenaDirector ResolveDirectorForSelectedMode()
    {
        if (selectedGameMode == null || string.IsNullOrWhiteSpace(selectedGameMode.modeId))
            return null;

        string modeId = selectedGameMode.modeId.Trim().ToLowerInvariant();

        switch (modeId)
        {
            case "classic":
                return classicDirector;

            case "survival":
                return survivalDirector;

            case "constructor":
                return constructorDirector;

            case "pvp":
                return pvpDirector;

            default:
                Debug.LogWarning($"ArenaModeController: неизвестный modeId = {modeId}");
                return null;
        }
    }

    // ВАЖНО:
    // теперь метод принимает 4 параметра
    public void SetSelectedConfigs(
        ArenaConfig arenaConfig,
        ArenaDifficultyProfile difficultyProfile,
        GameModeConfig gameModeConfig,
        ArenaModeRulesProfile modeRulesProfile)
    {
        selectedArenaConfig = arenaConfig;
        selectedDifficultyProfile = difficultyProfile;
        selectedGameMode = gameModeConfig;
        selectedModeRulesProfile = modeRulesProfile;
    }
}