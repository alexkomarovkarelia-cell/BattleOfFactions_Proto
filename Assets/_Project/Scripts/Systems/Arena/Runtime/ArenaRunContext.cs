using UnityEngine;

// ArenaRunContext
// Это НЕ MonoBehaviour и НЕ ScriptableObject.
// Это обычный класс, который хранит состояние ТЕКУЩЕГО забега.
//
// ВАЖНО:
// - директор режима будет получать именно этот объект
// - сюда потом можно спокойно добавить:
//   - команду
//   - фракцию
//   - бюджетный план
//   - погодные условия
//   - активные вмешательства директора
//   - историю волн
//   - сервисы
//   - что угодно ещё
//
// То есть это "контекст текущего забега", а не кусок логики сцены.
[System.Serializable]
public class ArenaRunContext
{
    // =========================================================
    // ОСНОВНЫЕ ДАННЫЕ ЗАБЕГА
    // =========================================================

    // Какая волна сейчас идёт.
    // На старте забега обычно ставим 0,
    // а перед первой волной переводим в 1.
    public int CurrentWave { get; private set; }

    // Общее время текущего забега в секундах.
    public float RunTime { get; private set; }

    // Активна ли сейчас арена.
    public bool IsRunActive { get; private set; }

    // Завершён ли забег.
    public bool IsRunFinished { get; private set; }

    // Какая арена используется сейчас.
    public ArenaConfig CurrentArena { get; private set; }

    // Какой профиль сложности сейчас активен.
    public ArenaDifficultyProfile CurrentDifficultyProfile { get; private set; }

    // Какой режим сейчас активен.
    public GameModeConfig CurrentGameMode { get; private set; }
    //Какой профиль правил выбран
    public ArenaModeRulesProfile CurrentModeRulesProfile { get; private set; }

    // =========================================================
    // БАЗОВАЯ СТАТИСТИКА ЗАБЕГА
    // =========================================================

    // Сколько врагов убито за весь забег.
    public int TotalEnemiesKilled { get; private set; }

    // Сколько волн уже завершено.
    public int CompletedWaves { get; private set; }

    // Сколько времени заняла предыдущая волна.
    // Пока это просто одно поле.
    // Потом можно сделать отдельную историю по волнам.
    public float LastWaveDuration { get; private set; }

    // Была ли последняя волна очень лёгкой по оценке системы.
    // Пока простой флаг.
    // Потом можно заменить на более богатую оценку.
    public bool LastWaveWasVeryEasy { get; private set; }

    // =========================================================
    // ЗАПУСК / СБРОС / ОБНОВЛЕНИЕ
    // =========================================================

    // Запуск нового забега.
    public void StartNewRun(
     ArenaConfig arenaConfig,
     ArenaDifficultyProfile difficultyProfile,
     GameModeConfig gameModeConfig,
     ArenaModeRulesProfile modeRulesProfile)
    {
        CurrentArena = arenaConfig;
        CurrentDifficultyProfile = difficultyProfile;
        CurrentGameMode = gameModeConfig;
        CurrentModeRulesProfile = modeRulesProfile;

        CurrentWave = 0;
        RunTime = 0f;
        IsRunActive = true;
        IsRunFinished = false;

        TotalEnemiesKilled = 0;
        CompletedWaves = 0;
        LastWaveDuration = 0f;
        LastWaveWasVeryEasy = false;
    }

    // Полный ручной сброс контекста.
    public void ResetContext()
    {
        CurrentArena = null;
        CurrentDifficultyProfile = null;
        CurrentGameMode = null;
        CurrentModeRulesProfile = null;

        CurrentWave = 0;
        RunTime = 0f;
        IsRunActive = false;
        IsRunFinished = false;

        TotalEnemiesKilled = 0;
        CompletedWaves = 0;
        LastWaveDuration = 0f;
        LastWaveWasVeryEasy = false;
    }

    // Обновление времени забега.
    // Эту функцию потом сможет вызывать контроллер режима или директор.
    public void TickRunTime(float deltaTime)
    {
        if (!IsRunActive || IsRunFinished)
            return;

        if (deltaTime < 0f)
            return;

        RunTime += deltaTime;
    }

    // Переход к следующей волне.
    public void AdvanceToNextWave()
    {
        if (!IsRunActive || IsRunFinished)
            return;

        CurrentWave++;
    }

    // Сообщаем, что волна завершена.
    public void MarkWaveCompleted(float waveDuration, bool wasVeryEasy)
    {
        if (!IsRunActive || IsRunFinished)
            return;

        CompletedWaves++;
        LastWaveDuration = Mathf.Max(0f, waveDuration);
        LastWaveWasVeryEasy = wasVeryEasy;
    }

    // Сообщаем, что был убит ещё один враг.
    public void RegisterEnemyKill()
    {
        if (!IsRunActive || IsRunFinished)
            return;

        TotalEnemiesKilled++;
    }

    // Завершаем забег.
    public void FinishRun()
    {
        if (!IsRunActive)
            return;

        IsRunActive = false;
        IsRunFinished = true;
    }
}