using UnityEngine;
using UnityEngine.InputSystem;

// Этот скрипт отвечает за:
// 1) движение игрока
// 2) поворот игрока
// 3) прыжок игрока
//
// ВАЖНО:
// - движение идёт через New Input System
// - прыжок тоже идёт через Input Actions
// - поворот теперь делаем ПРАВИЛЬНО через Rigidbody.MoveRotation()
// - и делаем это в FixedUpdate(), потому что это физический объект
public class PlayerMovement : MonoBehaviour
{
    // =========================================================
    // НАСТРОЙКИ В INSPECTOR
    // =========================================================

    [Header("Как интерпретировать движение")]
    [Tooltip("true = движение относительно камеры, false = относительно игрока")]
    public bool moveRelativeToCamera = true;

    [Header("Настройки скорости")]
    [Tooltip("Скорость перемещения игрока")]
    public float moveSpeed = 5f;

    [Tooltip("Скорость поворота игрока")]
    public float rotationSpeed = 4f;

    [Tooltip("Сила прыжка вверх")]
    public float jumpForce = 5f;

    [Tooltip("Множитель скорости движения назад. 0.5 = назад идём в 2 раза медленнее")]
    [Range(0.1f, 1f)]
    public float backwardSpeedMultiplier = 0.5f;

    // =========================================================
    // ВНУТРЕННИЕ ПЕРЕМЕННЫЕ
    // =========================================================

    // Сгенерированный класс Input Actions
    private PlayerInputActions inputActions;

    // Текущий ввод движения:
    // X = влево/вправо
    // Y = вперёд/назад
    private Vector2 moveInput;

    // Флаг прыжка:
    // когда нажали Jump, ставим true,
    // а потом в логике прыжка его сбрасываем
    private bool jumpPressed = false;

    // Стоит ли игрок на земле
    private bool isGrounded = true;

    // Rigidbody игрока
    private Rigidbody rb;

    // "Курс" движения по оси Y.
    // Он нужен, чтобы при свободном обзоре мышью
    // движение не дёргалось каждый кадр.
    private float moveYaw;

    // Помогает один раз правильно задать курс движения
    private bool moveYawInitialized = false;

    // =========================================================
    // UNITY: ИНИЦИАЛИЗАЦИЯ
    // =========================================================

    private void Awake()
    {
        // Создаём объект ввода
        inputActions = new PlayerInputActions();

        // Получаем Rigidbody
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError("PlayerMovement: на объекте Player не найден Rigidbody.");
            enabled = false;
            return;
        }

        // Включаем сглаживание движения
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Более надёжные столкновения
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    private void Start()
    {
        // Блокируем заваливание на бок.
        // Поворот по Y оставляем свободным.
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void OnEnable()
    {
        if (inputActions == null)
            return;

        inputActions.Player.Enable();

        // Подписка на движение
        inputActions.Player.Move.performed += OnMove;
        inputActions.Player.Move.canceled += OnMove;

        // Подписка на прыжок
        inputActions.Player.Jump.performed += OnJump;
    }

    private void OnDisable()
    {
        if (inputActions == null)
            return;

        inputActions.Player.Move.performed -= OnMove;
        inputActions.Player.Move.canceled -= OnMove;
        inputActions.Player.Jump.performed -= OnJump;

        inputActions.Player.Disable();
    }

    // =========================================================
    // INPUT ACTIONS
    // =========================================================

    private void OnMove(InputAction.CallbackContext context)
    {
        // Читаем ввод движения
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        // Просто отмечаем, что игрок хочет прыгнуть
        jumpPressed = true;
    }

    // =========================================================
    // UNITY: ОБНОВЛЕНИЕ
    // =========================================================

    private void Update()
    {
        // В Update оставляем только прыжок,
        // потому что он просто читает флаг и запускает вертикальную скорость
        HandleJump();
    }

    private void FixedUpdate()
    {
        // В FixedUpdate делаем всё, что связано с Rigidbody:
        // 1) движение
        // 2) поворот
        HandleMovement();
        HandleRotation();
    }

    // =========================================================
    // ДВИЖЕНИЕ
    // =========================================================

    private void HandleMovement()
    {
        // Если зажата ПКМ — включён свободный обзор.
        // В этот момент курс движения НЕ обновляем.
        bool freeLook = Mouse.current != null && Mouse.current.rightButton.isPressed;

        // Один раз инициализируем курс
        if (!moveYawInitialized)
        {
            moveYawInitialized = true;
            moveYaw = transform.eulerAngles.y;
        }

        // Если свободного обзора нет —
        // обновляем курс движения
        if (!freeLook)
        {
            if (moveRelativeToCamera && Camera.main != null)
            {
                // Берём поворот камеры по оси Y
                moveYaw = Camera.main.transform.eulerAngles.y;
            }
            else
            {
                // Либо считаем относительно самого игрока
                moveYaw = transform.eulerAngles.y;
            }
        }

        // Превращаем угол в поворот
        Quaternion yawRot = Quaternion.Euler(0f, moveYaw, 0f);

        // Переводим ввод в мировое направление
        Vector3 movement = yawRot * new Vector3(moveInput.x, 0f, moveInput.y);

        // Нормализуем направление, чтобы по диагонали скорость не была выше,
        // чем по прямой.
        if (movement.sqrMagnitude > 1f)
        {
            movement.Normalize();
        }

        // Итоговая скорость движения.
        // По умолчанию идём с обычной скоростью.
        float currentMoveSpeed = moveSpeed;

        // Если есть движение назад (ось Y меньше нуля),
        // уменьшаем скорость движения.
        // Это даст ощущение, что вперёд идти легче, чем отступать назад.
        if (moveInput.y < -0.01f)
        {
            currentMoveSpeed *= backwardSpeedMultiplier;
        }

        // Получаем горизонтальную скорость
        Vector3 horizontalVelocity = movement * currentMoveSpeed;

        // Двигаем Rigidbody, но сохраняем текущую скорость по Y
        // (чтобы не ломать прыжок и падение)
        rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);
    }

    // =========================================================
    // ПОВОРОТ
    // =========================================================

    private void HandleRotation()
    {
        // Если зажата ПКМ —
        // это режим свободного обзора, автоповорот отключаем
        bool freeLook = Mouse.current != null && Mouse.current.rightButton.isPressed;
        if (freeLook)
            return;
        // Если есть движение назад (чисто назад или по диагонали назад),
        // то НЕ разворачиваем игрока автоматически.
        // Это даёт нормальное тактическое отступление лицом к врагу.
        bool hasBackwardMove = moveInput.y < -0.01f;
        if (hasBackwardMove)
            return;

        // Определяем угол, относительно которого считаем движение
        float yaw = transform.eulerAngles.y;

        if (moveRelativeToCamera && Camera.main != null)
            yaw = Camera.main.transform.eulerAngles.y;

        Quaternion yawRot = Quaternion.Euler(0f, yaw, 0f);

        // Получаем мировое направление движения
        Vector3 movement = yawRot * new Vector3(moveInput.x, 0f, moveInput.y);

        // Если движения почти нет — не вращаемся
        if (movement.sqrMagnitude <= 0.001f)
            return;

        // Целевой поворот в сторону движения
        Quaternion targetRotation = Quaternion.LookRotation(movement.normalized, Vector3.up);

        // Плавно вращаем Rigidbody.
        // Это правильнее для физического объекта, чем transform.rotation в Update.
        Quaternion smoothRotation = Quaternion.Slerp(
            rb.rotation,
            targetRotation,
            rotationSpeed * Time.fixedDeltaTime
        );

        rb.MoveRotation(smoothRotation);
    }

    // =========================================================
    // ПРЫЖОК
    // =========================================================

    private void HandleJump()
    {
        if (!jumpPressed)
            return;

        // Сбрасываем флаг сразу
        jumpPressed = false;

        // Прыгать можно только с земли
        if (!isGrounded)
            return;

        // Добавляем вертикальную скорость вверх
        rb.linearVelocity += Vector3.up * jumpForce;

        // После прыжка считаем, что игрок уже не на земле
        isGrounded = false;

        Debug.Log("ПРЫЖОК");
    }

    // =========================================================
    // СТОЛКНОВЕНИЯ С ЗЕМЛЁЙ
    // =========================================================

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
            Debug.Log("ПРИЗЕМЛИЛСЯ");
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        // Пока специально ничего не делаем
    }

    // =========================================================
    // GIZMOS ДЛЯ ОТЛАДКИ
    // =========================================================

    private void OnDrawGizmos()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * 0.2f);

        Gizmos.color = Color.blue;
        Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y);

        if (direction.magnitude > 0.1f)
        {
            Gizmos.DrawRay(transform.position, direction.normalized * 2f);
        }
    }
}