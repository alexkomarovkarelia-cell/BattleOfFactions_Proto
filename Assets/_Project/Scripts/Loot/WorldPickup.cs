using UnityEngine;

// WorldPickup — предмет лута в мире (монета/аптечка).
// ВАЖНО: ЛОГИКА применения лута остаётся в PlayerLootReceiver,
// а весь "juice" (3D звук + VFX) делаем здесь, на самом предмете.
[RequireComponent(typeof(Collider))]
public class WorldPickup : MonoBehaviour
{
    [Header("Loot Data")]
    public LootKind kind = LootKind.Coins;
    public int amount = 1;

    [Header("Pickup Feedback (3D SFX + VFX)")]
    [Tooltip("Перетащи сюда child-объект Visual (из префаба). Мы будем выключать только визуал.")]
    [SerializeField] private Transform visualRoot;

    [Tooltip("3D AudioSource на root префаба (Spatial Blend = 1). Если не задан — найдём автоматически.")]
    [SerializeField] private AudioSource pickupSource;

    [Tooltip("Клип подбора (у монеты свой, у аптечки свой).")]
    [SerializeField] private AudioClip pickupClip;

    [Tooltip("VFX префаб (например VFX_PickupSparkle).")]
    [SerializeField] private GameObject pickupVfxPrefab;

    [Tooltip("Если клипа нет — через сколько уничтожить объект.")]
    [SerializeField] private float fallbackDestroyDelay = 0.05f;

    private bool pickedUp;

    private void Awake()
    {
        // Гарантируем, что коллайдер — триггер (чтобы "подошёл -> подобрал")
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        // Если AudioSource не назначили — возьмём с этого же объекта
        if (pickupSource == null)
            pickupSource = GetComponent<AudioSource>();
    }

    // Если дроппер создаёт лут программно — можно вызывать Init.
    public void Init(LootKind newKind, int newAmount)
    {
        kind = newKind;
        amount = newAmount;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (pickedUp) return;
        if (!other.CompareTag("Player")) return;

        // Находим принимающий компонент на игроке
        var receiver = other.GetComponent<PlayerLootReceiver>()
                      ?? other.GetComponentInParent<PlayerLootReceiver>();

        if (receiver == null)
        {
            Debug.LogWarning("WorldPickup: не найден PlayerLootReceiver на игроке.");
            return;
        }

        pickedUp = true;

        // 1) Применяем лут (монеты/хилл)
        receiver.Receive(kind, amount);

        // 2) VFX
        if (pickupVfxPrefab != null)
            Instantiate(pickupVfxPrefab, transform.position, Quaternion.identity);

        // 3) 3D звук (важно: НЕ уничтожать объект сразу, иначе звук обрежется)
        float destroyDelay = fallbackDestroyDelay;

        if (pickupSource != null && pickupClip != null)
        {
            pickupSource.PlayOneShot(pickupClip);
            destroyDelay = Mathf.Max(destroyDelay, pickupClip.length);
        }

        // 4) Отключаем коллайдер — чтобы не срабатывало второй раз
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // 5) Прячем визуал сразу, но объект живёт пока доиграет звук
        if (visualRoot != null)
        {
            visualRoot.gameObject.SetActive(false);
        }
        else
        {
            // запасной вариант: выключить все рендереры
            foreach (var r in GetComponentsInChildren<Renderer>())
                r.enabled = false;
        }

        // 6) Уничтожаем после звука
        Destroy(gameObject, destroyDelay);
    }
}