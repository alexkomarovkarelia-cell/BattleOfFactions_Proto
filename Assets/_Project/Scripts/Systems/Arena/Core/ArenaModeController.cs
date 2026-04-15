using System;
using UnityEngine;

// ArenaModeController
// Это общий управляющий слой выбора режима.
//
// ВАЖНО:
// Это НЕ директор режима.
// Это "роутер" / "контроллер режимов".
//
// Его задача:
// - хранить выбранные config-данные
// - создать ArenaRunContext
// - выбрать нужного директора по режиму
// - передать ему контекст
// - потом обращаться к директору единым способом
//
// То есть:
// ArenaModeController -> выбирает мозг режима
// IArenaDirector      -> уже думает в рамках своего режима
[DisallowMultipleComponent]
public class ArenaModeController : MonoBehaviour
{
    [Header("Выбранные данные забега")]
    [SerializeField] private ArenaConfig selectedArenaConfig;
    [SerializeField] private ArenaDifficultyProfile selectedDifficultyProfile;
    [SerializeField] private GameModeConfig selectedGameMode;

    [Header("Директоры режимов")]
    [Tooltip("Сюда потом подключим ClassicArenaDirector.")]
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

    // Кэш интерфейсов директоров.
    private IArenaDirector classicDirector;
    private IArenaDirector survivalDirector;
    private IArenaDirector constructorDirector;
    private IArenaDirector pvpDirector;

    // Активный директор текущего режима.
    private IArenaDirector activeDirector;

    // Текущий контекст забега.
    private ArenaRunContext currentRunContext;

    // Публичный доступ только для чтения.
    public ArenaRunContext CurrentRunContext => currentRunContext;
    public IArenaDirector ActiveDirector => activeDirector;

    private void Awake()
    {
        CacheDirectors();
    }

    private void Update()
    {
        // Автоматически обновляем время забега,
        // если забег уже активен.
        if (autoTickRunTime &&
            currentRunContext != null &&
            currentRunContext.IsRunActive &&
            !currentRunContext.IsRunFinished)
        {
            currentRunContext.TickRunTime(Time.deltaTime);
        }

        // Даём директору режимный Tick,
        // если он уже активен.
        activeDirector?.Tick(Time.deltaTime);
    }

    // =========================================================
    // ПОДГОТОВКА И ЗАПУСК ЗАБЕГА
    // =========================================================

    // Подготовить новый забег:
    // - проверить configs
    // - выбрать нужного директора
    // - создать и инициализировать context
    public bool PrepareRun()
    {
        if (!ValidateSelectedConfigs())
            return false;

        CacheDirectors();

        activeDirector = ResolveDirectorForSelectedMode();

        if (activeDirector == null)
        {
            Debug.LogWarning("ArenaModeController: не найден директор для выбранного режима.");
            return false;
        }

        currentRunContext = new ArenaRunContext();
        currentRunContext.StartNewRun(
            selectedArenaConfig,
            selectedDifficultyProfile,
            selectedGameMode
        );

        activeDirector.Initialize(currentRunContext);

        if (showDebugLogs)
        {
            Debug.Log(
                $"ArenaModeController: подготовлен забег. " +
                $"Mode = {selectedGameMode.modeId}, " +
                $"Arena = {selectedArenaConfig.arenaId}, " +
                $"Difficulty = {selectedDifficultyProfile.id}"
            );
        }

        return true;
    }

    // Попросить у текущего директора решение.
    // Пока это просто каркасный вызов.
    public bool TryBuildNextDecision(out ArenaDirectorDecision decision)
    {
        decision = null;

        if (activeDirector == null)
        {
            Debug.LogWarning("ArenaModeController: активный директор не назначен.");
            return false;
        }

        bool success = activeDirector.TryBuildDecision(out decision);

        if (success && decision != null)
        {
            decision.ValidateData();

            if (showDebugLogs)
            {
                Debug.Log(
                    $"ArenaModeController: директор вернул решение. " +
                    $"Wave = {decision.targetWaveNumber}, " +
                    $"Budget = {decision.targetWaveBudget}, " +
                    $"Reason = {decision.debugReason}"
                );
            }
        }

        return success;
    }

    // =========================================================
    // СОБЫТИЯ ЗАБЕГА
    // =========================================================

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

    // =========================================================
    // ВСПОМОГАТЕЛЬНАЯ ЛОГИКА
    // =========================================================

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

        return true;
    }

    // Преобразуем MonoBehaviour-поля в IArenaDirector.
    // Это нужно потому, что интерфейсы напрямую в Inspector не назначаются.
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
            Debug.LogWarning(
                $"ArenaModeController: объект {behaviour.name} не реализует IArenaDirector."
            );
            return null;
        }

        // Небольшая подстраховка:
        // если подключили не тот директор не в тот слот.
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

    // =========================================================
    // ПУБЛИЧНЫЕ СЕТТЕРЫ (на будущее)
    // =========================================================

    public void SetSelectedConfigs(
        ArenaConfig arenaConfig,
        ArenaDifficultyProfile difficultyProfile,
        GameModeConfig gameModeConfig)
    {
        selectedArenaConfig = arenaConfig;
        selectedDifficultyProfile = difficultyProfile;
        selectedGameMode = gameModeConfig;
    }
}