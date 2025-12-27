using UnityEngine;

public sealed class DodgeStateBehaviour : StateMachineBehaviour
{
    [Header("Early cancel")]
    [Tooltip("С какого normalizedTime (0..1) можно отменять додж в движение")]
    [Range(0f, 1f)]
    [SerializeField] private float cancelMoveStart = 0.35f;

    [Tooltip("Сбрасывать ли MoveX/MoveY при выходе из стейта")]
    [SerializeField] private bool resetMoveOnExit = true;

    private PlayerController _pc;
    private Animator _animator;
    private bool _canCancel;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _animator = animator;
        _pc ??= animator.GetComponent<PlayerController>();

        _canCancel = false;
        _pc.BeginDodge();
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        float t = stateInfo.normalizedTime;

        // Окно отмены
        if (!_canCancel && t >= cancelMoveStart)
            _canCancel = true;

        if (!_canCancel)
            return;

        // Проверяем ввод движения
        Vector2 move = _pc.GetMoveInput(); // 👈 см. ниже

        if (move != Vector2.zero)
        {
            // Прерываем додж
            _animator.SetTrigger(AnimatorParameters.ExitDodge);
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _pc.EndDodge();

        if (resetMoveOnExit)
        {
            animator.SetFloat(AnimatorParameters.X, 0f);
            animator.SetFloat(AnimatorParameters.Y, 0f);
        }
    }
}
