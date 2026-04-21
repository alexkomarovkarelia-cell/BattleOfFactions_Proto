using System.Collections.Generic;
using UnityEngine;

// ArenaRunSummaryBuilder
// Это отдельный строитель ИТОГА забега.
//
// ВАЖНО:
// Он не спавнит.
// Он не управляет режимом.
// Он не решает сложность.
//
// Его задача:
// - взять ArenaRunContext
// - посмотреть историю волн
// - собрать из неё чистую итоговую сводку
//
// Это уже база под:
// - экран результата
// - сохранение результатов игрока
// - аналитику разработчика
[DisallowMultipleComponent]
public class ArenaRunSummaryBuilder : MonoBehaviour
{
    [Header("Отладка")]
    [SerializeField] private bool showDebugLogs = true;

    public ArenaRunSummary BuildSummary(ArenaRunContext runContext)
    {
        if (runContext == null)
            return null;

        ArenaRunSummary summary = new ArenaRunSummary();

        // Основная информация
        summary.arenaId = runContext.CurrentArena != null
            ? runContext.CurrentArena.arenaId
            : "unknown_arena";

        summary.modeId = runContext.CurrentGameMode != null
            ? runContext.CurrentGameMode.modeId
            : "unknown_mode";

        summary.difficultyId = runContext.CurrentDifficultyProfile != null
            ? runContext.CurrentDifficultyProfile.id.ToString()
            : "unknown_difficulty";

        // Прогресс
        summary.completedWaves = runContext.CompletedWaves;
        summary.isEndlessMode = runContext.CurrentGameMode != null && runContext.CurrentGameMode.isEndless;

        // Если сложность переопределяет число волн — используем его.
        if (runContext.CurrentDifficultyProfile != null &&
            runContext.CurrentDifficultyProfile.overrideTotalWaves)
        {
            summary.targetWaves = runContext.CurrentDifficultyProfile.totalWaves;
        }
        else if (runContext.CurrentGameMode != null)
        {
            summary.targetWaves = runContext.CurrentGameMode.maxWaves;
        }
        else
        {
            summary.targetWaves = 0;
        }

        // История волн
        List<WavePerformanceResult> history = runContext.WaveHistory;

        if (history != null && history.Count > 0)
        {
            summary.recordedWaveCount = history.Count;

            float bestTime = float.MaxValue;
            float worstTime = 0f;
            float totalTime = 0f;
            int veryEasyCount = 0;

            for (int i = 0; i < history.Count; i++)
            {
                WavePerformanceResult result = history[i];
                if (result == null)
                    continue;

                float duration = Mathf.Max(0f, result.durationSeconds);

                totalTime += duration;

                if (duration < bestTime)
                    bestTime = duration;

                if (duration > worstTime)
                    worstTime = duration;

                if (result.wasVeryEasy)
                    veryEasyCount++;
            }

            summary.totalWaveTimeSeconds = totalTime;
            summary.bestWaveTimeSeconds = bestTime == float.MaxValue ? 0f : bestTime;
            summary.worstWaveTimeSeconds = worstTime;
            summary.averageWaveTimeSeconds = history.Count > 0 ? totalTime / history.Count : 0f;
            summary.veryEasyWaveCount = veryEasyCount;
        }
        else
        {
            summary.recordedWaveCount = 0;
            summary.totalWaveTimeSeconds = 0f;
            summary.bestWaveTimeSeconds = 0f;
            summary.worstWaveTimeSeconds = 0f;
            summary.averageWaveTimeSeconds = 0f;
            summary.veryEasyWaveCount = 0;
        }

        summary.debugSummary = BuildDebugSummary(summary);
        summary.ValidateData();

        if (showDebugLogs)
        {
            Debug.Log(
                $"ArenaRunSummaryBuilder: built summary. " +
                $"Mode = {summary.modeId}, " +
                $"Difficulty = {summary.difficultyId}, " +
                $"CompletedWaves = {summary.completedWaves}, " +
                $"RecordedWaves = {summary.recordedWaveCount}, " +
                $"TotalTime = {summary.totalWaveTimeSeconds:F2}"
            );
        }

        return summary;
    }

    private string BuildDebugSummary(ArenaRunSummary summary)
    {
        if (summary == null)
            return "Summary is null";

        string text =
            $"Run summary: arena={summary.arenaId}, mode={summary.modeId}, difficulty={summary.difficultyId}. " +
            $"CompletedWaves={summary.completedWaves}, RecordedWaves={summary.recordedWaveCount}, " +
            $"TotalTime={summary.totalWaveTimeSeconds:F2}, AvgWave={summary.averageWaveTimeSeconds:F2}.";

        if (summary.veryEasyWaveCount > 0)
            text += $" VeryEasyCount={summary.veryEasyWaveCount}.";

        return text;
    }
}