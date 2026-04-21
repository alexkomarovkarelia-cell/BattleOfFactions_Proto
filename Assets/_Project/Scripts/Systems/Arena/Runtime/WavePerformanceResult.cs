using UnityEngine;

// WavePerformanceResult
// Это простой объект-результат одной завершённой волны.
//
// ВАЖНО:
// Это НЕ MonoBehaviour.
// Это НЕ ScriptableObject.
// Это просто контейнер данных,
// который хранит базовую информацию о том,
// как была пройдена конкретная волна.
[System.Serializable]
public class WavePerformanceResult
{
    [Header("Основные данные волны")]
    public int waveNumber = 1;

    [Tooltip("Сколько секунд заняла волна.")]
    public float durationSeconds = 0f;

    [Tooltip("Была ли волна слишком лёгкой по базовой оценке.")]
    public bool wasVeryEasy = false;

    [Header("Отладка")]
    [TextArea]
    public string debugSummary = "";

    public void ValidateData()
    {
        if (waveNumber < 1)
            waveNumber = 1;

        if (durationSeconds < 0f)
            durationSeconds = 0f;
    }
}