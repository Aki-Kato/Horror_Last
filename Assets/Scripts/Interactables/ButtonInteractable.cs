using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ButtonInteractable : InteractableTrigger
{
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string pressTriggerName = "Press";

    [Tooltip("Если true — событие вызывается из Animation Event, а не сразу.")]
    [SerializeField] private bool useAnimationEvent = false;

    [Header("Logic")]
    [Tooltip("Можно ли нажимать кнопку много раз (в целом).")]
    [SerializeField] private bool canPressMultipleTimes = true;

    [Tooltip("Событие, которое произойдет сразу при нажатии кнопки.")]
    [SerializeField] private UnityEvent onPressed;

    private bool hasBeenPressed = false;

    // === Таймер ===
    [Header("Timer Settings")]
    [Tooltip("Включить ли таймер.")]
    [SerializeField] private bool useTimer = false;

    [Tooltip("Через сколько секунд после нажатия вызвать событие.")]
    [SerializeField] private float timerDelay = 2f;

    [Tooltip("Событие, вызываемое таймером (после задержки).")]
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
        // Если кнопка одноразовая и уже жали — выходим
        if (!canPressMultipleTimes && hasBeenPressed)
            return false;

        // Если есть таймер и он сейчас работает — блокируем повторное нажатие
        if (useTimer && timerRoutine != null)
            return false;

        hasBeenPressed = true;

        // 1) Анимация
        if (animator != null && !string.IsNullOrEmpty(pressTriggerName))
        {
            animator.SetTrigger(pressTriggerName);
        }

        // 2) Основное событие
        if (!useAnimationEvent)
        {
            InvokePressedEvent();
        }

        // 3) Запустить таймер (если включен)
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

    // --- Таймер ----------------------------------------------------

    private void StartTimer()
    {
        // раз таймер = защита, то пока он идёт, повторно мы сюда даже не попадём
        if (timerRoutine != null)
            return;

        timerRoutine = StartCoroutine(TimerCoroutine());
    }

    private IEnumerator TimerCoroutine()
    {
        yield return new WaitForSeconds(timerDelay);

        onTimer?.Invoke();

        // таймер отработал — можно снова жать кнопку
        timerRoutine = null;
    }

    // --- Animation Event --------------------------------------------

    public void AnimationEvent_TriggerButton()
    {
        if (!useAnimationEvent)
            return;

        InvokePressedEvent();
    }
}
