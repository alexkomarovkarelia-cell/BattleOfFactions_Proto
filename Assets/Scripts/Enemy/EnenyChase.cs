
using UnityEngine;

// Враг преследует игрока + простая попытка объезда препятствий.
// ВАЖНО: это НЕ NavMesh (не “умный путь”), а простой “если упёрся — уйди в сторону”.
[RequireComponent(typeof(Rigidbody))]
public class EnemyChase : MonoBehaviour
{
    [Header("Цель")]
    public Transform target;

    [Header("Движение")]
    public float speed = 3f;
    public float stopDistance = 1.9f;
    public float rotateSpeed = 720f;

    [Header("Обход препятствий (простая защита)")]
    public float avoidRayDistance = 0.8f;   // насколько далеко “смотреть” вперед
    public float sideRayDistance = 0.6f;    // насколько далеко “смотреть” в стороны
    public float avoidStrength = 0.9f;      // насколько сильно уводить в сторону (0..2)
    public LayerMask obstacleMask = ~0;     // какие слои считаем препятствиями

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Чтобы не кувыркался
        rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // Плавнее и надёжнее при столкновениях
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    private void Start()
    {
        // Если target не задан в инспекторе — найдём игрока по тегу
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) target = playerObj.transform;
        }
    }

    private void FixedUpdate()
    {
        if (target == null) return;

        // На всякий случай — чтобы не включался кинематик во время игры
        if (rb.isKinematic) rb.isKinematic = false;

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;

        float dist = toTarget.magnitude;
        if (dist <= stopDistance)
        {
            // Останавливаем только по XZ, Y оставляем (гравитация/прыжки если будут)
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        Vector3 dir = toTarget.normalized;

        // Объезд препятствий
        dir = ApplySimpleAvoidance(dir);

        // Движение через скорость (speed = метров в секунду)
        rb.linearVelocity = new Vector3(dir.x * speed, rb.linearVelocity.y, dir.z * speed);

        // Поворот
        Quaternion targetRot = Quaternion.LookRotation(dir);
        rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRot, rotateSpeed * Time.fixedDeltaTime));
    }

    private Vector3 ApplySimpleAvoidance(Vector3 dir)
    {
        // Лучи будем пускать чуть выше пола, чтобы не цепляли Ground
        Vector3 origin = transform.position + Vector3.up * 0.5f;

        // Луч вперёд: если что-то близко — пробуем уйти в сторону
        if (Physics.Raycast(origin, dir, out RaycastHit hit, avoidRayDistance, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            // Если вдруг попали в игрока — не считаем это препятствием
            if (hit.collider.CompareTag("Player"))
                return dir;

            // Направления влево/вправо относительно текущего движения
            Vector3 left = Quaternion.Euler(0f, -90f, 0f) * dir;
            Vector3 right = Quaternion.Euler(0f, 90f, 0f) * dir;

            bool leftBlocked = Physics.Raycast(origin, left, sideRayDistance, obstacleMask, QueryTriggerInteraction.Ignore);
            bool rightBlocked = Physics.Raycast(origin, right, sideRayDistance, obstacleMask, QueryTriggerInteraction.Ignore);

            // Выбираем сторону где свободнее
            Vector3 steer;
            if (leftBlocked && !rightBlocked) steer = right;
            else if (!leftBlocked && rightBlocked) steer = left;
            else steer = right; // если оба свободны или оба заняты — по умолчанию вправо

            // Смешиваем: “вперёд” + “в сторону”
            return (dir + steer * avoidStrength).normalized;
        }

        return dir;
    }

    // Для отладки (видно в Scene, когда враг выделен)
    private void OnDrawGizmosSelected()
    {
        if (target == null) return;

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.001f) return;

        Vector3 dir = toTarget.normalized;
        Vector3 origin = transform.position + Vector3.up * 0.5f;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, origin + dir * avoidRayDistance);
    }
}



