using UnityEngine;

// ArenaConfig
// Это ScriptableObject-конфиг конкретной арены.
//
// ВАЖНО:
// - это НЕ скрипт, который вешается на объект сцены
// - это файл-данные (asset), который хранит правила арены
// - потом директор режима будет получать ссылку на ArenaConfig
//
// Зачем это нужно:
// чтобы арена не была "жёстко зашита" в коде.
// Мы сможем добавлять новые арены без переписывания логики.
[CreateAssetMenu(
    fileName = "ArenaConfig",
    menuName = "Project/Arena/Arena Config"
)]
public class ArenaConfig : ScriptableObject
{
    [Header("Основная информация")]
    [Tooltip("Уникальный строковый ID арены. Нужен для логики, сохранений и удобства.")]
    public string arenaId = "base_arena";

    [Tooltip("Название арены, которое потом можно показывать в UI.")]
    public string arenaDisplayName = "Base Arena";

    [TextArea]
    [Tooltip("Короткая заметка про арену. Не обязательна, но полезна для порядка.")]
    public string developerNotes = "";

    [Header("Доступность арены в режимах")]
    [Tooltip("Можно ли использовать эту арену в классическом режиме.")]
    public bool allowInClassicMode = true;

    [Tooltip("Можно ли использовать эту арену в режиме Выживание.")]
    public bool allowInSurvivalMode = true;

    [Tooltip("Можно ли использовать эту арену в будущем режиме Конструктора.")]
    public bool allowInConstructorMode = false;

    [Header("Базовые настройки спавна")]
    [Tooltip("Сколько зон/точек спавна можно открыть в начале забега на этой арене.")]
    [Min(1)]
    public int startActiveSpawnZones = 2;

    [Tooltip("Максимум активных зон/точек, который можно открыть позже по ходу волн.")]
    [Min(1)]
    public int maxActiveSpawnZones = 4;

    [Tooltip("Минимальная безопасная дистанция от игрока до точки спавна.")]
    [Min(1f)]
    public float minSpawnDistanceFromPlayer = 6f;

    [Header("Темп арены")]
    [Tooltip("Базовая пауза между волнами на этой арене.")]
    [Min(0f)]
    public float baseTimeBetweenWaves = 2f;

    [Tooltip("Разрешать ли директору режима включать вмешательства окружения на этой арене.")]
    public bool allowEnvironmentInterventions = true;

    [Tooltip("Разрешать ли директору режима менять лутовые правила на этой арене.")]
    public bool allowLootInterventions = true;

    [Tooltip("Разрешать ли директору режима управлять сервисами арены (торговец, ремонт и т.д.).")]
    public bool allowServiceInterventions = true;

    // =========================================================
    // ВСПОМОГАТЕЛЬНЫЕ ПРОВЕРКИ
    // =========================================================

    // Этот метод нужен для подстраховки в инспекторе.
    // Если случайно поставили max меньше start —
    // Unity сама поправит значения.
    private void OnValidate()
    {
        if (startActiveSpawnZones < 1)
            startActiveSpawnZones = 1;

        if (maxActiveSpawnZones < 1)
            maxActiveSpawnZones = 1;

        if (maxActiveSpawnZones < startActiveSpawnZones)
            maxActiveSpawnZones = startActiveSpawnZones;

        if (minSpawnDistanceFromPlayer < 1f)
            minSpawnDistanceFromPlayer = 1f;

        if (baseTimeBetweenWaves < 0f)
            baseTimeBetweenWaves = 0f;
    }
}
