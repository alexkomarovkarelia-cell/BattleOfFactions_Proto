using UnityEngine;

// SimpleInteractable
// Это ПРОСТЕЙШИЙ тестовый объект для взаимодействия.
//
// Что делает:
// - когда игрок вызывает Interact()
// - объект пишет сообщение в Console
//
// Зачем нужен:
// - проверить, что система взаимодействия вообще работает
// - не строить пока двери, сундуки и NPC
public class SimpleInteractable : MonoBehaviour
{
    [Header("Текст для проверки")]
    [SerializeField] private string debugMessage = "Взаимодействие с объектом сработало!";

    [Header("Дополнительно")]
    [SerializeField] private bool changeColorOnInteract = true;
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Color interactedColor = Color.green;

    public void Interact()
    {
        Debug.Log(debugMessage);

        // Для наглядности можем ещё и менять цвет объекта,
        // чтобы было видно не только сообщение в Console
        if (changeColorOnInteract && targetRenderer != null)
        {
            targetRenderer.material.color = interactedColor;
        }
    }

    private void Reset()
    {
        // Автоматически пробуем найти Renderer на этом объекте
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();
    }
}