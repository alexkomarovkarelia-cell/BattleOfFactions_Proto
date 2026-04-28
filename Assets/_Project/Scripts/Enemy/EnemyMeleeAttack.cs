using UnityEngine;

// EnemyMeleeAttack Ч отвечает только за атаку вблизи.
// ƒвижение делает EnemyChase, здоровье Ч EnemyHealth.
//
// „то мен€ем на этапе 7C:
// - враг больше не бьЄт жЄстко PlayerHealth;
// - враг бьЄт общий ObjectHealth у цели.
//
// Ёто важно дл€ архитектуры:
// источник урона теперь зав€зан не на конкретный класс игрока,
// а на общий фундамент значени€.

[RequireComponent(typeof(EnemyHealth))]
public class EnemyMeleeAttack : MonoBehaviour
{
    [Header("÷ель (можно не задавать, найдЄм по тегу Player)")]
    [SerializeField] private Transform target;
    [SerializeField] private string playerTag = "Player";

    [Header("јтака")]
    [SerializeField] private float attackRange = 2.0f;
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackCooldown = 1f;

    private ObjectHealth targetHealth;
    private EnemyHealth enemyHealth;
    private float lastAttackTime = -999f;

    private int baseAttackDamage;
    private float baseAttackCooldown;

    private void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        baseAttackDamage = attackDamage;
        baseAttackCooldown = attackCooldown;
    }

    private void Start()
    {
        // ≈сли на враге есть EnemyChase и у него назначена цель Ч берЄм еЄ.
        EnemyChase chase = GetComponent<EnemyChase>();
        if (chase != null && chase.target != null)
            target = chase.target;

        // ≈сли цель всЄ ещЄ не задана Ч ищем игрока по тегу.
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObj != null)
                target = playerObj.transform;
        }

        // ¬место PlayerHealth берЄм общий ObjectHealth.
        if (target != null)
            targetHealth = target.GetComponent<ObjectHealth>();

        if (targetHealth == null)
            Debug.LogWarning("ObjectHealth не найден Ч враг не сможет наносить урон цели.");
    }

    private void Update()
    {
        // ≈сли враг умер Ч не атакуем
        if (enemyHealth != null && enemyHealth.IsDead)
            return;

        if (target == null || targetHealth == null)
            return;

        // ≈сли цель уже мертва Ч тоже не атакуем
        if (targetHealth.IsDead)
            return;

        float dist = Vector3.Distance(transform.position, target.position);
        if (dist > attackRange)
            return;

        if (Time.time - lastAttackTime < attackCooldown)
            return;

        targetHealth.TakeDamage(attackDamage);
        lastAttackTime = Time.time;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    public void ApplyDifficulty(float damageMultiplier, float cooldownMultiplier)
    {
        if (damageMultiplier <= 0f) damageMultiplier = 1f;
        if (cooldownMultiplier <= 0f) cooldownMultiplier = 1f;

        attackDamage = Mathf.Max(1, Mathf.RoundToInt(baseAttackDamage * damageMultiplier));
        attackCooldown = Mathf.Max(0.05f, baseAttackCooldown * cooldownMultiplier);
    }
}