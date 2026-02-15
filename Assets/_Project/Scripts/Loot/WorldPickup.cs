using UnityEngine;

// Гарантируем: если повесили этот скрипт — на объекте ОБЯЗАТЕЛЬНО будет Collider.
[RequireComponent(typeof(Collider))]
public class WorldPickup : MonoBehaviour
{
    [Header("Тип лута: Coins, Medkit и т.д.")]
    public LootKind kind = LootKind.Coins;   // <-- ОДНА переменная!

    [Header("Количество (сколько дать)")]
    public int amount = 1;

    // Метод для LootDropper: он может задать тип и количество после Instantiate
    public void Init(LootKind newKind, int newAmount)
    {
        kind = newKind;
        amount = newAmount;
    }

    // В редакторе, когда добавили компонент или нажали Reset
    private void Reset()
    {
        MakeTrigger();
    }

    // В редакторе, когда что-то меняется в инспекторе
    private void OnValidate()
    {
        MakeTrigger();
    }

    // Включаем триггер у коллайдера
    private void MakeTrigger()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    // Когда игрок вошёл в триггер — отдаём лут
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var receiver = other.GetComponent<PlayerLootReceiver>()
                      ?? other.GetComponentInParent<PlayerLootReceiver>();

        if (receiver == null)
        {
            Debug.LogWarning("PlayerLootReceiver не найден на Player — некуда отдавать лут.");
            return;
        }

        receiver.Receive(kind, amount);
        Destroy(gameObject);
    }
}
