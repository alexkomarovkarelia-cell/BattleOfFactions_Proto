using System;
using System.Collections.Generic;
using UnityEngine;

// LootTable — ассет (ScriptableObject), который мы создаём в Project.
// В нём настраиваем: что может выпасть, шанс и количество.
[CreateAssetMenu(menuName = "Loot/Loot Table", fileName = "LootTable")]
public class LootTable : ScriptableObject
{
    public List<LootEntry> entries = new List<LootEntry>();
}

[Serializable]
public class LootEntry
{
    [Header("Тип лута (Coins / Medkit / Item)")]
    public LootKind kind = LootKind.Coins;

    [Header("Какой prefab спавнить (CoinPickup / MedkitPickup)")]
    public GameObject pickupPrefab;

    [Range(0f, 1f)]
    [Header("Шанс выпадения (0..1). 1=100%, 0.3=30%")]
    public float chance = 1f;

    [Header("Количество (или сила эффекта для Medkit)")]
    public int minAmount = 1;
    public int maxAmount = 3;
}
