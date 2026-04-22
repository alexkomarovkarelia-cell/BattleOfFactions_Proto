using System.Collections.Generic;
using UnityEngine;

// ArenaEnemyPoolModifierPipeline
// Это pipeline модификаторов пула врагов.
//
// Логика такая:
// базовый пул кандидатов
// -> модификатор 1
// -> модификатор 2
// -> модификатор 3
// -> итоговый пул
//
// ВАЖНО:
// Этот pipeline отвечает только за набор и веса врагов.
// Он не меняет бюджет, боевые статы или бафы.
[DisallowMultipleComponent]
public class ArenaEnemyPoolModifierPipeline : MonoBehaviour
{
    [Header("Список модификаторов пула")]
    [Tooltip("Сюда потом можно добавлять компоненты, которые реализуют IArenaEnemyPoolModifier.")]
    [SerializeField] private MonoBehaviour[] modifierBehaviours;

    [Header("Отладка")]
    [SerializeField] private bool showDebugLogs = true;

    private readonly List<IArenaEnemyPoolModifier> cachedModifiers = new List<IArenaEnemyPoolModifier>();

    private void Awake()
    {
        CacheModifiers();
    }

    private void OnValidate()
    {
        CacheModifiers();
    }
    public int ConfiguredModifierCount => cachedModifiers.Count;

    public void RefreshModifierCache()
    {
        CacheModifiers();
    }

    public void ApplyPoolModifiers(
        ArenaRunContext runContext,
        int waveNumber,
        List<ArenaEnemyPoolCandidate> candidates)
    {
        if (candidates == null || candidates.Count == 0)
            return;

        if (cachedModifiers.Count == 0)
            return;

        int beforeCount = candidates.Count;

        for (int i = 0; i < cachedModifiers.Count; i++)
        {
            IArenaEnemyPoolModifier modifier = cachedModifiers[i];

            if (modifier == null)
                continue;

            modifier.ModifyPool(runContext, waveNumber, candidates);
        }

        // Чистим мусор после модификации:
        // убираем null, выключенных и пустых кандидатов.
        for (int i = candidates.Count - 1; i >= 0; i--)
        {
            ArenaEnemyPoolCandidate candidate = candidates[i];

            if (candidate == null)
            {
                candidates.RemoveAt(i);
                continue;
            }

            if (!candidate.isEnabled)
            {
                candidates.RemoveAt(i);
                continue;
            }

            candidate.ValidateData();
        }

        if (showDebugLogs && beforeCount != candidates.Count)
        {
            Debug.Log(
                $"ArenaEnemyPoolModifierPipeline: pool modified. " +
                $"Wave = {waveNumber}, Before = {beforeCount}, After = {candidates.Count}"
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

            IArenaEnemyPoolModifier modifier = behaviour as IArenaEnemyPoolModifier;

            if (modifier == null)
            {
                Debug.LogWarning(
                    $"ArenaEnemyPoolModifierPipeline: объект {behaviour.name} не реализует IArenaEnemyPoolModifier."
                );
                continue;
            }

            cachedModifiers.Add(modifier);
        }
    }
}
