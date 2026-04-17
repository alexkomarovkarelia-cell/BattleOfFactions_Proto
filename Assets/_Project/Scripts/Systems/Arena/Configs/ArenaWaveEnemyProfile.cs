using UnityEngine;

// ArenaWaveEnemyProfile
// Это ОПИСАНИЕ врага для волновой системы.
//
// ВАЖНО:
// Это не сам враг на сцене.
// Это не MonoBehaviour на prefab.
// Это именно data-asset (ScriptableObject), который говорит:
//
// - какой enemyTypeId использовать
// - с какой волны враг может появляться
// - до какой волны он актуален
// - какой у него вес выбора
// - в каких режимах он разрешён
//
// Потом WavePlanBuilder / EnemyPoolResolver будут читать эти профили
// и решать, кого можно спавнить на текущей волне.
[CreateAssetMenu(
    fileName = "ArenaWaveEnemyProfile",
    menuName = "Project/Arena/Wave Enemy Profile"
)]
public class ArenaWaveEnemyProfile : ScriptableObject
{
    [Header("Основная информация")]
    [Tooltip("Уникальный ID типа врага. Должен совпадать с enemyTypeId в ArenaWaveSpawner.")]
    public string enemyTypeId = "melee_basic";

    [Tooltip("Понятное имя для себя.")]
    public string displayName = "Melee Basic";

    [TextArea]
    [Tooltip("Заметка для себя.")]
    public string developerNotes = "";

    [Header("Окно появления по волнам")]
    [Tooltip("С какой волны враг может появляться.")]
    [Min(1)]
    public int unlockWave = 1;

    [Tooltip("До какой волны враг актуален. 0 = без лимита.")]
    [Min(0)]
    public int retireWave = 0;

    [Header("Вес выбора")]
    [Tooltip("Чем выше число, тем чаще враг выбирается среди доступных.")]
    [Min(1)]
    public int spawnWeight = 10;

    [Header("Разрешение по режимам")]
    public bool allowInClassic = true;
    public bool allowInSurvival = true;
    public bool allowInConstructor = true;

    [Header("Отладка")]
    public bool isEnabled = true;

    public bool CanAppear(GameModeConfig currentGameMode, int waveNumber)
    {
        if (!isEnabled)
            return false;

        if (waveNumber < unlockWave)
            return false;

        if (retireWave > 0 && waveNumber > retireWave)
            return false;

        string modeId = currentGameMode != null ? currentGameMode.modeId : "classic";

        switch (modeId)
        {
            case "classic":
                return allowInClassic;

            case "survival":
                return allowInSurvival;

            case "constructor":
                return allowInConstructor;

            default:
                return true;
        }
    }

    private void OnValidate()
    {
        if (unlockWave < 1)
            unlockWave = 1;

        if (spawnWeight < 1)
            spawnWeight = 1;

        if (retireWave < 0)
            retireWave = 0;
    }
}