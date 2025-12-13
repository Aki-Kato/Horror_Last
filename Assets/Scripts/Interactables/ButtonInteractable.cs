using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ButtonInteractable : InteractableTrigger
{
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string pressTriggerName = "Press";
    [SerializeField] private bool useAnimationEvent = false;

    [Header("Logic")]
    [SerializeField] private bool canPressMultipleTimes = true;
    [SerializeField] private UnityEvent onPressed;

    private bool hasBeenPressed = false;

    // === Требуемый предмет ===
    [Header("Required Item (Optional)")]
    [Tooltip("Если назначено — кнопка работает только при наличии предмета.")]
    [SerializeField] private Item requiredItem;

    [Tooltip("Событие, если предмет отсутствует.")]
    [SerializeField] private UnityEvent onFail;

    // === Таймер ===
    [Header("Timer Settings")]
    [SerializeField] private bool useTimer = false;
    [SerializeField] private float timerDelay = 2f;
    [SerializeField] private UnityEvent onTimer;

    private Coroutine timerRoutine;

    // ------------------------------------------------------

    protected override void Start()
    {
        base.Start();
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public override bool Interact()
    {
        // --- 1. Проверка на предмет ---
        if (requiredItem != null)
        {
            // Если нет инвентаря или нужного предмета
            if (Inventory.Instance == null || !Inventory.Instance.Contains(requiredItem))
            {
                Debug.Log($"{name}: Не хватает предмета {requiredItem.itemName} для активации!");

                // ВЫЗЫВАЕМ СОБЫТИЕ ОТКАЗА
                onFail?.Invoke();

                return false;
            }
        }

        // --- 2. Одноразовая кнопка ---
        if (!canPressMultipleTimes && hasBeenPressed)
            return false;

        // --- 3. Таймер в процессе — запрещаем нажимать ---
        if (useTimer && timerRoutine != null)
            return false;

        hasBeenPressed = true;

        // --- 4. Анимация ---
        if (animator != null && !string.IsNullOrEmpty(pressTriggerName))
        {
            animator.SetTrigger(pressTriggerName);
        }

        // --- 5. Основное событие ---
        if (!useAnimationEvent)
        {
            InvokePressedEvent();
        }

        // --- 6. Таймер ---
        if (useTimer)
        {
            StartTimer();
        }

        return true;
    }

    private void InvokePressedEvent()
    {
        onPressed?.Invoke();
    }

    // --- Таймер ---
    private void StartTimer()
    {
        if (timerRoutine != null)
            return;

        timerRoutine = StartCoroutine(TimerCoroutine());
    }

    private IEnumerator TimerCoroutine()
    {
        yield return new WaitForSeconds(timerDelay);

        onTimer?.Invoke();

        timerRoutine = null; // кнопка снова активна
    }

    // --- Animation Event ---
    public void AnimationEvent_TriggerButton()
    {
        if (!useAnimationEvent)
            return;

        InvokePressedEvent();
    }
}
