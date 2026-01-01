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
    public Transform cameraTransform;
    public Transform cameraPivot;
    public bool useInternalCameraLook = false;
    public Animator animatorController;

    [Header("Battle Control")]
    public bool isBattleMode = false;
    public bool isTakingWeapon = false;
    public int weaponLayerIndex = 1;
    [SerializeField] private DamageCollider damageCollider;
    [SerializeField] private List<AttackAnimation> attackAnimations;

    [Header("Movement")]
    public float walkSpeed = 3.5f;
    public float sprintSpeed = 6.5f;
    public float acceleration = 12f;
    public float rotationSpeed = 720f;
    public float gravity = -20f;
    public bool isGrounded = true;
    public float checkGroundRadius = 0.3f;
    public float checkGroundDistance = 0.2f;
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

    private CharacterController _cc;
    private PlayerInput _playerInput;
    private InputAction _move, _look, _attack, _interact, _takeWeapon, _next, _previous, _sprint, _crouch, _dodge;

    private Vector3 _planarVelocity;
    private float _verticalVelocity;
    private bool _isSprinting, _isCrouched, isDodging, isAttacking, inCombatTiming, isDead, wantNextAttack;
    private float _pitch, _targetHeight;
    private IInteractable objectToInteract;

    void Awake()
    {
        _instance = this;
        _cc = GetComponent<CharacterController>();
        _playerInput = GetComponent<PlayerInput>();
        _targetHeight = standHeight;
        
        SetupInputs();
        LockCursor();
        
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void SetupInputs()
    {
        var map = _playerInput.actions.FindActionMap("Player", true);
        _move = map.FindAction("Move", true);
        _look = map.FindAction("Look", true);
        _attack = map.FindAction("Attack", true);
        _interact = map.FindAction("Interact", true);
        _takeWeapon = map.FindAction("TakeWeapon", true);
        _next = map.FindAction("Next", true);
        _previous = map.FindAction("Previous", true);
        _sprint = map.FindAction("Sprint", true);
        _crouch = map.FindAction("Crouch", true);
        _dodge = map.FindAction("Dodge", true);
    }

    void Update()
    {
        if (isDead) return;
        HandleLook();
        if (!_cc.enabled) return;
        HandleGravity();
        HandleMove();
        HandleCrouchHeight();
    }

    // --- Внешние методы ---

    public Vector2 GetMoveInput() => _move.ReadValue<Vector2>();

    public void SetObjectToInteract(IInteractable interactable)
    {
        objectToInteract = interactable;
    }

    public bool ConsumeNextAttackRequest()
    {
        if (!wantNextAttack) return false;
        wantNextAttack = false;
        return true;
    }

    public void DoInteractionMove(AnimationClip interactionClip, IEnumerator preMoveAction = null)
    {
        StartCoroutine(DoInteractionMoveRoutine(interactionClip, preMoveAction));
    }

    private IEnumerator DoInteractionMoveRoutine(AnimationClip interactionClip, IEnumerator preMoveAction = null)
    {
        const int vaultLayerIndex = 2;
        _cc.enabled = false;
        DisableControls();
        if (preMoveAction != null) yield return StartCoroutine(preMoveAction);

        animatorController.SetLayerWeight(vaultLayerIndex, 1);
        animatorController.applyRootMotion = true;
        animatorController.Play(interactionClip.name);
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(interactionClip.length);
        
        _cc.enabled = true;
        animatorController.SetLayerWeight(vaultLayerIndex, 0);
        animatorController.applyRootMotion = false;
        EnableControls();
    }

    // --- Движение ---

    void HandleMove()
    {
        if (isDodging) return;
        Vector2 input = _move.ReadValue<Vector2>();

        if (isAttacking)
        {
            if (inCombatTiming && input != Vector2.zero)
                animatorController.SetBool(AnimatorParameters.Attack, false);
            
            animatorController.SetFloat(AnimatorParameters.Speed, 0f);
            return;
        }

        SetAnimatorMove(input.x, input.y);

        if (input == Vector2.zero)
        {
            _planarVelocity = Vector3.MoveTowards(_planarVelocity, Vector3.zero, acceleration * Time.deltaTime);
            animatorController.SetFloat(AnimatorParameters.Speed, 0);
        }
        else
        {
            if (isBattleMode) HandleBattleMove(input);
            else HandleNormalMove(input);
        }

        _cc.Move((_planarVelocity + Vector3.up * _verticalVelocity) * Time.deltaTime);
    }

    private Vector3 GetCameraOrientedDirection(Vector2 input)
    {
        Vector3 fwd = cameraTransform ? cameraTransform.forward : Vector3.forward;
        Vector3 right = cameraTransform ? cameraTransform.right : Vector3.right;
        fwd.y = 0; right.y = 0;
        return (fwd.normalized * input.y + right.normalized * input.x);
    }

    void HandleNormalMove(Vector2 input)
    {
        Vector3 desiredDir = GetCameraOrientedDirection(input);
        float targetSpeed = _isCrouched ? crouchSpeed : (_isSprinting ? sprintSpeed : walkSpeed);
        animatorController.SetFloat(AnimatorParameters.Speed, targetSpeed);
        _planarVelocity = Vector3.MoveTowards(_planarVelocity, desiredDir * targetSpeed, acceleration * Time.deltaTime);

        if (desiredDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(desiredDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }

    void HandleBattleMove(Vector2 input)
    {
        Vector3 desiredDir = GetCameraOrientedDirection(input);
        animatorController.SetFloat(AnimatorParameters.Speed, walkSpeed);
        _planarVelocity = Vector3.MoveTowards(_planarVelocity, desiredDir * walkSpeed, acceleration * Time.deltaTime);

        if (cameraTransform)
        {
            Vector3 camFwd = cameraTransform.forward;
            camFwd.y = 0;
            if (camFwd.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(camFwd.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }
        }
    }

    void HandleLook()
    {
        if (!useInternalCameraLook || cameraPivot == null) return;
        Vector2 look = _look.ReadValue<Vector2>();
        _pitch = Mathf.Clamp(_pitch - look.y * lookSensitivity * Time.deltaTime, minPitch, maxPitch);
        cameraPivot.parent.Rotate(0f, look.x * lookSensitivity * Time.deltaTime, 0f, Space.Self);
        cameraPivot.localEulerAngles = new Vector3(_pitch, 0f, 0f);
    }

    void HandleGravity()
    {
        isGrounded = Physics.SphereCast(checkgroundPosition.position, checkGroundRadius, Vector3.down, out _, checkGroundDistance, groundLayer);
        if (isGrounded && _verticalVelocity < 0f) _verticalVelocity = -2f;
        else _verticalVelocity += gravity * Time.deltaTime;
    }

    void HandleCrouchHeight()
    {
        if (Mathf.Abs(_cc.height - _targetHeight) < 0.01f) return;
        _cc.height = Mathf.Lerp(_cc.height, _targetHeight, heightLerpSpeed * Time.deltaTime);
        _cc.center = new Vector3(0, _cc.height * 0.55f, 0);
    }

    // --- Боевка и Действия ---

    public void StartAttack()
    {
        if (animatorController.GetBool(AnimatorParameters.Attack)) return;
        
        Vector2 move = GetMoveInput();
        if (move == Vector2.zero) move = Vector2.up;
        SetAnimatorMove(move.x, move.y);

        if (cameraTransform)
        {
            Vector3 fwd = cameraTransform.forward;
            fwd.y = 0;
            if (fwd.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(fwd);
        }

        int id = (attackAnimations != null && attackAnimations.Count > 0) ? Random.Range(0, attackAnimations.Count) : 0;
        animatorController.SetInteger(AnimatorParameters.AttackId, id);
        animatorController.SetBool(AnimatorParameters.Attack, true);
    }

    private void OnAttackPerformed(InputAction.CallbackContext ctx)
    {
        if (!isBattleMode || isDead) return;
        if (!isAttacking) StartAttack();
        else if (inCombatTiming) wantNextAttack = true;
    }

    private void OnDodgePerformed(InputAction.CallbackContext ctx)
    {
        if (!isBattleMode || isDodging || isDead) return;
        Vector2 move = GetMoveInput();
        if (move == Vector2.zero) move.y = -1f;
        SetAnimatorMove(move.x, move.y);
        animatorController.SetTrigger(AnimatorParameters.DodgeTrigger);
    }

    private void OnTakeWeaponPerformed(InputAction.CallbackContext ctx)
    {
        if (isTakingWeapon || isAttacking || isDead) return;
        isTakingWeapon = true;
        animatorController.SetLayerWeight(weaponLayerIndex, 1f);
        animatorController.Play(!isBattleMode ? "Take_Weapon" : "Take_Weapon 0", weaponLayerIndex, 0f);
    }

    public void EndTakeWeapon()
    {
        isBattleMode = !isBattleMode;
        animatorController.SetBool(AnimatorParameters.Weapon, isBattleMode);
        animatorController.SetLayerWeight(weaponLayerIndex, 0f);
        isTakingWeapon = false;
    }

    private void OnCrouchPerformed(InputAction.CallbackContext ctx)
    {
        if (isAttacking || isDead) return;
        _isCrouched = !_isCrouched;
        _targetHeight = _isCrouched ? crouchHeight : standHeight;
        animatorController.SetBool(AnimatorParameters.Crouch, _isCrouched);
    }

    private void SetAnimatorMove(float x, float y)
    {
        animatorController.SetFloat(AnimatorParameters.X, x);
        animatorController.SetFloat(AnimatorParameters.Y, y);
    }

    private void OnEnable() => EnableControls();
    private void OnDisable() => DisableControls();
    private void OnDestroy() => DisableControls();

    private void EnableControls()
    {
        if (_attack == null) return;
        _attack.performed += OnAttackPerformed;
        _takeWeapon.performed += OnTakeWeaponPerformed;
        _dodge.performed += OnDodgePerformed;
        _crouch.performed += OnCrouchPerformed;
        _sprint.performed += ctx => _isSprinting = true;
        _sprint.canceled += ctx => _isSprinting = false;
        _interact.performed += ctx => objectToInteract?.Interact();
    }

    private void DisableControls()
    {
        if (_attack == null) return;
        _attack.performed -= OnAttackPerformed;
        _takeWeapon.performed -= OnTakeWeaponPerformed;
        _dodge.performed -= OnDodgePerformed;
        _crouch.performed -= OnCrouchPerformed;
    }

    private void LockCursor() { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
    public void Die() { isDead = true; _planarVelocity = Vector3.zero; DisableControls(); }

    public void SetCombatTiming(bool value) => inCombatTiming = value;
    public void SetAttackState(bool value) => isAttacking = value;
    public void SetTakingWeapon(bool value) => isTakingWeapon = value;
    public void BeginDodge() { isDodging = true; animatorController.applyRootMotion = true; }
    public void EndDodge() { isDodging = false; animatorController.applyRootMotion = false; }
    public void EnableDamageCollider() => damageCollider?.gameObject.SetActive(true);
    public void DisableDamageCollider() => damageCollider?.gameObject.SetActive(false);
}