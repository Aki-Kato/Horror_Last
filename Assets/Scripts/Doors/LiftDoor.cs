using System.Collections;
using UnityEngine;

public class LiftDoor : MonoBehaviour
{
    public enum DoorMode
    {
        Animation,
        Transform
    }

    [Header("Режим работы")]
    [SerializeField] private DoorMode mode = DoorMode.Transform;

    [Header("Общие настройки")]
    [SerializeField] private bool startOpen = false;
    [SerializeField] private bool toggleOnRequest = true; // если true — Toggle() будет открывать/закрывать

    private bool isOpen;

    [Header("Анимация")]
    [SerializeField] private Animator animator;
    [Tooltip("Триггер для открытия (если не используешь bool).")]
    [SerializeField] private string openTriggerName = "Open";
    [Tooltip("Триггер для закрытия (если не используешь bool).")]
    [SerializeField] private string closeTriggerName = "Close";
    [Tooltip("Опционально: bool-параметр вместо триггеров. Если не пустой — используется он.")]
    [SerializeField] private string openBoolName = "";

    [Header("Трансформ движение")]
    [Tooltip("Что двигать (дверное полотно). Если не задано — этот объект.")]
    [SerializeField] private Transform doorTransform;
    [Tooltip("На сколько юнитов вверх поднимается дверь.")]
    [SerializeField] private float openHeight = 3f;
    [Tooltip("Время открытия/закрытия в секундах.")]
    [SerializeField] private float moveDuration = 1f;
    [Tooltip("Кривая движения. По умолчанию плавное EaseInOut.")]
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector3 closedPosition;
    private Vector3 openPosition;
    private Coroutine moveRoutine;

    private void Awake()
    {
        if (doorTransform == null)
            doorTransform = transform;

        closedPosition = doorTransform.position;
        openPosition = closedPosition + Vector3.up * openHeight;

        if (mode == DoorMode.Animation && animator == null)
            animator = GetComponent<Animator>();

        isOpen = startOpen;

        if (mode == DoorMode.Transform)
        {
            doorTransform.position = isOpen ? openPosition : closedPosition;
        }
        else if (mode == DoorMode.Animation)
        {
            if (!string.IsNullOrEmpty(openBoolName) && animator != null)
            {
                animator.SetBool(openBoolName, isOpen);
            }
        }
    }

    // === Публичные методы для кнопки ===

    public void Toggle()
    {
        if (!toggleOnRequest)
        {
            Open();
            return;
        }

        if (isOpen)
            Close();
        else
            Open();
    }

    public void Open()
    {
        if (isOpen && mode == DoorMode.Transform)
        {
            // уже открыта и никуда не едет
            if (moveRoutine == null)
                return;
            // если едет — пусть доезжает в сторону открытия
        }

        isOpen = true;

        if (mode == DoorMode.Transform)
        {
            StartMove(openPosition); // <-- теперь всегда от ТЕКУЩЕЙ позиции
        }
        else
        {
            PlayAnimation(true);
        }
    }

    public void Close()
    {
        if (!isOpen && mode == DoorMode.Transform)
        {
            if (moveRoutine == null)
                return;
        }

        isOpen = false;

        if (mode == DoorMode.Transform)
        {
            StartMove(closedPosition); // <-- тоже от текущей позиции
        }
        else
        {
            PlayAnimation(false);
        }
    }

    // === Внутренняя логика движения ===

    private void StartMove(Vector3 targetPosition)
    {
        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(MoveDoor(targetPosition));
    }

    private IEnumerator MoveDoor(Vector3 targetPosition)
    {
        Vector3 startPosition = doorTransform.position; // <-- ВАЖНО: запоминаем текущую!
        float time = 0f;

        while (time < moveDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / moveDuration);
            float eval = moveCurve.Evaluate(t);
            doorTransform.position = Vector3.LerpUnclamped(startPosition, targetPosition, eval);
            yield return null;
        }

        doorTransform.position = targetPosition;
        moveRoutine = null;
    }

    private void PlayAnimation(bool open)
    {
        if (animator == null) return;

        if (!string.IsNullOrEmpty(openBoolName))
        {
            animator.SetBool(openBoolName, open);
            return;
        }

        if (open && !string.IsNullOrEmpty(openTriggerName))
        {
            animator.SetTrigger(openTriggerName);
        }
        else if (!open && !string.IsNullOrEmpty(closeTriggerName))
        {
            animator.SetTrigger(closeTriggerName);
        }
    }
}
