using UnityEngine;

// ArenaDirectorDecision
// Это "пакет решения", который возвращает директор режима.
//
// ВАЖНО:
// Директор НЕ должен сам всё исполнять.
// Он должен:
// 1) проанализировать входные данные
// 2) принять решение
// 3) отдать решение на реализацию другим системам
//
// Именно для этого и нужен ArenaDirectorDecision.
//
// Пока мы делаем первую базовую версию.
// Позже сюда можно будет спокойно добавить:
// - более подробный SpawnPlan
// - вмешательства окружения
// - вмешательства лута
// - вмешательства сервисов
// - команды для UI
// - наградные модификаторы
[System.Serializable]
public class ArenaDirectorDecision
{
    [Header("Общий статус решения")]
    [Tooltip("Можно ли считать это решение валидным.")]
    public bool isValidDecision = true;

    [Tooltip("Нужно ли после этого решения запускать волну.")]
    public bool shouldStartWave = true;

    [Header("Данные по волне")]
    [Tooltip("Номер волны, к которой относится решение.")]
    [Min(0)]
    public int targetWaveNumber = 0;

    [Tooltip("Бюджет угрозы для этой волны.")]
    [Min(0)]
    public int targetWaveBudget = 0;

    [Tooltip("Сколько зон/точек спавна можно использовать на этой волне.")]
    [Min(1)]
    public int activeSpawnZoneCount = 1;

    [Tooltip("Через сколько секунд после решения можно запускать следующую волну.")]
    [Min(0f)]
    public float nextWaveDelay = 0f;

    [Header("Будущие вмешательства директора")]
    [Tooltip("Нужно ли раньше обычного подключить элитника.")]
    public bool requestEarlyElite = false;

    [Tooltip("Нужно ли запросить вмешательство окружения (туман, ветер, мороз и т.д.).")]
    public bool requestEnvironmentIntervention = false;

    [Tooltip("Нужно ли запросить изменение правил лута.")]
    public bool requestLootIntervention = false;

    [Tooltip("Нужно ли запросить изменение сервисов арены.")]
    public bool requestServiceIntervention = false;

    [Header("Отладка")]
    [TextArea]
    [Tooltip("Короткая заметка, почему директор принял именно такое решение.")]
    public string debugReason = "";

    // Удобный метод для создания "пустого/невалидного" решения.
    public static ArenaDirectorDecision CreateInvalid(string reason)
    {
        return new ArenaDirectorDecision
        {
            isValidDecision = false,
            shouldStartWave = false,
            debugReason = reason
        };
    }

    // Удобный метод для быстрой защиты от мусорных значений.
    public void ValidateData()
    {
        if (targetWaveNumber < 0)
            targetWaveNumber = 0;

        if (targetWaveBudget < 0)
            targetWaveBudget = 0;

        if (activeSpawnZoneCount < 1)
            activeSpawnZoneCount = 1;

        if (nextWaveDelay < 0f)
            nextWaveDelay = 0f;
    }
}