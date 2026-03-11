// Эти две строки подключают нужные пространства имён:
// UnityEngine - базовые классы Unity (Transform, Rigidbody, MonoBehaviour и т.д.)
// UnityEngine.InputSystem - новая система ввода (Input Actions, Keyboard, Mouse и т.д.)
using UnityEngine;
using UnityEngine.InputSystem;

// Этот скрипт отвечает за:
// 1) движение игрока
// 2) поворот игрока
// 3) прыжок игрока
//
// ВАЖНО:
// - движение уже работает через Input Actions (Move)
// - в этом варианте прыжок тоже переведён на Input Actions (Jump)
// - прямое чтение Keyboard.current.spaceKey мы убрали
public class PlayerMovement : MonoBehaviour
{
    // =========================================================
    // НАСТРОЙКИ, КОТОРЫЕ ВИДНО В INSPECTOR
    // =========================================================

    [Header("Как интерпретировать движение")]
    [Tooltip("true = WASD относительно камеры, false = относительно самого игрока")]
    public bool moveRelativeToCamera = true;

    [Header("Настройки скорости")]
    [Tooltip("Скорость перемещения игрока")]
    public float moveSpeed = 5f;

    [Tooltip("Скорость поворота игрока к направлению движения")]
    public float rotationSpeed = 10f;

    [Tooltip("Сила прыжка вверх")]
    public float jumpForce = 5f;

    // =========================================================
    // ВНУТРЕННИЕ ПЕРЕМЕННЫЕ
    // =========================================================

    // Сгенерированный класс от Input Actions asset.
    // Через него мы получаем доступ к Player.Move, Player.Jump и т.д.
    private PlayerInputActions inputActions;

    // Сюда сохраняем текущее направление движения от Input Actions:
    // X = влево/вправо
    // Y = вперёд/назад
    private Vector2 moveInput;

    // Флаг прыжка:
    // true  = в этом кадре игрок запросил прыжок
    // false = запроса прыжка нет
    //
    // Почему так:
    // событие ввода приходит в любой момент кадра,
    // а саму физику прыжка мы применяем аккуратно в логике скрипта.
    private bool jumpPressed = false;

    // Стоит ли игрок на земле.
    // Пока оставляем текущую простую логику через тег Ground.
    private bool isGrounded = true;

    // Ссылка на Rigidbody игрока.
    // Через него мы двигаем персонажа физически.
    private Rigidbody rb;

    // "Курс" движения в градусах по Y.
    // Это нужно, чтобы WASD можно было считать относительно камеры,
    // но при зажатой ПКМ курс не прыгал каждый кадр.
    private float moveYaw;

    // Помогает один раз корректно инициализировать moveYaw.
    private bool moveYawInitialized = false;

    // =========================================================
    // UNITY: ИНИЦИАЛИЗАЦИЯ
    // =========================================================

    // Awake вызывается раньше Start.
    // Здесь удобно получать компоненты и создавать InputActions.
    private void Awake()
    {
        // Создаём экземпляр класса ввода.
        // Это НЕ компонент Player Input на объекте,
        // а кодовый способ работы с New Input System.
        inputActions = new PlayerInputActions();

        // Берём Rigidbody с этого же объекта.
        rb = GetComponent<Rigidbody>();

        // На случай, если Rigidbody забыли добавить.
        // Такое лучше ловить сразу.
        if (rb == null)
        {
            Debug.LogError("PlayerMovement: на объекте Player не найден Rigidbody.");
            enabled = false;
            return;
        }

        // Делаем движение более плавным визуально.
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Более надёжная проверка столкновений для движущегося объекта.
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    // Start вызывается после Awake.
    private void Start()
    {
        // Запрещаем игроку заваливаться на бок по X и Z.
        // Поворот по Y оставляем, чтобы персонаж мог разворачиваться.
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    // OnEnable вызывается, когда объект или компонент становится активным.
    private void OnEnable()
    {
        // Если inputActions не создан (например, скрипт отключили из-за ошибки),
        // то дальше не идём.
        if (inputActions == null)
            return;

        // Включаем карту действий Player.
        inputActions.Player.Enable();

        // Подписываемся на событие движения.
        // performed = когда значение движения изменилось
        // canceled  = когда ввод отпущен и движение стало (0,0)
        inputActions.Player.Move.performed += OnMove;
        inputActions.Player.Move.canceled += OnMove;

        // Подписываемся на прыжок.
        // performed срабатывает в момент нажатия кнопки Jump.
        inputActions.Player.Jump.performed += OnJump;
    }

    // OnDisable вызывается, когда объект или компонент выключается.
    private void OnDisable()
    {
        // Защита от ситуации, если inputActions почему-то ещё не создан.
        if (inputActions == null)
            return;

        // Обязательно отписываемся от событий,
        // чтобы не получать ошибки и двойные вызовы.
        inputActions.Player.Move.performed -= OnMove;
        inputActions.Player.Move.canceled -= OnMove;
        inputActions.Player.Jump.performed -= OnJump;

        // Выключаем карту действий.
        inputActions.Player.Disable();
    }

    // =========================================================
    // INPUT ACTIONS: ОБРАБОТЧИКИ СОБЫТИЙ
    // =========================================================

    // Этот метод вызывается, когда меняется движение.
    private void OnMove(InputAction.CallbackContext context)
    {
        // Читаем значение действия Move как Vector2.
        moveInput = context.ReadValue<Vector2>();

        // Примеры:
        // W   -> (0, 1)
        // S   -> (0, -1)
        // A   -> (-1, 0)
        // D   -> (1, 0)
        // W+D -> (1, 1) в сыром виде, потом используется как направление
    }

    // Этот метод вызывается в момент нажатия кнопки Jump.
    private void OnJump(InputAction.CallbackContext context)
    {
        // Мы не прыгаем прямо здесь.
        // Мы просто отмечаем, что игрок запросил прыжок.
        // Так логика получается чище и безопаснее.
        jumpPressed = true;
    }

    // =========================================================
    // UNITY: ОБНОВЛЕНИЕ
    // =========================================================

    // Update вызывается каждый кадр.
    // Сюда удобно ставить логику, которая не связана напрямую с физикой.
    private void Update()
    {
        HandleRotation();
        HandleJump();
    }

    // FixedUpdate вызывается через фиксированный интервал времени.
    // Сюда лучше помещать физическое движение через Rigidbody.
    private void FixedUpdate()
    {
        HandleMovement();
    }

    // =========================================================
    // ДВИЖЕНИЕ
    // =========================================================

    private void HandleMovement()
    {
        // Если зажата ПКМ, считаем, что включён свободный обзор камеры.
        // В этот момент курс движения не обновляем.
        bool freeLook = Mouse.current != null && Mouse.current.rightButton.isPressed;

        // Если курс ещё ни разу не инициализировали — задаём стартовый.
        if (!moveYawInitialized)
        {
            moveYawInitialized = true;
            moveYaw = transform.eulerAngles.y;
        }

        // Если свободного обзора нет — обновляем курс,
        // относительно которого будем переводить WASD в мировое направление.
        if (!freeLook)
        {
            if (moveRelativeToCamera && Camera.main != null)
            {
                // Берём только поворот камеры по Y.
                moveYaw = Camera.main.transform.eulerAngles.y;
            }
            else
            {
                // Либо считаем направление относительно самого игрока.
                moveYaw = transform.eulerAngles.y;
            }
        }

        // Создаём поворот только по оси Y.
        Quaternion yawRot = Quaternion.Euler(0f, moveYaw, 0f);

        // Переводим локальный ввод (X,Y) в мировое 3D-направление.
        Vector3 movement = yawRot * new Vector3(moveInput.x, 0f, moveInput.y);

        // Получаем горизонтальную скорость.
        Vector3 horizontalVelocity = movement * moveSpeed;

        // Применяем только XZ-скорость.
        // Текущую скорость по Y сохраняем, чтобы не ломать прыжок и гравитацию.
        rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);
    }

    // =========================================================
    // ПОВОРОТ
    // =========================================================

    private void HandleRotation()
    {
        // При свободном обзоре игрок не поворачивается вслед за движением.
        bool freeLook = Mouse.current != null && Mouse.current.rightButton.isPressed;
        if (freeLook)
            return;

        // Небольшой твой текущий дизайн:
        // если игрок жмёт назад (S), не разворачиваем его,
        // а позволяем "пятиться".
        if (moveInput.y < -0.01f)
            return;

        // Определяем yaw, относительно которого считаем направление.
        float yaw = transform.eulerAngles.y;

        if (moveRelativeToCamera && Camera.main != null)
            yaw = Camera.main.transform.eulerAngles.y;

        Quaternion yawRot = Quaternion.Euler(0f, yaw, 0f);
        Vector3 movement = yawRot * new Vector3(moveInput.x, 0f, moveInput.y);

        // Если движение заметное — поворачиваем игрока в сторону движения.
        if (movement.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }

    // =========================================================
    // ПРЫЖОК
    // =========================================================

    private void HandleJump()
    {
        // Если кнопка Jump не была нажата — выходим.
        if (!jumpPressed)
            return;

        // Сбрасываем флаг сразу,
        // чтобы один запрос прыжка не повторялся много кадров подряд.
        jumpPressed = false;

        // Прыгаем только если стоим на земле.
        if (!isGrounded)
            return;

        // Добавляем вертикальную скорость вверх.
        // Можно было бы использовать AddForce, но для текущего простого MVP
        // этот вариант тоже понятный и рабочий.
        rb.linearVelocity += Vector3.up * jumpForce;

        // После прыжка считаем, что мы уже в воздухе.
        isGrounded = false;

        Debug.Log("ПРЫЖОК");
    }

    // =========================================================
    // СТОЛКНОВЕНИЯ С ЗЕМЛЁЙ
    // =========================================================

    private void OnCollisionEnter(Collision collision)
    {
        // Если коснулись объекта с тегом Ground — считаем, что стоим на земле.
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
            Debug.Log("ПРИЗЕМЛИЛСЯ");
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        // Пока продолжаем стоять на земле — поддерживаем флаг grounded.
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        // Пока специально ничего не делаем.
        // Текущая логика grounded у тебя работает через Enter/Stay.
    }

    // =========================================================
    // GIZMOS ДЛЯ ОТЛАДКИ В SCENE
    // =========================================================

    private void OnDrawGizmos()
    {
        // Линия вниз:
        // зелёная = на земле
        // красная = в воздухе
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * 0.2f);

        // Синяя стрелка направления сырого ввода.
        Gizmos.color = Color.blue;
        Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y);

        if (direction.magnitude > 0.1f)
        {
            Gizmos.DrawRay(transform.position, direction.normalized * 2f);
        }
    }
}