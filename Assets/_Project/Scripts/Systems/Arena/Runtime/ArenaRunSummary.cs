using UnityEngine;

// ArenaRunSummary
// Это ИТОГ одного завершённого забега.
//
// ВАЖНО:
// Это НЕ MonoBehaviour.
// Это НЕ ScriptableObject.
// Это просто контейнер итоговых данных.
//
// Зачем он нужен:
// - показать игроку понятный итог забега
// - потом сохранить результат игроку
// - потом использовать как базу для аналитики
// - потом дать директору сжатую сводку, а не сырой мусор
[System.Serializable]
public class ArenaRunSummary
{
    [Header("Основная информация")]
    public string arenaId = "";
    public string modeId = "";
    public string difficultyId = "";

    [Header("Прогресс забега")]
    public int completedWaves = 0;
    public int targetWaves = 0;
    public bool isEndlessMode = false;

    [Header("Время")]
    [Tooltip("Суммарное время всех завершённых волн.")]
    public float totalWaveTimeSeconds = 0f;

    [Tooltip("Самая быстрая завершённая волна.")]
    public float bestWaveTimeSeconds = 0f;

    [Tooltip("Самая долгая завершённая волна.")]
    public float worstWaveTimeSeconds = 0f;

    [Tooltip("Среднее время волны.")]
    public float averageWaveTimeSeconds = 0f;

    [Header("Оценка прохождения")]
    [Tooltip("Сколько волн были отмечены как слишком лёгкие.")]
    public int veryEasyWaveCount = 0;

    [Tooltip("Сколько результатов волн реально записано в историю.")]
    public int recordedWaveCount = 0;

    [Header("Отладка")]
    [TextArea]
    public string debugSummary = "";

    public void ValidateData()
    {
        if (completedWaves < 0)
            completedWaves = 0;

        if (targetWaves < 0)
            targetWaves = 0;

        if (totalWaveTimeSeconds < 0f)
            totalWaveTimeSeconds = 0f;

        if (bestWaveTimeSeconds < 0f)
            bestWaveTimeSeconds = 0f;

        if (worstWaveTimeSeconds < 0f)
            worstWaveTimeSeconds = 0f;

        if (averageWaveTimeSeconds < 0f)
            averageWaveTimeSeconds = 0f;

        if (veryEasyWaveCount < 0)
            veryEasyWaveCount = 0;

        if (recordedWaveCount < 0)
            recordedWaveCount = 0;
    }
}