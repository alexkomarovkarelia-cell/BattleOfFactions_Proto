using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;


// HUDController — управляет UI на Canvas.
// Он НЕ хранит игровую логику, а только "показывает" информацию и включает/выключает панели.
//
// Что он делает:
// 1) Показывает HP и монеты
// 2) Показывает волну и прогресс волны
// 3) Показывает сообщение по центру (между волнами, таймер и т.д.)
// 4) Показывает ResultPanel (ПОБЕДА/ПОРАЖЕНИЕ) и умеет перезапускать сцену
// 5) Управляет StartPanel и запускает спавн после кнопки START
public class HUDController : MonoBehaviour
{
    // ===== Ссылки на элементы UI (TextMeshPro) =====
    // Эти поля заполняются в Inspector: перетаскиваешь нужные Text элементы.
    [Header("HUD Texts (TMP)")]
    [SerializeField] private TMP_Text healthText; // текст "HP: 90/100"
    [SerializeField] private TMP_Text coinsText;  // текст "Монеты: 15"

    // ===== UI волн =====
    [Header("Wave HUD (TMP)")]
    [SerializeField] private TMP_Text waveText;          // например "Волна: 1/50"
    [SerializeField] private TMP_Text waveProgressText;  // например "Врагов: 2/5"
    [SerializeField] private TMP_Text centerMessageText; // сообщение по центру: "Следующая волна через 3..."

    // ===== Панель результата (Победа/Поражение) =====
    [Header("Result Panel")]
    [SerializeField] private GameObject resultPanel;   // весь объект панели результата
    [SerializeField] private TMP_Text resultTitleText; // "ПОБЕДА" или "ПОРАЖЕНИЕ"
    [SerializeField] private TMP_Text resultHintText;  // "Нажми R или кнопку RESTART"
    [SerializeField] private TMP_Text resultStatsText; // "Пройдено волн: X/Y"
    [SerializeField] private Button restartButton;     // кнопка RESTART

    // ===== Стартовая панель (START) =====
    [Header("Start Panel")]
    [SerializeField] private GameObject startPanel; // панель со стартовой кнопкой
    [SerializeField] private Button startButton;    // кнопка START

    // ===== Ссылка на спавнер =====
    // HUD запускает спавн только после нажатия START
    // и останавливает спавн при ПОБЕДЕ/ПОРАЖЕНИИ.
    [Header("Spawner")]
    [SerializeField] private EnemySpawner spawner;

    // Флаг: показан ли результат (чтобы R работала только после победы/поражения)
    private bool resultShown = false;

    private void Awake()
    {
        // Awake вызывается до Start, когда объект создаётся.
        // Здесь мы подготавливаем UI в "начальное состояние".

        // Скрываем панель результата (чтобы при запуске она не была видна)
        if (resultPanel != null) resultPanel.SetActive(false);

        // Скрываем центральное сообщение (оно показывается только когда нужно)
        if (centerMessageText != null) centerMessageText.gameObject.SetActive(false);

        // Показываем стартовую панель (игра начнётся только после START)
        if (startPanel != null) startPanel.SetActive(true);

        // Подписываем кнопки на действия:
        // restartButton -> RestartScene()
        // startButton   -> StartGame()
        if (restartButton != null) restartButton.onClick.AddListener(RestartScene);
        if (startButton != null) startButton.onClick.AddListener(StartGame);

        // Важный момент:
        // Мы ставим паузу игре, пока игрок не нажмёт START.
        // Time.timeScale = 0 -> "время остановлено", Update работает, UI работает,
        // но физика/движение/анимации (зависящие от deltaTime) обычно "замерзают".
        Time.timeScale = 0f;
    }

    private void Update()
    {
        // R — рестарт только когда показан результат,
        // чтобы случайно не перезапустить игру во время боя.
        //
        // Keyboard.current — это New Input System.
        // rKey.wasPressedThisFrame — нажатие именно в этот кадр.
        if (resultShown && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            RestartScene();
    }

    // Нажатие START:
    // 1) прячем StartPanel
    // 2) снимаем паузу (timeScale = 1)
    // 3) просим спавнер начать спавнить
    public void StartGame()
    {
        SFXPlayer.I?.PlayStart();   // ✅ звук старта

        if (startPanel != null) startPanel.SetActive(false);

        // Возвращаем "нормальное течение времени"
        Time.timeScale = 1f;

        // Запускаем спавн врагов, если спавнер назначен
        if (spawner != null)
            spawner.BeginSpawning();
    }

    // ===== Методы обновления HUD (вызываются из других скриптов) =====

    // Обновление здоровья:
    // current и max у тебя float, но мы округляем вверх CeilToInt, чтобы красиво показывать целыми.
    // (если ты используешь int HP — тоже норм, просто передашь int как float)
    public void SetHealth(float current, float max)
    {
        if (healthText == null) return;

        // Пример: HP: 75/100
        healthText.text = $"HP: {Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
    }

    // Обновление монет
    public void SetCoins(int coins)
    {
        if (coinsText == null) return;
        coinsText.text = $"Монеты: {coins}";
    }

    // Показ текущей волны
    public void SetWave(int waveNumber, int totalWaves)
    {
        if (waveText == null) return;
        waveText.text = $"Волна: {waveNumber}/{totalWaves}";
    }

    // Прогресс волны: сколько врагов убито из нужного количества
    public void SetWaveProgress(int killedThisWave, int needThisWave)
    {
        if (waveProgressText == null) return;
        waveProgressText.text = $"Врагов: {killedThisWave}/{needThisWave}";
    }

    // Показать сообщение по центру (например: "Следующая волна через 3")
    public void ShowCenterMessage(string msg)
    {
        if (centerMessageText == null) return;
        centerMessageText.gameObject.SetActive(true);
        centerMessageText.text = msg;
    }

    // Спрятать центральное сообщение
    public void HideCenterMessage()
    {
        if (centerMessageText == null) return;
        centerMessageText.gameObject.SetActive(false);
    }

    // ===== Result (WIN / GAME OVER) =====

    // Победа: просто вызываем универсальный ShowResult с нужными текстами
    public void ShowWin()
    {
        SFXPlayer.I?.PlayWin(); // ✅Звук  победы
        ShowResult("ПОБЕДА", "Нажми R или кнопку RESTART");
    }

    // Поражение: аналогично
    public void ShowGameOver()
    {
        SFXPlayer.I?.PlayLose(); // ✅ Звук поражения
        ShowResult("ПОРАЖЕНИЕ", "Нажми R или кнопку RESTART");
    }

    // Общий метод показа результата
    private void ShowResult(string title, string hint)
    {
        // Запоминаем, что результат показан (теперь можно жать R)
        resultShown = true;

        // Ставим игру на паузу
        Time.timeScale = 0f;

        // Останавливаем спавнер, чтобы враги не продолжали появляться во время результата
        spawner?.StopSpawning();

        // Показываем панель результата и выставляем тексты
        if (resultPanel != null) resultPanel.SetActive(true);
        if (resultTitleText != null) resultTitleText.text = title;
        if (resultHintText != null) resultHintText.text = hint;

        // Статистика результата: "Пройдено волн: X/Y"
        // WavesCompleted и TotalWaves — это свойства/поля в EnemySpawner.
        if (resultStatsText != null && spawner != null)
        {
            resultStatsText.text = $"Пройдено волн: {spawner.WavesCompleted}/{spawner.TotalWaves}";
        }
    }

    // Перезапуск сцены:
    // 1) снимаем паузу (на всякий случай)
    // 2) загружаем текущую сцену заново
    private void RestartScene()
    {
        // ✅ можно использовать startClip, либо позже добавишь отдельный restartClip
        SFXPlayer.I?.PlayStart();

        StartCoroutine(RestartAfterSound());
    }

    private IEnumerator RestartAfterSound()
    {
        Time.timeScale = 1f;

        // Небольшая пауза, чтобы звук успел прозвучать (не зависит от timeScale)
        yield return new WaitForSecondsRealtime(0.15f);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

}
