using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Runtime.InteropServices;

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
    public string attackActionName = "Attack";
    public string interactActionName = "Interact";
    public string nextActionName = "Next";
    public string previousActionName = "Previous";
    public string sprintActionName = "Sprint";
    public string crouchActionName = "Crouch";

    [Header("Interaction")]
    [SerializeField] private IInteractable objectToInteract;
    // internals
    CharacterController _cc;
    PlayerInput _playerInput;

    public Animator animatorController;

    InputAction _move;
    InputAction _look;
    InputAction _attack;
    InputAction _interact;
    InputAction _next;
    InputAction _previous;
    InputAction _sprint;
    InputAction _crouch;

    Vector3 _planarVelocity;
    float _verticalVelocity;
    bool _isSprinting;
    bool _isCrouched;
    float _pitch;
    float _targetHeight;

    [Header("Animations")]
    [SerializeField] private AnimationClip idling;
    [SerializeField] private AnimationClip walking;
    [SerializeField] private AnimationClip running;
    [SerializeField] private AnimationClip crouching;
    [SerializeField] private AnimationClip crouchWalking;
    [SerializeField] private List<AttackAnimation> attackAnimations;
    int attackCounter;
    bool isAttacking;
    bool inCombatTiming;
    bool isDead;
    Coroutine attackCoroutine;
    Coroutine combatTimingCoroutine;
    [SerializeField] private DamageCollider damageCollider;

    void Awake()
    {
        _instance = this;
        _cc = GetComponent<CharacterController>();
        _playerInput = GetComponent<PlayerInput>();

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
        _next = map.FindAction(nextActionName, true);
        _previous = map.FindAction(previousActionName, true);
        _sprint = map.FindAction(sprintActionName, true);
        _crouch = map.FindAction(crouchActionName, true);

        _targetHeight = standHeight;
        if (_cc != null) _cc.height = standHeight;
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
        if (isDead || isAttacking) return;

        Vector2 move = _move.ReadValue<Vector2>();
        if (move == Vector2.zero)
        {
            //Vector3 y = _planarVelocity + Vector3.up * _verticalVelocity;
            //_cc.Move(y);
            if (!_isCrouched)
                animatorController.Play(idling.name);
                
            else
                animatorController.Play(crouching.name);
            return;
        }
        // Базовые оси камеры по земле
        Vector3 fwd = Vector3.forward;
        Vector3 right = Vector3.right;

        if (cameraTransform != null)
        {
            fwd = cameraTransform.forward; fwd.y = 0f; fwd.Normalize();
            right = cameraTransform.right; right.y = 0f; right.Normalize();
        }

        Vector3 desired = (fwd * move.y + right * move.x);
        float targetSpeed;
        if (!_isCrouched)
        {
            if (!_isSprinting)
            {
                animatorController.Play(walking.name);
                targetSpeed = walkSpeed;
            }

            else
            {
                animatorController.Play(running.name);
                targetSpeed = sprintSpeed;
            }

        }
        else
        {
            animatorController.Play(crouchWalking.name);
            targetSpeed = crouchSpeed;
        }

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
    }
    // Attack
    void OnAttackPerformed(InputAction.CallbackContext _)
    {
        HandleAttack();
    }
    void HandleAttack()
    {
        if (isAttacking && !inCombatTiming) return;

        SetCrouch(false);
        if (inCombatTiming)
        {
            StopCoroutine(combatTimingCoroutine);
            StopCoroutine(attackCoroutine);
            inCombatTiming = false;
            attackCounter++;
        }

        isAttacking = true;
        attackCoroutine = StartCoroutine(AttackRoutine());
    }
    
    IEnumerator AttackRoutine()
    {
        var animation = attackAnimations[attackCounter % attackAnimations.Count];
        animatorController.applyRootMotion = true;
        animatorController.Play(animation.clip.name);
        combatTimingCoroutine = StartCoroutine(CombatTimingRoutine(animation));
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(animation.clip.length);
         animatorController.applyRootMotion = false;
        isAttacking = false;
        attackCounter = 0;
        Debug.Log("attack is over");
    }

    IEnumerator CombatTimingRoutine(AttackAnimation attackAnimation)
    {
        yield return new WaitForSeconds(attackAnimation.timingStart);
        inCombatTiming = true;
        Debug.Log("Timing is Started");
        var delta = attackAnimation.timingEnd - attackAnimation.timingStart;
        yield return new WaitForSeconds(delta);
        inCombatTiming = false;
        Debug.Log("Timing is Over");
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
        c.y = _cc.height * 0.5f;
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
        _cc.enabled = false;
        DisableControls();
        if (preMoveAction != null)
        {
            yield return StartCoroutine(preMoveAction);
        }
        animatorController.applyRootMotion = true;
        animatorController.Play(interactionClip.name);
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(interactionClip.length);
        _cc.enabled = true;
        EnableControls();
    }

    public void Die()
    {
        isDead = true;
        DisableControls();
    }
}
