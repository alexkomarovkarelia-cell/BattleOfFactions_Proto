using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

// HUDController
// Это ВРЕМЕННЫЙ UI-контроллер.
//
// ВАЖНО:
// - он НЕ хранит боевую логику
// - он НЕ решает, какую волну запускать
// - он НЕ является мозгом арены
//
// Его задача:
// 1) показывать тексты HUD
// 2) показывать стартовую панель
// 3) по кнопке START передавать выбранные config-данные в ArenaModeController
// 4) показывать результат (ПОБЕДА / ПОРАЖЕНИЕ)
// 5) уметь перезапускать сцену
//
// То есть HUD не запускает старый EnemySpawner,
// а работает через новую систему арены: ArenaModeController.
public class HUDController : MonoBehaviour
{
    // =========================================================
    // ССЫЛКИ НА ТЕКСТЫ HUD
    // =========================================================

    [Header("HUD Texts (TMP)")]
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text coinsText;

    [Header("Wave HUD (TMP)")]
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text waveProgressText;
    [SerializeField] private TMP_Text centerMessageText;

    // =========================================================
    // ПАНЕЛЬ РЕЗУЛЬТАТА
    // =========================================================

    [Header("Result Panel")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultTitleText;
    [SerializeField] private TMP_Text resultHintText;
    [SerializeField] private TMP_Text resultStatsText;
    [SerializeField] private Button restartButton;

    // =========================================================
    // СТАРТОВАЯ ПАНЕЛЬ
    // =========================================================

    [Header("Start Panel")]
    [SerializeField] private GameObject startPanel;
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Dropdown difficultyDropdown;
    [SerializeField] private TMP_Dropdown modeDropdown;

    // =========================================================
    // НОВАЯ СИСТЕМА АРЕНЫ
    // =========================================================

    [Header("Arena System")]
    [SerializeField] private ArenaModeController arenaModeController;

    [Tooltip("Какая арена будет выбрана сейчас.")]
    [SerializeField] private ArenaConfig defaultArenaConfig;

    [Header("Mode Configs")]
    [SerializeField] private GameModeConfig classicModeConfig;
    [SerializeField] private GameModeConfig survivalModeConfig;

    [Header("Mode Rules Profiles")]
    [SerializeField] private ArenaModeRulesProfile classicModeRulesProfile;
    [SerializeField] private ArenaModeRulesProfile survivalModeRulesProfile;

    [Header("Difficulty Profiles")]
    [SerializeField] private ArenaDifficultyProfile easyDifficultyProfile;
    [SerializeField] private ArenaDifficultyProfile normalDifficultyProfile;
    [SerializeField] private ArenaDifficultyProfile hardDifficultyProfile;

    // Флаг: показан ли экран результата
    private bool resultShown = false;

    private void Awake()
    {
        // Скрываем результат в начале
        if (resultPanel != null)
            resultPanel.SetActive(false);

        // Сообщение по центру тоже скрываем
        if (centerMessageText != null)
            centerMessageText.gameObject.SetActive(false);

        // Стартовую панель показываем
        if (startPanel != null)
            startPanel.SetActive(true);

        // Кнопка рестарта
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartScene);

        // Кнопка старта
        if (startButton != null)
            startButton.onClick.AddListener(StartGame);

        // До старта ставим игру на паузу
        Time.timeScale = 0f;
    }

    private void Update()
    {
        // После показа результата разрешаем рестарт по клавише R
        if (resultShown &&
            Keyboard.current != null &&
            Keyboard.current.rKey.wasPressedThisFrame)
        {
            RestartScene();
        }
    }

    // =========================================================
    // СТАРТ ИГРЫ
    // =========================================================

    public void StartGame()
    {
        // Проигрываем звук старта, если есть
        SFXPlayer.I?.PlayStart();

        if (arenaModeController == null)
        {
            Debug.LogWarning("HUDController: не назначен ArenaModeController.");
            return;
        }

        if (defaultArenaConfig == null)
        {
            Debug.LogWarning("HUDController: не назначен defaultArenaConfig.");
            return;
        }

        // Определяем выбранную сложность
        ArenaDifficultyProfile selectedDifficulty = ResolveDifficultyFromDropdown();
        if (selectedDifficulty == null)
        {
            Debug.LogWarning("HUDController: не удалось определить выбранную сложность.");
            return;
        }

        // Определяем выбранный режим и профиль правил этого режима
        ResolveModeSelection(
            out GameModeConfig selectedModeConfig,
            out ArenaModeRulesProfile selectedRulesProfile
        );

        if (selectedModeConfig == null)
        {
            Debug.LogWarning("HUDController: не удалось определить выбранный GameModeConfig.");
            return;
        }

        if (selectedRulesProfile == null)
        {
            Debug.LogWarning("HUDController: не удалось определить выбранный ArenaModeRulesProfile.");
            return;
        }

        // Передаём ВСЕ выбранные данные в ArenaModeController
        arenaModeController.SetSelectedConfigs(
            defaultArenaConfig,
            selectedDifficulty,
            selectedModeConfig,
            selectedRulesProfile
        );

        // Прячем стартовую панель
        if (startPanel != null)
            startPanel.SetActive(false);

        // Возвращаем игре нормальное течение времени
        Time.timeScale = 1f;

        // Запускаем арену
        arenaModeController.BeginArenaRun();
    }

    // Определяем сложность по dropdown.
    // ВАЖНО:
    // 0 = Лёгкий
    // 1 = Нормальный
    // 2 = Сложный
    private ArenaDifficultyProfile ResolveDifficultyFromDropdown()
    {
        if (difficultyDropdown == null)
        {
            // Если dropdown не назначен, по умолчанию берём Normal
            return normalDifficultyProfile;
        }

        switch (difficultyDropdown.value)
        {
            case 0:
                return easyDifficultyProfile;

            case 2:
                return hardDifficultyProfile;

            case 1:
            default:
                return normalDifficultyProfile;
        }
    }

    // Определяем режим по dropdown.
    // ВАЖНО:
    // 0 = Классика
    // 1 = Выживание
    private void ResolveModeSelection(
        out GameModeConfig selectedModeConfig,
        out ArenaModeRulesProfile selectedRulesProfile)
    {
        // Если modeDropdown не назначен, по умолчанию запускаем классику
        if (modeDropdown == null)
        {
            selectedModeConfig = classicModeConfig;
            selectedRulesProfile = classicModeRulesProfile;
            return;
        }

        switch (modeDropdown.value)
        {
            case 1:
                selectedModeConfig = survivalModeConfig;
                selectedRulesProfile = survivalModeRulesProfile;
                break;

            case 0:
            default:
                selectedModeConfig = classicModeConfig;
                selectedRulesProfile = classicModeRulesProfile;
                break;
        }
    }

    // =========================================================
    // МЕТОДЫ ОБНОВЛЕНИЯ HUD
    // =========================================================

    public void SetHealth(float current, float max)
    {
        if (healthText == null)
            return;

        healthText.text = $"HP: {Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
    }

    public void SetCoins(int coins)
    {
        if (coinsText == null)
            return;

        coinsText.text = $"Монеты: {coins}";
    }

    public void SetWave(int waveNumber, int totalWaves)
    {
        if (waveText == null)
            return;

        waveText.text = $"Волна: {waveNumber}/{totalWaves}";
    }

    public void SetWaveProgress(int killedThisWave, int needThisWave)
    {
        if (waveProgressText == null)
            return;

        waveProgressText.text = $"Врагов: {killedThisWave}/{needThisWave}";
    }

    public void ShowCenterMessage(string msg)
    {
        if (centerMessageText == null)
            return;

        centerMessageText.gameObject.SetActive(true);
        centerMessageText.text = msg;
    }

    public void HideCenterMessage()
    {
        if (centerMessageText == null)
            return;

        centerMessageText.gameObject.SetActive(false);
    }

    // =========================================================
    // RESULT (WIN / LOSE)
    // =========================================================

    public void ShowWin()
    {
        SFXPlayer.I?.PlayWin();
        ShowResult("ПОБЕДА", "Нажми R или кнопку RESTART");
    }

    public void ShowGameOver()
    {
        SFXPlayer.I?.PlayLose();
        ShowResult("ПОРАЖЕНИЕ", "Нажми R или кнопку RESTART");
    }

    private void ShowResult(string title, string hint)
    {
        resultShown = true;

        // Ставим игру на паузу
        Time.timeScale = 0f;

        // Останавливаем текущий забег
        arenaModeController?.StopArenaRun();

        if (resultPanel != null)
            resultPanel.SetActive(true);

        if (resultTitleText != null)
            resultTitleText.text = title;

        if (resultHintText != null)
            resultHintText.text = hint;

        if (resultStatsText != null)
            resultStatsText.text = BuildResultStatsText();
    }

    // Собираем текст статистики из новой системы арены
    private string BuildResultStatsText()
    {
        if (arenaModeController == null)
            return "Статистика недоступна";

        // 1. Сначала пробуем взять готовый summary
        ArenaRunSummary summary = arenaModeController.LastRunSummary;

        if (summary != null)
        {
            string wavesText;

            if (summary.isEndlessMode)
                wavesText = $"Пройдено волн: {summary.completedWaves}";
            else if (summary.targetWaves > 0)
                wavesText = $"Пройдено волн: {summary.completedWaves}/{summary.targetWaves}";
            else
                wavesText = $"Пройдено волн: {summary.completedWaves}";

            return
                $"{wavesText}\n" +
                $"Лучшее время волны: {summary.bestWaveTimeSeconds:F2} сек\n" +
                $"Худшее время волны: {summary.worstWaveTimeSeconds:F2} сек\n" +
                $"Среднее время волны: {summary.averageWaveTimeSeconds:F2} сек\n" +
                $"Лёгких волн: {summary.veryEasyWaveCount}";
        }

        // 2. Если summary по какой-то причине ещё нет —
        // используем старую запасную логику
        if (arenaModeController.CurrentRunContext == null)
            return "Статистика недоступна";

        ArenaRunContext runContext = arenaModeController.CurrentRunContext;
        int completedWaves = runContext.CompletedWaves;

        if (runContext.CurrentGameMode != null && runContext.CurrentGameMode.isEndless)
        {
            return $"Пройдено волн: {completedWaves}";
        }

        if (runContext.CurrentDifficultyProfile != null &&
            runContext.CurrentDifficultyProfile.overrideTotalWaves)
        {
            return $"Пройдено волн: {completedWaves}/{runContext.CurrentDifficultyProfile.totalWaves}";
        }

        if (runContext.CurrentGameMode != null)
        {
            return $"Пройдено волн: {completedWaves}/{runContext.CurrentGameMode.maxWaves}";
        }

        return $"Пройдено волн: {completedWaves}";
    }

    // =========================================================
    // RESTART
    // =========================================================

    private void RestartScene()
    {
        SFXPlayer.I?.PlayStart();
        StartCoroutine(RestartAfterSound());
    }

    private IEnumerator RestartAfterSound()
    {
        // Перед перезагрузкой возвращаем время
        Time.timeScale = 1f;

        // Небольшая пауза, чтобы звук успел начаться
        yield return new WaitForSecondsRealtime(0.15f);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}