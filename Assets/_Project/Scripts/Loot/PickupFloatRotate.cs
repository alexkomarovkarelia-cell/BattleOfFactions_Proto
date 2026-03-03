using UnityEngine;

// Вешаем на Visual (дочерний объект лута), а НЕ на root.
// Root остаётся на месте (коллайдер/триггер не прыгает),
// а "красота" двигается отдельно.
public class PickupFloatRotate : MonoBehaviour
{
    [Header("Rotation (degrees per second)")]
    public Vector3 rotationSpeed = new Vector3(0f, 120f, 0f);

    [Header("Bobbing (up/down)")]
    public float bobHeight = 0.12f; // высота подпрыгивания
    public float bobSpeed = 2.0f;   // скорость подпрыгивания

    private Vector3 startLocalPos;

    private void Awake()
    {
        startLocalPos = transform.localPosition;
    }

    private void Update()
    {
        // Вращение
        transform.Rotate(rotationSpeed * Time.deltaTime, Space.Self);

        // Подпрыгивание
        float yOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.localPosition = startLocalPos + Vector3.up * yOffset;
    }
}
