// Эти две строки подключают необходимые "библиотеки" Unity
// UnityEngine - основная библиотека, нужна для всех скриптов
// UnityEngine.InputSystem - библиотека для работы с новой системой ввода (клавиатура, мышь)
using UnityEngine;
using UnityEngine.InputSystem;

// Класс (скрипт) с названием PlayerMovement. MonoBehaviour - это тип скрипта, который можно вешать на объекты в Unity
public class PlayerMovement : MonoBehaviour
{
    // Куда интерпретировать WASD, когда НЕ зажата ПКМ:
    // true  = относительно камеры (удобно)
    // false = относительно игрока (танковое управление)
    public bool moveRelativeToCamera = true;

    // "Курс" (угол), по которому мы идём. Пока ПКМ зажата — курс НЕ меняется.
    private float moveYaw;
    private bool moveYawInitialized = false;

    // ================ ПЕРЕМЕННЫЕ (хранение данных) ================

    // Переменная для работы с системой ввода (Input System)
    // Input System - это как "переводчик" между твоими нажатиями клавиш и кодом
    private PlayerInputActions inputActions;

    // Vector2 - это тип данных для хранения двух чисел (X и Y)
    // Здесь храним направление движения: X - влево/вправо, Y - вперед/назад
    private Vector2 moveInput;

    // [Header("...")] - создает заголовок в инспекторе Unity для удобства
    [Header("Настройки скорости")]
    public float moveSpeed = 5f;        // Скорость перемещения (public - видно в Unity, можно менять)
    public float rotationSpeed = 10f;   // Скорость поворота игрока
    public float jumpForce = 5f;        // Сила прыжка

    // Булевая переменная (bool) - может быть только true (истина) или false (ложь)
    // Флаг, который показывает, стоит ли игрок на земле
    private bool isGrounded = true;

    // Переменная для работы с физическим телом игрока (Rigidbody)
    // Rigidbody - это компонент, который делает объект физическим (подчиняется гравитации, может сталкиваться)
    private Rigidbody rb;

    // ================ МЕТОДЫ (функции, которые что-то делают) ================

    // Awake() - метод, который вызывается ПЕРВЫМ при создании объекта
    // Здесь мы инициализируем (настраиваем) переменные
    private void Awake()
    {
        // Создаем новый экземпляр системы ввода
        // Представь, что это как купить новую клавиатуру для игры
        inputActions = new PlayerInputActions();

        // GetComponent<Rigidbody>() - ищет на этом же объекте компонент Rigidbody
        // Это как сказать: "Эй, Unity, найди на этом объекте физическое тело и дай мне на него ссылку"
        rb = GetComponent<Rigidbody>();

        // Настройки для плавности движения:
        // Interpolate - делает движение более плавным (сглаживает)
        // Continuous - точнее определяет столкновения (объекты не пролетают друг сквозь друга)
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    // OnEnable() - вызывается, когда объект становится активным (включается)
    // Здесь мы включаем систему ввода и подписываемся на события
    private void Start()
    {
        // Запрещаем физике заваливать игрока на бок
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }


    private void OnEnable()
    {
        // Включаем карту действий "Player" (в Input System есть разные карты действий)
        inputActions.Player.Enable();

        // Подписываемся на событие движения:
        // Когда игрок нажимает клавиши WASD ? вызывается метод OnMove
        // performed - когда нажал, canceled - когда отпустил
        inputActions.Player.Move.performed += OnMove;
        inputActions.Player.Move.canceled += OnMove;
    }

    // OnDisable() - вызывается, когда объект выключается
    // Здесь мы отписываемся от событий и выключаем систему ввода
    private void OnDisable()
    {
        // Важно отписаться, иначе будут ошибки когда объект уничтожится
        inputActions.Player.Move.performed -= OnMove;
        inputActions.Player.Move.canceled -= OnMove;

        // Выключаем карту действий
        inputActions.Player.Disable();
    }

    // OnMove - вызывается каждый раз, когда игрок меняет ввод движения
    // InputAction.CallbackContext context - содержит информацию о вводе (какие клавиши нажаты)
    private void OnMove(InputAction.CallbackContext context)
    {
        // context.ReadValue<Vector2>() - читаем значение ввода как Vector2
        // Например: W нажата ? (0, 1), A нажата ? (-1, 0)
        moveInput = context.ReadValue<Vector2>();

        // Пример значений moveInput:
        // Ничего не нажато: (0, 0)
        // W: (0, 1)      A: (-1, 0)      S: (0, -1)      D: (1, 0)
        // W+A: (-0.7, 0.7) - диагональ
    }

    // Update() - вызывается КАЖДЫЙ КАДР (очень часто, 60-100 раз в секунду)
    // Здесь обрабатываем ввод, который должен реагировать мгновенно (например, прыжок)
    private void Update()
    {
        // Поворачиваем игрока в сторону движения
        HandleRotation();

        // Проверяем, хочет ли игрок прыгнуть
        HandleJump();
    }

    // FixedUpdate() - вызывается с ФИКСИРОВАННОЙ частотой (по умолчанию 50 раз в секунду)
    // Здесь обрабатываем ФИЗИКУ (движение, столкновения), т.к. физика требует постоянного интервала
    private void FixedUpdate()
    {
        // Перемещаем игрока с учетом физики
        HandleMovement();
    }

    // Метод для перемещения игрока
    // Метод для перемещения игрока
    private void HandleMovement()
    {
        // ПКМ зажата = свободный обзор (камера крутится, но курс движения фиксируем)
        bool freeLook = Mouse.current != null && Mouse.current.rightButton.isPressed;

        // Если курс ещё ни разу не задавали — зададим стартовый (чтобы не было 0)
        if (!moveYawInitialized)
        {
            moveYawInitialized = true;
            moveYaw = transform.eulerAngles.y;
        }

        // Если НЕ freeLook — обновляем "курс" движения (куда считать WASD)
        // Вариант: относительно камеры или относительно игрока — выбирается флагом moveRelativeToCamera
        if (!freeLook)
        {
            if (moveRelativeToCamera && Camera.main != null)
                moveYaw = Camera.main.transform.eulerAngles.y;
            else
                moveYaw = transform.eulerAngles.y;
        }

        // Переводим ввод WASD в направление по нашему курсу
        Quaternion yawRot = Quaternion.Euler(0f, moveYaw, 0f);

        // ВАЖНО: теперь movement — в мировых координатах, но по выбранному "курсу"
        Vector3 movement = yawRot * new Vector3(moveInput.x, 0f, moveInput.y);

        // Умножаем направление на скорость
        Vector3 horizontalVelocity = movement * moveSpeed;

        // Применяем скорость к Rigidbody (Y сохраняем для прыжка/гравитации)
        rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);
    }

    // Метод для поворота игрока
    private void HandleRotation()
    {
        bool freeLook = Mouse.current != null && Mouse.current.rightButton.isPressed;
        if (freeLook) return;

        // ✅ Фикс: если жмёшь назад (S) — не поворачиваемся, просто пятимся
        if (moveInput.y < -0.01f) return;

        float yaw = transform.eulerAngles.y;
        if (moveRelativeToCamera && Camera.main != null)
            yaw = Camera.main.transform.eulerAngles.y;

        Quaternion yawRot = Quaternion.Euler(0f, yaw, 0f);
        Vector3 movement = yawRot * new Vector3(moveInput.x, 0f, moveInput.y);

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


    private void HandleJump()
    {
        // Проверяем два условия через И (&&):
        // 1. Keyboard.current.spaceKey.wasPressedThisFrame - была ли нажата пробел ЭТОТ КАДР
        // 2. isGrounded - стоит ли игрок на земле
        if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
        {
            // Прыжок: добавляем вертикальную скорость
            // rb.linearVelocity - текущая скорость объекта
            // Vector3.up * jumpForce - вектор вверх, умноженный на силу прыжка
            // Пример: если jumpForce = 5, то добавляем (0, 5, 0) к скорости
            rb.linearVelocity += Vector3.up * jumpForce;

            // Устанавливаем, что игрок больше не на земле
            // Это предотвращает двойные прыжки в воздухе
            isGrounded = false;

            // Выводим сообщение в консоль Unity для отладки
            Debug.Log("ПРЫЖОК"); // Можно посмотреть в окне Console при запуске игры
        }
    }

    // ================ МЕТОДЫ СТОЛКНОВЕНИЙ (КОЛЛИЗИЙ) ================

    // OnCollisionEnter() - вызывается, когда объект СТАЛКИВАЕТСЯ с другим объектом
    // Collision collision - информация о столкновении (с каким объектом, сила и т.д.)
    private void OnCollisionEnter(Collision collision)
    {
        // CompareTag("Ground") - проверяет, есть ли у объекта тег "Ground"
        // Теги (Tags) - это как наклейки на объектах, чтобы их различать
        // Мы назначили тег "Ground" полу в Unity
        if (collision.gameObject.CompareTag("Ground"))
        {
            // Если столкнулись с землей - значит мы на земле!
            isGrounded = true;

            Debug.Log("ПРИЗЕМЛИЛСЯ"); // Отладочное сообщение
        }
    }

    // ДОПОЛНИТЕЛЬНО: можно добавить еще методы для лучшего контроля

    // OnCollisionStay() - вызывается КАЖДЫЙ КАДР, пока объект соприкасается с другим
    private void OnCollisionStay(Collision collision)
    {
        // Если стоим на земле - постоянно обновляем isGrounded
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    // OnCollisionExit() - вызывается, когда объект ПЕРЕСТАЕТ соприкасаться
    private void OnCollisionExit(Collision collision)
    {
        // Если ушли от земли - мы еще не обязательно в воздухе
        // (можем переходить на другую платформу)
        // Поэтому здесь ничего не делаем, CheckGround() проверит в Update
    }

    // ================ ВИЗУАЛИЗАЦИЯ ДЛЯ ОТЛАДКИ ================

    // OnDrawGizmos() - рисует вспомогательные графики в окне Scene (только в редакторе)
    private void OnDrawGizmos()
    {
        // Рисуем луч от игрока вниз для визуализации проверки земли
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * 0.2f);

        // Рисуем стрелку направления движения
        Gizmos.color = Color.blue;
        Vector3 direction = new Vector3(moveInput.x, 0, moveInput.y);
        if (direction.magnitude > 0.1f)
        {
            Gizmos.DrawRay(transform.position, direction.normalized * 2f);
        }
    }

    // ================ ПРОСТОЙ ИНТЕРФЕЙС В ИГРЕ ================

    // OnGUI() - рисует интерфейс поверх игры (устаревший способ, но простой для новичка)
    //private void OnGUI()
    //{
    //    // Создаем прямоугольник в левом верхнем углу
    //    Rect rect = new Rect(10, 10, 200, 100);

    //    // Рисуем черный фон
    //    GUI.Box(rect, "");

    //    // Выводим информацию
    //    GUI.Label(new Rect(15, 15, 190, 20), "Управление: WASD + SPACE");
    //    GUI.Label(new Rect(15, 35, 190, 20), $"На земле: {isGrounded}");
    //    GUI.Label(new Rect(15, 55, 190, 20), $"Скорость: {rb.linearVelocity.magnitude:F1}");
    //}
}

// ================ КРАТКИЙ СЛОВАРИК ТЕРМИНОВ ================
/*
  Vector2/Vector3 - математический вектор (направление + длина)
  Quaternion - способ хранения поворота в 3D (сложная математика, но Unity упрощает)
  Rigidbody - компонент физики (подчиняется гравитации, сталкивается)
  Collider - компонент, который определяет форму для столкновений
  Update() - вызывается каждый кадр (для ввода, анимаций)
  FixedUpdate() - вызывается фиксированно (для физики)
  Time.deltaTime - время между кадрами (делает движение плавным при любом FPS)
  Debug.Log() - выводит сообщение в консоль Unity
  public/private - область видимости (public видно в инспекторе Unity)
*/