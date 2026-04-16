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
// То есть теперь HUD не работает со старым EnemySpawner,
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

    // =========================================================
    // НОВАЯ СИСТЕМА АРЕНЫ
    // =========================================================

    [Header("Arena System")]
    [SerializeField] private ArenaModeController arenaModeController;

    [Tooltip("Какая арена будет выбрана сейчас. Пока берём одну базовую арену.")]
    [SerializeField] private ArenaConfig defaultArenaConfig;

    [Tooltip("Режим Классика. Пока кнопка START запускает именно его.")]
    [SerializeField] private GameModeConfig classicModeConfig;

    [Tooltip("Профиль сложности Easy.")]
    [SerializeField] private ArenaDifficultyProfile easyDifficultyProfile;

    [Tooltip("Профиль сложности Normal.")]
    [SerializeField] private ArenaDifficultyProfile normalDifficultyProfile;

    [Tooltip("Профиль сложности Hard.")]
    [SerializeField] private ArenaDifficultyProfile hardDifficultyProfile;

    // Флаг: показан ли результат
    private bool resultShown = false;

    private void Awake()
    {
        // Скрываем результат
        if (resultPanel != null)
            resultPanel.SetActive(false);

        // Скрываем сообщение по центру
        if (centerMessageText != null)
            centerMessageText.gameObject.SetActive(false);

        // Показываем стартовую панель
        if (startPanel != null)
            startPanel.SetActive(true);

        // Подписываем кнопки
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartScene);

        if (startButton != null)
            startButton.onClick.AddListener(StartGame);

        // До старта ставим игру на паузу
        Time.timeScale = 0f;
    }

    private void Update()
    {
        // Разрешаем R только после показа результата
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
        // Звук старта
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

        if (classicModeConfig == null)
        {
            Debug.LogWarning("HUDController: не назначен classicModeConfig.");
            return;
        }

        ArenaDifficultyProfile selectedDifficulty = ResolveDifficultyFromDropdown();
        if (selectedDifficulty == null)
        {
            Debug.LogWarning("HUDController: не удалось определить выбранную сложность.");
            return;
        }

        // Передаём выбранные конфиги в новую систему арены
        arenaModeController.SetSelectedConfigs(
            defaultArenaConfig,
            selectedDifficulty,
            classicModeConfig
        );

        // Прячем стартовую панель
        if (startPanel != null)
            startPanel.SetActive(false);

        // Возвращаем нормальное течение времени
        Time.timeScale = 1f;

        // Запускаем новую систему арены
        arenaModeController.BeginArenaRun();
    }

    // Определяем профиль сложности по dropdown.
    // Пока логика простая:
    // 0 = Easy
    // 1 = Normal
    // 2 = Hard
    private ArenaDifficultyProfile ResolveDifficultyFromDropdown()
    {
        if (difficultyDropdown == null)
        {
            // Если dropdown вдруг не назначен —
            // для подстраховки берём Normal.
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

    // =========================================================
    // МЕТОДЫ ОБНОВЛЕНИЯ HUD
    // =========================================================

    public void SetHealth(float current, float max)
    {
        if (healthText == null) return;
        healthText.text = $"HP: {Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
    }

    public void SetCoins(int coins)
    {
        if (coinsText == null) return;
        coinsText.text = $"Монеты: {coins}";
    }

    public void SetWave(int waveNumber, int totalWaves)
    {
        if (waveText == null) return;
        waveText.text = $"Волна: {waveNumber}/{totalWaves}";
    }

    public void SetWaveProgress(int killedThisWave, int needThisWave)
    {
        if (waveProgressText == null) return;
        waveProgressText.text = $"Врагов: {killedThisWave}/{needThisWave}";
    }

    public void ShowCenterMessage(string msg)
    {
        if (centerMessageText == null) return;
        centerMessageText.gameObject.SetActive(true);
        centerMessageText.text = msg;
    }

    public void HideCenterMessage()
    {
        if (centerMessageText == null) return;
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

        // Останавливаем новый забег арены
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

    // Собираем текст статистики из новой системы арены.
    private string BuildResultStatsText()
    {
        if (arenaModeController == null || arenaModeController.CurrentRunContext == null)
            return "Статистика недоступна";

        ArenaRunContext runContext = arenaModeController.CurrentRunContext;

        int completedWaves = runContext.CompletedWaves;

        // Если это бесконечный режим — показываем просто число пройденных волн.
        if (runContext.CurrentGameMode != null && runContext.CurrentGameMode.isEndless)
        {
            return $"Пройдено волн: {completedWaves}";
        }

        // Если профиль сложности переопределяет общее число волн — используем его.
        if (runContext.CurrentDifficultyProfile != null &&
            runContext.CurrentDifficultyProfile.overrideTotalWaves)
        {
            return $"Пройдено волн: {completedWaves}/{runContext.CurrentDifficultyProfile.totalWaves}";
        }

        // Иначе используем лимит режима.
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
        Time.timeScale = 1f;
        yield return new WaitForSecondsRealtime(0.15f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}