using System.Collections.Generic;
using UnityEngine;

// WaveExecutionPlan
// Это ГОТОВЫЙ план одной волны,
// который потом будет исполнять ArenaWaveSpawner.
//
// ВАЖНО:
// Директор режима НЕ должен сам руками расписывать каждый spawn.
// Директор говорит "какой тип давления нужен",
// а WavePlanBuilder превращает это в конкретный план.
//
// Этот класс специально делаем как "контейнер данных".
// Он не должен думать, он должен просто хранить уже собранный план.
[System.Serializable]
public class WaveExecutionPlan
{
    [Header("Общая информация о волне")]
    [Tooltip("Номер волны, к которой относится этот план.")]
    public int waveNumber = 1;

    [Tooltip("Исходный бюджет волны, который пришёл от директора.")]
    public int sourceBudget = 0;

    [Tooltip("Через сколько секунд после команды можно начинать волну.")]
    public float startDelay = 0f;

    [Header("Общая структура плана")]
    [Tooltip("Сколько одновременно активных зон спавна разрешено в этом плане.")]
    public int activeSpawnZoneCount = 1;

    [Tooltip("Есть ли в этой волне ранний элитный враг.")]
    public bool containsEarlyElite = false;

    [Tooltip("Нужно ли для этой волны включить окруженческое вмешательство.")]
    public bool requestEnvironmentIntervention = false;

    [Tooltip("Нужно ли для этой волны изменить лутовые правила.")]
    public bool requestLootIntervention = false;

    [Tooltip("Нужно ли для этой волны изменить сервисы арены.")]
    public bool requestServiceIntervention = false;

    [Header("Конкретные точки спавна")]
    [Tooltip("Список конкретных команд спавна для этой волны.")]
    public List<WaveSpawnCommand> spawnCommands = new List<WaveSpawnCommand>();

    [Header("Отладка")]
    [TextArea]
    [Tooltip("Короткое описание, как именно был собран этот план.")]
    public string debugSummary = "";

    // Удобное свойство:
    // сколько всего команд спавна собрано в плане.
    public int TotalSpawnCommands => spawnCommands != null ? spawnCommands.Count : 0;

    // Быстрая проверка:
    // план считается валидным, если в нём есть хотя бы одна команда спавна.
    public bool IsValid()
    {
        return spawnCommands != null && spawnCommands.Count > 0;
    }

    // Удобный метод для подстраховки от мусорных значений.
    public void ValidateData()
    {
        if (waveNumber < 1)
            waveNumber = 1;

        if (sourceBudget < 0)
            sourceBudget = 0;

        if (startDelay < 0f)
            startDelay = 0f;

        if (activeSpawnZoneCount < 1)
            activeSpawnZoneCount = 1;

        if (spawnCommands == null)
            spawnCommands = new List<WaveSpawnCommand>();

        for (int i = 0; i < spawnCommands.Count; i++)
        {
            if (spawnCommands[i] == null)
                continue;

            spawnCommands[i].ValidateData();
        }
    }

    // Удобный статический конструктор для пустого плана.
    public static WaveExecutionPlan CreateEmpty(string reason = "")
    {
        return new WaveExecutionPlan
        {
            waveNumber = 1,
            sourceBudget = 0,
            startDelay = 0f,
            activeSpawnZoneCount = 1,
            debugSummary = string.IsNullOrWhiteSpace(reason) ? "Empty plan" : reason,
            spawnCommands = new List<WaveSpawnCommand>()
        };
    }
}

// WaveSpawnCommand
// Это ОДНА конкретная команда спавна внутри волны.
//
// Например:
// - заспавнить melee_basic
// - в точке 3
// - через 1.5 секунды после старта волны
// - это элитный или обычный вариант
//
// Позже сюда можно будет добавить:
// - редкость
// - вариант врага
// - параметры бафа
// - фракцию
// - особые теги
[System.Serializable]
public class WaveSpawnCommand
{
    [Tooltip("ID типа врага. Пока это строка, чтобы не жёстко шить всё в enum.")]
    public string enemyTypeId = "melee_basic";

    [Tooltip("Индекс точки спавна, который потом использует спавнер.")]
    public int spawnPointIndex = 0;

    [Tooltip("Через сколько секунд после старта волны нужно сделать этот спавн.")]
    public float spawnDelay = 0f;

    [Tooltip("Является ли этот конкретный спавн элитной версией.")]
    public bool isElite = false;

    [Tooltip("Дополнительная заметка для отладки.")]
    public string debugNote = "";

    public void ValidateData()
    {
        if (string.IsNullOrWhiteSpace(enemyTypeId))
            enemyTypeId = "melee_basic";

        if (spawnPointIndex < 0)
            spawnPointIndex = 0;

        if (spawnDelay < 0f)
            spawnDelay = 0f;
    }
}
