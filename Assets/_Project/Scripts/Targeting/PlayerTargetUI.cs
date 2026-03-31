using TMPro;
using UnityEngine;

// PlayerTargetUI
// Этот скрипт отвечает ТОЛЬКО за показ информации о цели в UI.
//
// Важно:
// - он НЕ выбирает цель
// - он НЕ решает сложные игровые правила
// - он НЕ знает, как устроен враг внутри
//
// Он просто:
// 1. спрашивает у TargetDisplayResolver, что сейчас можно показать
// 2. выводит это на экран
//
// То есть TargetDisplayResolver решает "ЧТО можно показать",
// а PlayerTargetUI решает "КАК это нарисовать".
public class PlayerTargetUI : MonoBehaviour
{
    [Header("Ссылка на resolver показа")]
    [SerializeField] private TargetDisplayResolver targetDisplayResolver;
    // Сюда нужен TargetDisplayResolver, который висит на Player

    [Header("Текст имени цели")]
    [SerializeField] private TMP_Text targetNameText;
    // Это наш TMP Text на Canvas

    [Header("Текст перед именем цели")]
    [SerializeField] private string targetPrefix = "Цель: ";
    // Позже можно заменить или убрать

    [Header("Настройки")]
    [SerializeField] private bool hideWhenNoTarget = true;
    // true = если цели нет, текст полностью скрывается

    private void Awake()
    {
        // Если resolver не назначили вручную —
        // ищем автоматически
        if (targetDisplayResolver == null)
            targetDisplayResolver = FindFirstObjectByType<TargetDisplayResolver>();

        RefreshTargetUI();
    }

    private void Update()
    {
        // Для MVP обновляем UI каждый кадр.
        // Для одного текста это нормально и очень просто.
        RefreshTargetUI();
    }

    private void RefreshTargetUI()
    {
        if (targetNameText == null)
            return;

        if (targetDisplayResolver == null)
        {
            HideTargetText();
            return;
        }

        bool hasData = targetDisplayResolver.TryGetDisplayData(
            out string displayName,
            out bool showHealth,
            out int currentHealth,
            out int maxHealth);

        // showHealth/currentHealth/maxHealth пока не используем,
        // но уже принимаем их как часть фундамента.
        // Позже сюда удобно добавить HP-бар и цифры.

        if (!hasData || string.IsNullOrWhiteSpace(displayName))
        {
            HideTargetText();
            return;
        }

        targetNameText.gameObject.SetActive(true);
        targetNameText.text = targetPrefix + displayName;
    }

    private void HideTargetText()
    {
        if (targetNameText == null)
            return;

        if (hideWhenNoTarget)
        {
            targetNameText.gameObject.SetActive(false);
        }
        else
        {
            targetNameText.gameObject.SetActive(true);
            targetNameText.text = "";
        }
    }
}