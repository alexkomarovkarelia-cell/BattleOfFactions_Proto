using UnityEngine;
using UnityEngine.InputSystem;
// PlayerTargeting
// Этот скрипт отвечает ТОЛЬКО за таргет игрока.
//
// Что он делает:
// 1. Ищет врага под курсором мыши через Raycast
// 2. Хранит цель под курсором (hoveredTarget)
// 3. Хранит выбранную цель (selectedTarget)
// 4. Умеет выбрать цель
// 5. Умеет очистить цель
// 6. Умеет проверить, что цель ещё валидна
//
// ВАЖНО:
// - Этот скрипт пока НЕ атакует
// - Этот скрипт пока НЕ включает мягкий режим
// - Этот скрипт пока НЕ рисует UI
// - Это только фундамент таргета
//
// Почему это хорошо:
// - потом этот скрипт смогут использовать
//   обычная атака,
//   мягкий режим,
//   способности,
//   бафы,
//   дебафы,
//   часть дальнего боя
public class PlayerTargeting : MonoBehaviour
{
    [Header("Камера, из которой пускаем луч")]
    [SerializeField] private Camera mainCamera;
    // Обычно это Main Camera.
    // Если не назначить вручную, попробуем взять Camera.main автоматически.

    [Header("Какие слои считаем целями")]
    [SerializeField] private LayerMask targetLayerMask;
    // Здесь в Inspector нужно выбрать слой Enemy.
    // Raycast будет искать только объекты на этом слое.

    [Header("Настройки Raycast")]
    [SerializeField] private float rayDistance = 100f;
    // На какое расстояние вперёд пускать луч от камеры.
    // Для арены этого более чем достаточно.

    [Header("Отладка")]
    [SerializeField] private bool showDebugLogs = false;
    // Если включить — в Console будет легче смотреть,
    // как работает захват цели.

    // Цель, которая сейчас находится ПОД курсором мыши.
    private EnemyHealth hoveredTarget;

    // Цель, которая выбрана как текущая.
    // Позже её будет использовать мягкий режим.
    private EnemyHealth selectedTarget;

    // Публичные свойства только для чтения.
    // Другие скрипты смогут узнать текущие цели,
    // но напрямую ломать их не должны.
    public EnemyHealth HoveredTarget => hoveredTarget;
    public EnemyHealth SelectedTarget => selectedTarget;

    private void Awake()
    {
        // Если камеру не назначили вручную —
        // пробуем найти Main Camera автоматически.
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Update()
    {
        // На каждом кадре обновляем цель под курсором.
        UpdateHoveredTarget();

        // Если выбранная цель стала невалидной —
        // очищаем её.
        if (!IsTargetValid(selectedTarget))
        {
            ClearSelectedTarget();
        }
    }

    // =========================================================
    // ОСНОВНАЯ ЛОГИКА
    // =========================================================

    private void UpdateHoveredTarget()
    {
        // Если камеры нет — ничего не делаем
        if (mainCamera == null)
        {
            hoveredTarget = null;
            return;
        }

        // Если мыши нет — тоже ничего не делаем
        if (Mouse.current == null)
        {
            hoveredTarget = null;
            return;
        }

        // Читаем позицию мыши через NEW Input System
        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();

        // Строим луч из камеры в позицию курсора
        Ray ray = mainCamera.ScreenPointToRay(mouseScreenPosition);

        // Пытаемся попасть в цель только на нужном слое
        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, targetLayerMask))
        {
            // Ищем EnemyHealth на объекте или у родителя
            EnemyHealth enemyHealth = hit.collider.GetComponentInParent<EnemyHealth>();

            // Если нашли живую валидную цель — сохраняем её
            if (IsTargetValid(enemyHealth))
            {
                if (showDebugLogs && hoveredTarget != enemyHealth)
                {
                    Debug.Log($"Hovered target: {enemyHealth.name}");
                }

                hoveredTarget = enemyHealth;
                return;
            }
        }

        // Если не попали во врага — цели под курсором нет
        hoveredTarget = null;
    }

    // Выбрать текущую цель под курсором как выбранную цель.
    public bool SelectHoveredTarget()
    {
        if (!IsTargetValid(hoveredTarget))
            return false;

        selectedTarget = hoveredTarget;

        if (showDebugLogs)
            Debug.Log($"Selected target: {selectedTarget.name}");

        return true;
    }

    // Выбрать конкретную цель напрямую.
    // Это пригодится позже, если другой скрипт захочет
    // явно назначить цель без повторного Raycast.
    public bool SelectTarget(EnemyHealth target)
    {
        if (!IsTargetValid(target))
            return false;

        selectedTarget = target;

        if (showDebugLogs)
            Debug.Log($"Selected target directly: {selectedTarget.name}");

        return true;
    }

    // Очистить выбранную цель.
    public void ClearSelectedTarget()
    {
        if (showDebugLogs && selectedTarget != null)
            Debug.Log($"Selected target cleared: {selectedTarget.name}");

        selectedTarget = null;
    }

    // Полностью очистить таргет.
    // И hovered, и selected.
    public void ClearAllTargets()
    {
        hoveredTarget = null;
        selectedTarget = null;

        if (showDebugLogs)
            Debug.Log("All targets cleared");
    }

    // =========================================================
    // ПРОВЕРКА ЦЕЛИ
    // =========================================================

    // Проверяем, можно ли считать цель валидной.
    // Пока на Этапе 5 этого достаточно:
    // - цель существует
    // - объект активен
    // - враг не мёртв
    public bool IsTargetValid(EnemyHealth target)
    {
        if (target == null)
            return false;

        if (!target.gameObject.activeInHierarchy)
            return false;

        if (target.IsDead)
            return false;

        return true;
    }

    // Удобный метод: есть ли сейчас цель под курсором.
    public bool HasHoveredTarget()
    {
        return IsTargetValid(hoveredTarget);
    }

    // Удобный метод: есть ли сейчас выбранная цель.
    public bool HasSelectedTarget()
    {
        return IsTargetValid(selectedTarget);
    }

    // =========================================================
    // GIZMOS / DEBUG
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        // Здесь пока ничего сложного не рисуем.
        // Позже можно добавить отладку,
        // если понадобится.
    }
}