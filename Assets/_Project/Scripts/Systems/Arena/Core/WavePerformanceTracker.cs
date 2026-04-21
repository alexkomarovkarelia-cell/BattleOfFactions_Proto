using UnityEngine;

// WavePerformanceTracker
// Это отдельный модуль анализа завершённой волны.
//
// ВАЖНО:
// Он не решает, какую волну запускать.
// Он не спавнит.
// Он не управляет режимом.
//
// Его задача:
// - принять базовые данные о завершённой волне
// - собрать WavePerformanceResult
// - вернуть его наружу
//
// Сейчас анализ простой:
// - номер волны
// - длительность
// - была ли волна "слишком лёгкой"
//
// Позже сюда можно будет добавить:
// - урон по игроку
// - использование хилок
// - число опасных моментов
// - процент HP игрока к концу волны
[DisallowMultipleComponent]
public class WavePerformanceTracker : MonoBehaviour
{
    [Header("Отладка")]
    [SerializeField] private bool showDebugLogs = true;

    public WavePerformanceResult BuildResult(
        int waveNumber,
        float waveDuration,
        bool wasVeryEasy)
    {
        WavePerformanceResult result = new WavePerformanceResult
        {
            waveNumber = waveNumber,
            durationSeconds = waveDuration,
            wasVeryEasy = wasVeryEasy,
            debugSummary = BuildDebugSummary(waveNumber, waveDuration, wasVeryEasy)
        };

        result.ValidateData();

        if (showDebugLogs)
        {
            Debug.Log(
                $"WavePerformanceTracker: built result. " +
                $"Wave = {result.waveNumber}, " +
                $"Duration = {result.durationSeconds:F2}, " +
                $"VeryEasy = {result.wasVeryEasy}"
            );
        }

        return result;
    }

    private string BuildDebugSummary(int waveNumber, float waveDuration, bool wasVeryEasy)
    {
        string summary =
            $"Wave {waveNumber} finished in {waveDuration:F2} sec.";

        if (wasVeryEasy)
            summary += " Result: very easy.";

        return summary;
    }
}
