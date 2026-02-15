using UnityEngine;
using UnityEngine.InputSystem;

// Камера-орбита вокруг игрока:
// - колесо: зум (с ограничениями min/max)
// - зажат ПКМ: крутим камеру (yaw/pitch)
// - отпустил ПКМ: камера возвращается "за спину" игрока
public class CameraOrbitFollow : MonoBehaviour
{
    // -----------------------------
    // НАСТРОЙКИ (Inspector)
    // -----------------------------

    [Header("Цель")]
    public Transform target;              // Игрок (за кем следим)
    public float targetHeight = 1.5f;     // Точка, куда смотрим (высота над землёй)

    [Header("Дистанция (Zoom)")]
    public float distance = 10f;          // Стартовая дистанция
    public float minDistance = 2f;        // Ближе нельзя (чтобы не залезать в модель)
    public float maxDistance = 18f;       // Дальше нельзя (ограничение обзора)
    public float zoomSpeed = 5f;          // Скорость зума колесом

    [Header("Поворот (ПКМ)")]
    public bool holdRightMouseToRotate = true; // Крутить только при зажатом ПКМ
    public float rotateSpeed = 0.2f;      // Чувствительность поворота (градусов на пиксель)
    public float minPitch = 10f;          // Ограничение вверх/вниз
    public float maxPitch = 70f;

    [Header("Возврат камеры")]
    public float defaultPitch = 35f;      // “Стандартный” наклон камеры
    public float returnSpeed = 8f;        // Насколько быстро возвращается за спину

    [Header("Плавность")]
    public float positionSmooth = 10f;    // Плавность позиции
    public float rotationSmooth = 12f;    // Плавность вращения

    // -----------------------------
    // ВНУТРЕННИЕ ПЕРЕМЕННЫЕ
    // -----------------------------
    private float desiredYaw;             // Куда хотим повернуться по горизонтали
    private float desiredPitch;           // Куда хотим по вертикали
    private float desiredDistance;        // Какая должна быть дистанция

    private float currentYaw;             // Текущее значение (с плавностью)
    private float currentPitch;
    private float currentDistance;

    private void Start()
    {
        if (target == null) return;

        // Стартовые значения: камера “за спиной”
        desiredYaw = target.eulerAngles.y;
        desiredPitch = defaultPitch;
        desiredDistance = distance;

        currentYaw = desiredYaw;
        currentPitch = desiredPitch;
        currentDistance = desiredDistance;
    }

    private void LateUpdate()
    {
        // 1) ZOOM колесиком (ограниченный)
        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.y.ReadValue(); // <-- правильнее так

            // Если колесо реально крутили
            if (Mathf.Abs(scroll) > 0.01f)
            {
                // Если значение большое (например 120) — уменьшаем влияние
                float scale = Mathf.Abs(scroll) > 10f ? 0.01f : 1f;

                desiredDistance -= scroll * zoomSpeed * scale;
                desiredDistance = Mathf.Clamp(desiredDistance, minDistance, maxDistance);

                // Временно для проверки (потом можно удалить)
                Debug.Log($"Scroll={scroll}, desiredDistance={desiredDistance}");
            }
        }


        // -----------------------------
        // 2) ВРАЩЕНИЕ при зажатом ПКМ
        // -----------------------------
        bool rotatingNow = false;

        if (Mouse.current != null)
        {
            rotatingNow = holdRightMouseToRotate
                ? Mouse.current.rightButton.isPressed
                : true;

            if (rotatingNow)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();

                // yaw — поворот вокруг игрока (влево/вправо)
                desiredYaw += delta.x * rotateSpeed;

                // pitch — вверх/вниз (инверсия обычно удобнее так)
                desiredPitch -= delta.y * rotateSpeed;
                desiredPitch = Mathf.Clamp(desiredPitch, minPitch, maxPitch);
            }
        }

        // -----------------------------
        // 3) Возврат “за спину”, когда ПКМ отпущена
        // -----------------------------
        if (!rotatingNow)
        {
            float backYaw = target.eulerAngles.y; // “за спину” = по направлению игрока

            desiredYaw = Mathf.LerpAngle(desiredYaw, backYaw, returnSpeed * Time.deltaTime);
            desiredPitch = Mathf.Lerp(desiredPitch, defaultPitch, returnSpeed * Time.deltaTime);
        }

        // -----------------------------
        // 4) ПЛАВНОСТЬ (сглаживание)
        // -----------------------------
        currentYaw = Mathf.LerpAngle(currentYaw, desiredYaw, rotationSmooth * Time.deltaTime);
        currentPitch = Mathf.Lerp(currentPitch, desiredPitch, rotationSmooth * Time.deltaTime);
        currentDistance = Mathf.Lerp(currentDistance, desiredDistance, positionSmooth * Time.deltaTime);

        // -----------------------------
        // 5) Расчёт позиции и поворота камеры
        // -----------------------------
        // Точка, вокруг которой вращаемся (центр игрока + высота)
        Vector3 targetPos = target.position + Vector3.up * targetHeight;

        // Поворот камеры по pitch/yaw
        Quaternion rot = Quaternion.Euler(currentPitch, currentYaw, 0f);

        // Смещение назад на distance
        Vector3 camOffset = rot * new Vector3(0f, 0f, -currentDistance);

        // Итоговая позиция
        Vector3 camPos = targetPos + camOffset;

        // Применяем
        transform.position = Vector3.Lerp(transform.position, camPos, positionSmooth * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, rotationSmooth * Time.deltaTime);
    }
}
