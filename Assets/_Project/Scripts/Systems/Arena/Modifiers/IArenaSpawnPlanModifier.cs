// IArenaSpawnPlanModifier
// Это интерфейс для ВСЕХ будущих модификаторов плана спавна.
//
// ВАЖНО:
// Этот модификатор отвечает ТОЛЬКО за уже собранный план спавна.
//
// Он может:
// - менять activeZoneIndices
// - менять startDelay
// - менять spawnDelay у команд
// - добавлять/убирать команды спавна
// - переставлять порядок
//
// Он НЕ отвечает за:
// - бюджет волны
// - выбор пула врагов
// - боевые бафы/дебафы
// - урон/статы
//
// Для этого есть другие слои.
public interface IArenaSpawnPlanModifier
{
    void ModifySpawnPlan(
        ArenaRunContext runContext,
        ArenaDirectorDecision directorDecision,
        WaveExecutionPlan wavePlan
    );
}