using UnityEngine;

// PlayerInteraction
// Этот скрипт отвечает ТОЛЬКО за попытку взаимодействия игрока с объектами.
//
// Что он делает:
// 1. Берёт точку взаимодействия перед игроком
// 2. Ищет рядом объекты на нужном слое
// 3. Находит компонент SimpleInteractable
// 4. Вызывает у него метод Interact()
//
// Важно:
// - сам этот скрипт НЕ читает клавиши
// - кнопку E будет ловить PlayerInputHandler
// - этот скрипт только выполняет TryInteract()
public class PlayerInteraction : MonoBehaviour
{
    [Header("Точка взаимодействия")]
    [SerializeField] private Transform interactionPoint;
    // Это пустой объект перед игроком.
    // Из этой точки мы проверяем, есть ли рядом объект для взаимодействия.

    [Header("Радиус взаимодействия")]
    [SerializeField] private float interactionRadius = 1.25f;
    // Радиус поиска объектов для взаимодействия.
    // Если будет неудобно - потом немного подправим.

    [Header("Слой объектов для взаимодействия")]
    [SerializeField] private LayerMask interactableMask;
    // Здесь нужно выбрать слой Interactable.
    // Скрипт будет искать только объекты на этом слое.

    // Главный метод, который потом вызывает PlayerInputHandler
    public void TryInteract()
    {
        // Если точка взаимодействия не назначена - предупреждаем и выходим
        if (interactionPoint == null)
        {
            Debug.LogWarning("PlayerInteraction: не назначен interactionPoint!");
            return;
        }

        // Ищем все коллайдеры рядом с точкой взаимодействия
        Collider[] hits = Physics.OverlapSphere(interactionPoint.position, interactionRadius, interactableMask);

        // Если рядом ничего нет - просто выходим
        if (hits == null || hits.Length == 0)
            return;

        // Пока для MVP нам достаточно взаимодействовать
        // только с ПЕРВЫМ найденным подходящим объектом
        foreach (Collider hit in hits)
        {
            SimpleInteractable interactable = hit.GetComponentInParent<SimpleInteractable>();

            if (interactable == null)
                continue;

            interactable.Interact();
            return;
        }
    }

    // Рисуем радиус взаимодействия в окне Scene,
    // когда объект игрока выделен
    private void OnDrawGizmosSelected()
    {
        if (interactionPoint == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(interactionPoint.position, interactionRadius);
    }
}