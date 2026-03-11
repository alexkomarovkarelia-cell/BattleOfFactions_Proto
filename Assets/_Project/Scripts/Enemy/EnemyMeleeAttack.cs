using UnityEngine;

// EnemyMeleeAttack — отвечает только за атаку вблизи.
// Движение делает EnemyChase, здоровье — EnemyHealth.
[RequireComponent(typeof(EnemyHealth))]
public class EnemyMeleeAttack : MonoBehaviour
{
    [Header("Цель (можно не задавать, найдём по тегу Player)")]
    [SerializeField] private Transform target;
    [SerializeField] private string playerTag = "Player";

    [Header("Атака")]
    [SerializeField] private float attackRange = 2.0f; // дистанция удара
    [SerializeField] private int attackDamage = 10;    // урон за удар (int)
    [SerializeField] private float attackCooldown = 1f; // пауза между ударами

    private PlayerHealth playerHealth;
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
        // Если на враге есть EnemyChase и у него назначена цель — возьмём её
        var chase = GetComponent<EnemyChase>();
        if (chase != null && chase.target != null)
            target = chase.target;

        // Если цель всё ещё не задана — найдём игрока по тегу
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObj != null)
                target = playerObj.transform;
        }

        // Берём PlayerHealth с цели
        if (target != null)
            playerHealth = target.GetComponent<PlayerHealth>();

        if (playerHealth == null)
            Debug.LogWarning("PlayerHealth не найден — враг не сможет наносить урон.");
    }

    private void Update()
    {
        // Если враг умер — не атакуем
        if (enemyHealth != null && enemyHealth.IsDead) return;

        if (target == null || playerHealth == null) return;

        // Проверяем дистанцию
        float dist = Vector3.Distance(transform.position, target.position);
        if (dist > attackRange) return;

        // Проверяем кулдаун
        if (Time.time - lastAttackTime < attackCooldown) return;

        // Наносим урон
        playerHealth.TakeDamage(attackDamage);
        lastAttackTime = Time.time;
    }

    // Чтобы видеть радиус атаки в Scene
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
