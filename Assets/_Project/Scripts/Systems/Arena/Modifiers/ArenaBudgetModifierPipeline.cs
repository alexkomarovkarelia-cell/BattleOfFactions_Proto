using System.Collections.Generic;
using UnityEngine;

// ArenaBudgetModifierPipeline
// Это "труба", через которую проходит бюджет волны.
//
// ВАЖНО:
// - он сам ничего не придумывает
// - он не знает, какой режим сейчас главный
// - он просто берёт список модификаторов и применяет их по очереди
//
// То есть логика такая:
// базовый бюджет директора
// -> модификатор 1
// -> модификатор 2
// -> модификатор 3
// -> итоговый бюджет
//
// Сейчас список может быть пустой — и это нормально.
// Это именно точка встройки на будущее.
// ВАЖНО:
// Pipeline бюджета работает только с числом бюджета волны.
// Он не меняет боевые характеристики, бафы, дебафы и статы.
//
// Если потом нужно будет влиять на:
// - урон
// - магию
// - фракционные бонусы
// - боевые эффекты
// это делается через отдельный pipeline / отдельные модификаторы.
[DisallowMultipleComponent]
public class ArenaBudgetModifierPipeline : MonoBehaviour
{
    [Header("Список модификаторов бюджета")]
    [Tooltip("Сюда потом можно добавлять компоненты, которые реализуют IArenaBudgetModifier.")]
    [SerializeField] private MonoBehaviour[] modifierBehaviours;

    [Header("Отладка")]
    [SerializeField] private bool showDebugLogs = true;

    // Кэш интерфейсов
    private readonly List<IArenaBudgetModifier> cachedModifiers = new List<IArenaBudgetModifier>();

    private void Awake()
    {
        CacheModifiers();
    }

    private void OnValidate()
    {
        CacheModifiers();
    }

    // Применяем все найденные модификаторы по очереди.
    public int ApplyBudgetModifiers(ArenaRunContext runContext, int waveNumber, int baseBudget)
    {
        // На всякий случай не даём бюджету уйти в 0 или минус.
        int budget = Mathf.Max(1, baseBudget);

        // Если список пустой — просто возвращаем базовый бюджет.
        if (cachedModifiers.Count == 0)
            return budget;

        int originalBudget = budget;

        for (int i = 0; i < cachedModifiers.Count; i++)
        {
            IArenaBudgetModifier modifier = cachedModifiers[i];

            if (modifier == null)
                continue;

            budget = modifier.ModifyBudget(runContext, waveNumber, budget);

            // Дополнительная защита, чтобы бюджет не сломали в минус.
            budget = Mathf.Max(1, budget);
        }

        if (showDebugLogs && budget != originalBudget)
        {
            Debug.Log(
                $"ArenaBudgetModifierPipeline: budget modified. " +
                $"Wave = {waveNumber}, Base = {originalBudget}, Final = {budget}"
            );
        }

        return budget;
    }

    // Собираем все компоненты, которые реально реализуют IArenaBudgetModifier.
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

            IArenaBudgetModifier modifier = behaviour as IArenaBudgetModifier;

            if (modifier == null)
            {
                Debug.LogWarning(
                    $"ArenaBudgetModifierPipeline: объект {behaviour.name} не реализует IArenaBudgetModifier."
                );
                continue;
            }

            cachedModifiers.Add(modifier);
        }
    }
}