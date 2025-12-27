using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;
using System.Collections;
using Random = UnityEngine.Random;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance => _instance;
    private static PlayerController _instance;
    [Header("Refs")]
    [Tooltip("Куда смотрит камера. Вектор движения проецируется по этому Transform.")]
    public Transform cameraTransform;
    [Tooltip("Опционально: узел-пивот камеры для вращения мышью/стиком по действию Look.")]
    public Transform cameraPivot;
    public bool useInternalCameraLook = false;
    [Header("Battle Control")]
    [SerializeField] bool isBattleMode = false;
    [SerializeField] bool isTakingWeapon = false;
    [SerializeField] private int weaponLayerIndex = 1;
    [SerializeField] private bool _pendingToggle;
    [SerializeField] private string weaponEmptyStateName = "EmptyState";
    [SerializeField] private string takeWeaponStateName = "Take_Weapon";
    [SerializeField] private string holsterWeaponStateName = "Take_Weapon 0";

    [Header("Movement")]
    public float walkSpeed = 3.5f;
    public float sprintSpeed = 6.5f;
    public float acceleration = 12f;
    public float rotationSpeed = 720f;
    public float gravity = -20f;
    public bool isGrounded = true;
    public float checkGroundRadius;
    public float checkGroundDistance;
    public Transform checkgroundPosition;
    [SerializeField] private LayerMask groundLayer;

    [Header("Crouch")]
    public float crouchSpeed = 2.0f;
    public float standHeight = 1.8f;
    public float crouchHeight = 1.0f;
    public float heightLerpSpeed = 12f;

    [Header("Look")]
    public float lookSensitivity = 150f;
    public float minPitch = -60f;
    public float maxPitch = 75f;


    [Header("Input Map/Action Names")]
    public string actionMapName = "Player";
    public string moveActionName = "Move";
    public string lookActionName = "Look";
    public string takeWeaponActionName = "TakeWeapon";
    public string attackActionName = "Attack";
    public string interactActionName = "Interact";
    public string nextActionName = "Next";
    public string previousActionName = "Previous";
    public string sprintActionName = "Sprint";
    public string crouchActionName = "Crouch";
    public string dodgeActionName = "Dodge";

    [Header("Interaction")]
    [SerializeField] private IInteractable objectToInteract;

    CharacterController _cc;
    PlayerInput _playerInput;

    public Animator animatorController;

    InputAction _move;
    InputAction _look;
    InputAction _attack;
    InputAction _interact;
    InputAction _takeWeapon;
    InputAction _next;
    InputAction _previous;
    InputAction _sprint;
    InputAction _crouch;
    InputAction _dodge;

    Vector3 _planarVelocity;
    float _verticalVelocity;
    bool _isSprinting;
    bool _isCrouched;
    float _pitch;
    float _targetHeight;
    [SerializeField] private bool isDodging;

    [Header("Animations")]
    [SerializeField] private List<AttackAnimation> attackAnimations;
    bool isAttacking;
    bool inCombatTiming;
    bool wantNextAttack;
    bool isDead;

    [SerializeField] private DamageCollider damageCollider;

    void Awake()
    {
        _instance = this;
        _cc = GetComponent<CharacterController>();
        _playerInput = GetComponent<PlayerInput>();
        LockCursor();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (_playerInput.actions == null)
        {
            Debug.LogError("PlayerInput должен ссылаться на ваш Input Actions asset.");
            enabled = false;
            return;
        }

        var map = _playerInput.actions.FindActionMap(actionMapName, true);

        _move = map.FindAction(moveActionName, true);
        _look = map.FindAction(lookActionName, true);
        _attack = map.FindAction(attackActionName, true);
        _interact = map.FindAction(interactActionName, true);
        _takeWeapon = map.FindAction(takeWeaponActionName, true);
        _next = map.FindAction(nextActionName, true);
        _previous = map.FindAction(previousActionName, true);
        _sprint = map.FindAction(sprintActionName, true);
        _crouch = map.FindAction(crouchActionName, true);
        _dodge = map.FindAction(dodgeActionName, true);

        _targetHeight = standHeight;
        if (_cc != null) _cc.height = standHeight;


    }
    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    void OnEnable()
    {
        EnableControls();
    }

    void OnDisable()
    {
        DisableControls();
    }

    void Update()
    {
        HandleLook();
        if (!_cc.enabled) return;
        HandleMove();

        HandleGravity();
        HandleCrouchHeight();
    }

    void HandleMove()
    {
        if (isDead || isDodging) return;

        Vector2 move = _move.ReadValue<Vector2>();
        if (isAttacking)
        {
            // Движение во время атаки запрещено
            // Исключение — combat timing, где движение = ОТМЕНА атаки
            if (inCombatTiming && move != Vector2.zero)
            {
                animatorController.SetBool(AnimatorParameters.Attack, false);
            }

            animatorController.SetFloat(AnimatorParameters.Speed, 0f);
            return;
        }

        SetAnimatorMove(move.x, move.y);
        if (move == Vector2.zero)
        {
            animatorController.SetFloat(AnimatorParameters.Speed, 0);
            return;
        }
        Action<Vector2> handleMove = isBattleMode ? HandleBattleMove : HandleNormalMove;
        handleMove(move);
    }
    public Vector2 GetMoveInput()
    {
        return _move.ReadValue<Vector2>();
    }
    void HandleNormalMove(Vector2 move)
    {

        // Базовые оси камеры по земле
        Vector3 fwd = Vector3.forward;
        Vector3 right = Vector3.right;

        if (cameraTransform != null)
        {
            fwd = cameraTransform.forward;
            fwd.y = 0f;
            fwd.Normalize();
            right = cameraTransform.right;
            right.y = 0f;
            right.Normalize();
        }

        Vector3 desired = (fwd * move.y + right * move.x);
        float targetSpeed;
        if (!_isCrouched)
        {
            if (!_isSprinting)
            {
                targetSpeed = walkSpeed;
            }

            else
            {
                targetSpeed = sprintSpeed;
            }

        }
        else
        {
            targetSpeed = crouchSpeed;
        }

        animatorController.SetFloat(AnimatorParameters.Speed, targetSpeed);

        Vector3 desiredVelocity = desired * targetSpeed;

        // Плавное ускорение/торможение
        _planarVelocity = Vector3.MoveTowards(_planarVelocity, desiredVelocity, acceleration * Time.deltaTime);

        // Поворот в сторону движения
        Vector3 planar = new Vector3(_planarVelocity.x, 0f, _planarVelocity.z);
        if (planar.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(planar.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        // Применяем движение (плюс вертикаль из гравитации в HandleGravity)
        Vector3 velocity = _planarVelocity;
        _cc.Move(velocity * Time.deltaTime);
    }

    void HandleBattleMove(Vector2 move)
    {
        // Базовые оси камеры по земле
        Vector3 fwd = Vector3.forward;
        Vector3 right = Vector3.right;

        if (cameraTransform != null)
        {
            fwd = cameraTransform.forward;
            fwd.y = 0f;
            fwd.Normalize();

            right = cameraTransform.right;
            right.y = 0f;
            right.Normalize();
        }

        // Движение относительно камеры (как у тебя)
        Vector3 desired = (fwd * move.y + right * move.x);


        animatorController.SetFloat(AnimatorParameters.Speed, walkSpeed);

        Vector3 desiredVelocity = desired * walkSpeed;

        // Плавное ускорение/торможение
        _planarVelocity = Vector3.MoveTowards(_planarVelocity, desiredVelocity, acceleration * Time.deltaTime);

        // Поворот: ВСЕГДА к yaw камеры (в боевом режиме)
        if (cameraTransform != null)
        {
            Vector3 camFwd = cameraTransform.forward;
            camFwd.y = 0f;

            if (camFwd.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(camFwd.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }
        }

        // Движение
        Vector3 velocity = _planarVelocity;
        _cc.Move(velocity * Time.deltaTime);

    }
    // Look (вращение пивота камеры, если включено)
    void HandleLook()
    {
        if (!useInternalCameraLook || cameraPivot == null) return;

        Vector2 look = _look.ReadValue<Vector2>();
        float yawDelta = look.x * lookSensitivity * Time.deltaTime;
        float pitchDelta = -look.y * lookSensitivity * Time.deltaTime;

        _pitch = Mathf.Clamp(_pitch + pitchDelta, minPitch, maxPitch);

        // yaw на пивоте-родителе (или на персонаже, если хотите)
        cameraPivot.parent.Rotate(0f, yawDelta, 0f, Space.Self);

        // pitch на самом пивоте
        cameraPivot.localEulerAngles = new Vector3(_pitch, 0f, 0f);
    }

    // Sprint (удержание)
    void OnSprintPerformed(InputAction.CallbackContext _)
    {
        _isSprinting = true;
    }
    void OnSprintCanceled(InputAction.CallbackContext _)
    {
        _isSprinting = false;
    }

    // Crouch (тоггл)
    void OnCrouchPerformed(InputAction.CallbackContext _)
    {
        if (isAttacking) return;

        SetCrouch(!_isCrouched);
    }
    void SetCrouch(bool isCrouched)
    {
        _isCrouched = isCrouched;
        _targetHeight = _isCrouched ? crouchHeight : standHeight;
        animatorController.SetBool(AnimatorParameters.Crouch, isCrouched);
    }

    // WeaponMode
    void OnTakeWeaponPerformed(InputAction.CallbackContext _)
{
    if (isTakingWeapon || isAttacking)
        return;

    isTakingWeapon = true;

    animatorController.SetLayerWeight(weaponLayerIndex, 1f);

    // ВАЖНО: смотрим ТЕКУЩЕЕ состояние, но НЕ МЕНЯЕМ его
    bool take = !isBattleMode;

    string clip = take ? "Take_Weapon" : "Take_Weapon 0";
    animatorController.Play(clip, weaponLayerIndex, 0f);
}
   public void EndTakeWeapon()
{
    // ТОЛЬКО ЗДЕСЬ меняем состояние
    isBattleMode = !isBattleMode;
    animatorController.SetBool(AnimatorParameters.Weapon, isBattleMode);

    animatorController.SetLayerWeight(weaponLayerIndex, 0f);
    isTakingWeapon = false;
}

    public void SetTakingWeapon(bool value)
    {
        isTakingWeapon = value;
    }

    public void SetCombatTiming(bool value)
    {
        inCombatTiming = value;
    }

    public void SetAttackState(bool value)
    {
        isAttacking = value;
    }

    void OnAttackPerformed(InputAction.CallbackContext _)
    {
        if (!isBattleMode) return;

        if (!isAttacking)
        {
            StartAttack();
            return;
        }

        if (inCombatTiming)
        {
            wantNextAttack = true;
        }
    }

    public bool ConsumeNextAttackRequest()
    {
        if (!wantNextAttack) return false;
        wantNextAttack = false;
        return true;
    }

    public void StartAttack()
    {
        if (animatorController.GetBool(AnimatorParameters.Attack))
            return; // защита от повторного старта

        Vector2 move = _move.ReadValue<Vector2>();
        if (move == Vector2.zero)
            move = Vector2.up;

        SetAnimatorMove(move.x, move.y);

        if (cameraTransform != null)
        {
            Vector3 fwd = cameraTransform.forward;
            fwd.y = 0f;

            if (fwd.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(fwd.normalized, Vector3.up);
        }

        int id = Random.Range(0, attackAnimations.Count);
        animatorController.SetInteger(AnimatorParameters.AttackId, id);
        animatorController.SetBool(AnimatorParameters.Attack, true);
    }



    public void BeginDodge()
    {
        DisableControls();
        isDodging = true;
        animatorController.applyRootMotion = true;
    }

    public void EndDodge()
    {
        animatorController.applyRootMotion = false;
        isDodging = false;
        SetAnimatorMove(0f, 0f);
        EnableControls();
    }

    private void OnDodgePerformed(InputAction.CallbackContext _)
    {
        if (!isBattleMode) return;
        if (isDodging) return; // защита от спама

        Vector2 move = _move.ReadValue<Vector2>();

        // если стоим — по твоей логике додж назад
        if (move == Vector2.zero) move.y = -1f;

        SetAnimatorMove(move.x, move.y);

        // лучше Trigger, чем Bool для одноразового действия
        animatorController.ResetTrigger(AnimatorParameters.DodgeTrigger);
        animatorController.SetTrigger(AnimatorParameters.DodgeTrigger);
    }

    void SetAnimatorMove(float x, float y)
    {
        animatorController.SetFloat(AnimatorParameters.X, x);
        animatorController.SetFloat(AnimatorParameters.Y, y);
    }
    // Interact
    void OnInteractPerformed(InputAction.CallbackContext _)
    {
        if (isDead) return;

        HandleInteract();
    }
    void HandleInteract()
    {
        if (objectToInteract == null) return;

        objectToInteract.Interact();
    }

    // Next / Previous (например, смена оружия/инвентаря)
    void OnNextPerformed(InputAction.CallbackContext _)
    {
        HandleNext();
    }
    void OnPreviousPerformed(InputAction.CallbackContext _)
    {
        HandlePrevious();
    }
    void HandleNext()
    {
        Debug.Log("Next item/slot");
    }
    void HandlePrevious()
    {
        Debug.Log("Previous item/slot");
    }

    void CheckGrounded()
    {
        Ray ray = new Ray(checkgroundPosition.position, Vector3.down);
        isGrounded = Physics.SphereCast(ray, checkGroundRadius, out _, checkGroundDistance, groundLayer, QueryTriggerInteraction.Ignore);
    }

    void HandleGravity()
    {
        CheckGrounded();
        if (_cc.isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = -2f;

        _verticalVelocity += gravity * Time.deltaTime;
        _cc.Move(Vector3.up * _verticalVelocity * Time.deltaTime);

    }

    public void EnableDamageCollider()
    {
        damageCollider.gameObject.SetActive(true);
    }
    public void DisableDamageCollider()
    {
        damageCollider.gameObject.SetActive(false);
    }

    void HandleCrouchHeight()
    {
        if (_cc == null) return;
        _cc.height = Mathf.Lerp(_cc.height, _targetHeight, heightLerpSpeed * Time.deltaTime);
        Vector3 c = _cc.center;
        c.y = _cc.height * 0.55f;
        _cc.center = c;
    }
    void EnableControls()
    {
        _attack.performed += OnAttackPerformed;
        _interact.performed += OnInteractPerformed;
        _next.performed += OnNextPerformed;
        _previous.performed += OnPreviousPerformed;
        _sprint.performed += OnSprintPerformed;
        _sprint.canceled += OnSprintCanceled;
        _crouch.performed += OnCrouchPerformed;
        _takeWeapon.performed += OnTakeWeaponPerformed;
        _dodge.performed += OnDodgePerformed;
    }



    void DisableControls()
    {
        _attack.performed -= OnAttackPerformed;
        _interact.performed -= OnInteractPerformed;
        _next.performed -= OnNextPerformed;
        _previous.performed -= OnPreviousPerformed;
        _sprint.performed -= OnSprintPerformed;
        _sprint.canceled -= OnSprintCanceled;
        _crouch.performed -= OnCrouchPerformed;
        _takeWeapon.performed -= OnTakeWeaponPerformed;
        _dodge.performed -= OnDodgePerformed;
    }

    public void SetObjectToInteract(IInteractable interactable)
    {

        Debug.Log("ObjectChange to " + interactable);
        objectToInteract = interactable;
    }

    public void DoInteractionMove(AnimationClip interactionClip, IEnumerator preMoveAction = null)
    {
        StartCoroutine(DoInteractionMoveRoutine(interactionClip, preMoveAction));
    }
    IEnumerator DoInteractionMoveRoutine(AnimationClip interactionClip, IEnumerator preMoveAction = null)
    {
        const int vaultLayerIndex = 2;
        _cc.enabled = false;
        DisableControls();
        if (preMoveAction != null)
        {
            yield return StartCoroutine(preMoveAction);
        }

        animatorController.SetLayerWeight(vaultLayerIndex, 1);
        animatorController.applyRootMotion = true;
        animatorController.Play(interactionClip.name);
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(interactionClip.length);
        _cc.enabled = true;
        animatorController.SetLayerWeight(vaultLayerIndex, 0);
        EnableControls();
    }

    public void Die()
    {
        isDead = true;
        DisableControls();
    }
}
