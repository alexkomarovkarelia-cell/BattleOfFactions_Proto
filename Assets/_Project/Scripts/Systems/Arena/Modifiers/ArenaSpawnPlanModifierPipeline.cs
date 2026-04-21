using UnityEngine;

// ArenaSpawnPlanModifierPipeline
// Это pipeline модификаторов плана спавна.
//
// Логика:
// базовый WaveExecutionPlan
// -> модификатор 1
// -> модификатор 2
// -> модификатор 3
// -> итоговый план спавна
//
// ВАЖНО:
// Он работает только с уже собранным планом.
// Он не считает бюджет и не выбирает пул врагов.
[DisallowMultipleComponent]
public class ArenaSpawnPlanModifierPipeline : MonoBehaviour
{
    [Header("Список модификаторов плана спавна")]
    [Tooltip("Сюда потом можно добавлять компоненты, которые реализуют IArenaSpawnPlanModifier.")]
    [SerializeField] private MonoBehaviour[] modifierBehaviours;

    [Header("Отладка")]
    [SerializeField] private bool showDebugLogs = true;

    private readonly System.Collections.Generic.List<IArenaSpawnPlanModifier> cachedModifiers
        = new System.Collections.Generic.List<IArenaSpawnPlanModifier>();

    private void Awake()
    {
        CacheModifiers();
    }

    private void OnValidate()
    {
        CacheModifiers();
    }

    public void ApplySpawnPlanModifiers(
        ArenaRunContext runContext,
        ArenaDirectorDecision directorDecision,
        WaveExecutionPlan wavePlan)
    {
        if (wavePlan == null)
            return;

        if (cachedModifiers.Count == 0)
            return;

        int beforeCommands = wavePlan.spawnCommands != null ? wavePlan.spawnCommands.Count : 0;
        int beforeZones = wavePlan.activeZoneIndices != null ? wavePlan.activeZoneIndices.Count : 0;

        for (int i = 0; i < cachedModifiers.Count; i++)
        {
            IArenaSpawnPlanModifier modifier = cachedModifiers[i];

            if (modifier == null)
                continue;

            modifier.ModifySpawnPlan(runContext, directorDecision, wavePlan);
        }

        // Подстраховка после всех модификаторов
        wavePlan.ValidateData();

        int afterCommands = wavePlan.spawnCommands != null ? wavePlan.spawnCommands.Count : 0;
        int afterZones = wavePlan.activeZoneIndices != null ? wavePlan.activeZoneIndices.Count : 0;

        if (showDebugLogs && (beforeCommands != afterCommands || beforeZones != afterZones))
        {
            Debug.Log(
                $"ArenaSpawnPlanModifierPipeline: plan modified. " +
                $"Wave = {wavePlan.waveNumber}, " +
                $"Commands {beforeCommands} -> {afterCommands}, " +
                $"Zones {beforeZones} -> {afterZones}"
            );
        }
    }

    private void CacheModifiers()
    {
        cachedModifiers.Clear();

        if (modifierBehaviours == null)
            return;

        for (int i = 0; i < modifierBehaviours.Length; i++)
        {
            MonoBehaviour behaviour = modifierBehaviours[i];

            if (behaviour == null)
                continue;

            IArenaSpawnPlanModifier modifier = behaviour as IArenaSpawnPlanModifier;

            if (modifier == null)
            {
                Debug.LogWarning(
                    $"ArenaSpawnPlanModifierPipeline: объект {behaviour.name} не реализует IArenaSpawnPlanModifier."
                );
                continue;
            }

            cachedModifiers.Add(modifier);
        }
    }
}
