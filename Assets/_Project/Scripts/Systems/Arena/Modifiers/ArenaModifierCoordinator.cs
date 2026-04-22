using UnityEngine;

// ArenaModifierCoordinator
// Это центральный КООРДИНАТОР модификаторов арены.
//
// ВАЖНО:
// Он НЕ считает бюджет.
// Он НЕ строит волну.
// Он НЕ выбирает врагов.
// Он НЕ спавнит.
//
// Его задача:
// - знать, какие pipeline подключены;
// - знать, есть ли в них активные модификаторы;
// - быть одной центральной точкой координации;
// - позже стать местом, куда будут подключаться наборы событий / сезонов / фракций.
//
// То есть это "регулировщик", а не "супер-мозг".
[DisallowMultipleComponent]
public class ArenaModifierCoordinator : MonoBehaviour
{
    [Header("Pipelines модификаторов")]
    [SerializeField] private ArenaBudgetModifierPipeline budgetModifierPipeline;
    [SerializeField] private ArenaEnemyPoolModifierPipeline enemyPoolModifierPipeline;
    [SerializeField] private ArenaSpawnPlanModifierPipeline spawnPlanModifierPipeline;

    [Header("Отладка")]
    [SerializeField] private bool showDebugLogs = true;

    // Текущий контекст забега.
    // Пока просто храним его здесь, чтобы координатор знал,
    // для какого именно забега он сейчас работает.
    private ArenaRunContext currentRunContext;

    // =========================
    // Публичные свойства
    // =========================

    public ArenaRunContext CurrentRunContext => currentRunContext;

    public bool HasBudgetModifiers =>
        budgetModifierPipeline != null && budgetModifierPipeline.ConfiguredModifierCount > 0;

    public bool HasEnemyPoolModifiers =>
        enemyPoolModifierPipeline != null && enemyPoolModifierPipeline.ConfiguredModifierCount > 0;

    public bool HasSpawnPlanModifiers =>
        spawnPlanModifierPipeline != null && spawnPlanModifierPipeline.ConfiguredModifierCount > 0;

    public bool HasAnyModifiers =>
        HasBudgetModifiers || HasEnemyPoolModifiers || HasSpawnPlanModifiers;

    // =========================
    // Инициализация на забег
    // =========================

    public void InitializeForRun(ArenaRunContext runContext)
    {
        currentRunContext = runContext;

        // Просим pipeline обновить кэш на случай,
        // если что-то поменяли в Inspector.
        budgetModifierPipeline?.RefreshModifierCache();
        enemyPoolModifierPipeline?.RefreshModifierCache();
        spawnPlanModifierPipeline?.RefreshModifierCache();

        if (showDebugLogs)
        {
            Debug.Log(BuildDebugSummary());
        }
    }

    public void ClearRun()
    {
        currentRunContext = null;
    }

    // =========================
    // Служебная отладка
    // =========================

    public string BuildDebugSummary()
    {
        string modeId = "unknown_mode";
        string arenaId = "unknown_arena";

        if (currentRunContext != null)
        {
            if (currentRunContext.CurrentGameMode != null)
                modeId = currentRunContext.CurrentGameMode.modeId;

            if (currentRunContext.CurrentArena != null)
                arenaId = currentRunContext.CurrentArena.arenaId;
        }

        return
            $"ArenaModifierCoordinator: initialized. " +
            $"Arena = {arenaId}, " +
            $"Mode = {modeId}, " +
            $"BudgetMods = {GetBudgetModifierCount()}, " +
            $"PoolMods = {GetEnemyPoolModifierCount()}, " +
            $"SpawnMods = {GetSpawnPlanModifierCount()}, " +
            $"AnyModifiers = {HasAnyModifiers}";
    }

    public int GetBudgetModifierCount()
    {
        return budgetModifierPipeline != null
            ? budgetModifierPipeline.ConfiguredModifierCount
            : 0;
    }

    public int GetEnemyPoolModifierCount()
    {
        return enemyPoolModifierPipeline != null
            ? enemyPoolModifierPipeline.ConfiguredModifierCount
            : 0;
    }

    public int GetSpawnPlanModifierCount()
    {
        return spawnPlanModifierPipeline != null
            ? spawnPlanModifierPipeline.ConfiguredModifierCount
            : 0;
    }
}