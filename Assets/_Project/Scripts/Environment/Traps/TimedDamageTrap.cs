using UnityEngine;

// TimedDamageTrap
// Простая временная одноразовая ловушка.
//
// Что умеет:
// - живёт ограниченное время;
// - ждёт вход объекта в trigger;
// - если объект имеет ObjectHealth, наносит ему урон;
// - после срабатывания уничтожается;
// - если не сработала за заданное время — тоже уничтожается.
//
// Это наш ПЕРВЫЙ тест:
// источник урона теперь не только игрок или враг,
// но и отдельный объект окружения.

[RequireComponent(typeof(Collider))]
public class TimedDamageTrap : MonoBehaviour
{
    [Header("Trap Settings")]
    [SerializeField] private int damage = 20;

    [SerializeField] private float lifetime = 8f;
    // Сколько секунд ловушка живёт максимум,
    // если никто на неё не наступил.

    [SerializeField] private bool destroyOnTrigger = true;
    // Если true — после срабатывания ловушка уничтожается сразу.

    [Header("Target Filter")]
    [SerializeField] private LayerMask targetMask;
    // Кого ловушка может задеть.
    //
    // На старте можно поставить только Player.
    // Позже можно добавить Enemy, Breakable и т.д.

    [Header("Optional VFX")]
    [SerializeField] private GameObject triggerVfxPrefab;
    [SerializeField] private Vector3 triggerVfxOffset = new Vector3(0f, 0.2f, 0f);

    private bool wasTriggered = false;

    private void Awake()
    {
        // Убеждаемся, что Collider есть и он trigger.
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void Start()
    {
        // Если за lifetime секунд никто не сработал —
        // ловушка сама исчезнет.
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Если ловушка уже сработала — выходим
        if (wasTriggered)
            return;

        // Сначала ищем ObjectHealth на объекте или его родителе
        ObjectHealth targetHealth = other.GetComponentInParent<ObjectHealth>();
        if (targetHealth == null)
            return;

        // Если цель уже мертва / разрушена — не бьём
        if (targetHealth.IsDead)
            return;

        // ВАЖНО:
        // теперь фильтруем не по collider-объекту,
        // а по корневому объекту с ObjectHealth
        if (!IsInLayerMask(targetHealth.gameObject.layer, targetMask))
            return;

        // Наносим урон
        targetHealth.TakeDamage(damage);

        wasTriggered = true;

        // Эффект срабатывания
        SpawnTriggerVfx();

        // Если ловушка одноразовая — уничтожаем
        if (destroyOnTrigger)
        {
            Destroy(gameObject);
        }
    }
    private void SpawnTriggerVfx()
    {
        if (triggerVfxPrefab == null)
            return;

        Vector3 pos = transform.position + triggerVfxOffset;
        Instantiate(triggerVfxPrefab, pos, Quaternion.identity);
    }

    private bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }
}
