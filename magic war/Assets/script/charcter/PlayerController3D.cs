using UnityEngine;

public enum InputControlMode
{
    WASDMouseAim,
    ClickToMove
}

public enum PlayerActionMode
{
    Normal,
    AimingPrimary,
    AimingMelee
}

public class PlayerController3D : MonoBehaviour
{
    public static PlayerController3D ActivePlayer { get; private set; }

    public InputControlMode controlMode = InputControlMode.WASDMouseAim;
    public PlayerActionMode actionMode = PlayerActionMode.Normal;

    [SerializeField] private LayerMask groundMask;

    private CharacterMotor3D motor;
    private SkillCaster3D skillCaster;
    private CharacterFacing3D facing;
    private CharacterStateController stateController;
    private Camera mainCamera;
    private Vector3 lastGroundPoint;
    private bool hasGroundPoint;

    public InputControlMode ControlMode => controlMode;
    public PlayerActionMode ActionMode => actionMode;
    public bool IsAimingInClickMode => controlMode == InputControlMode.ClickToMove && actionMode != PlayerActionMode.Normal;
    public bool CanChangeInputMode => stateController == null || stateController.CanCast;
    public bool HasGroundPoint => hasGroundPoint;
    public Vector3 GroundPoint => lastGroundPoint;

    private void Awake()
    {
        ActivePlayer = this;
        motor = GetComponent<CharacterMotor3D>();
        skillCaster = GetComponent<SkillCaster3D>();
        facing = GetComponent<CharacterFacing3D>();
        stateController = GetComponent<CharacterStateController>();
        mainCamera = Camera.main;
        ResolveLayerMasks();
    }

    private void OnDisable()
    {
        if (ActivePlayer == this)
            ActivePlayer = null;
    }

    private void Update()
    {
        if (stateController != null && stateController.currentState == CharacterState.Dead)
            return;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (groundMask.value == 0)
            ResolveLayerMasks();

        switch (controlMode)
        {
            case InputControlMode.WASDMouseAim:
                HandleWASDMode();
                break;

            case InputControlMode.ClickToMove:
                HandleClickMoveMode();
                break;
        }

        HandleModeSwitch();
    }

    private void HandleWASDMode()
    {
        HandleWASDMovement();

        if (!TryGetMousePointOnGround(out Vector3 targetPoint))
            return;

        Vector3 aimDir = targetPoint - transform.position;
        aimDir.y = 0f;

        if (aimDir.sqrMagnitude > 0.001f)
            facing.FaceDirection(aimDir.normalized);

        if (Input.GetMouseButtonDown(0))
            skillCaster.TryCastPrimary(aimDir.normalized);

        if (Input.GetMouseButtonDown(1))
            skillCaster.TryCastMelee(aimDir.normalized);
    }

    private void HandleClickMoveMode()
    {
        if (!TryGetMousePointOnGround(out Vector3 targetPoint))
            return;

        if (actionMode == PlayerActionMode.Normal)
        {
            HandleClickMovement(targetPoint);

            if (motor.HasMoveTarget())
            {
                Vector3 dir = targetPoint - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                    facing.FaceDirection(dir.normalized);
            }

            if (Input.GetKeyDown(KeyCode.Q))
                TryEnterPrimaryAim();

            if (Input.GetKeyDown(KeyCode.E))
                TryEnterMeleeAim();
        }
        else
        {
            Vector3 aimDir = targetPoint - transform.position;
            aimDir.y = 0f;

            if (aimDir.sqrMagnitude > 0.001f)
                facing.FaceDirection(aimDir.normalized);

            if (Input.GetMouseButtonDown(0))
                TryConfirmAimCast(aimDir.normalized);

            if (Input.GetMouseButtonDown(1))
                actionMode = PlayerActionMode.Normal;
        }
    }

    private void HandleWASDMovement()
    {
        if (stateController != null && !stateController.CanMove)
        {
            motor.SetManualMoveInput(Vector3.zero);
            return;
        }

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(x, 0f, z);

        motor.SetManualMoveInput(input);
    }

    private void HandleClickMovement(Vector3 targetPoint)
    {
        if (stateController != null && !stateController.CanMove)
            return;

        if (Input.GetMouseButtonDown(1))
            motor.SetMoveTarget(targetPoint);
    }

    private void HandleModeSwitch()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
            TryToggleControlMode();
    }

    private bool TryGetMousePointOnGround(out Vector3 point)
    {
        point = lastGroundPoint;

        if (mainCamera == null)
            return false;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundMask))
        {
            point = hit.point;
            lastGroundPoint = hit.point;
            hasGroundPoint = true;
            return true;
        }

        return hasGroundPoint;
    }

    private void ResolveLayerMasks()
    {
        if (groundMask.value == 0)
            groundMask = LayerMaskConfig.GetGroundMask();
    }

    public string GetControlModeLabel()
    {
        return LocalizedUiTextBridge.GetControlModeLabel(controlMode);
    }

    public string GetActionModeLabel()
    {
        return LocalizedUiTextBridge.GetActionModeLabel(actionMode);
    }

    public string GetHintText()
    {
        return LocalizedUiTextBridge.GetHintText(controlMode, actionMode);
    }

    public bool TryCancelAimMode()
    {
        if (!IsAimingInClickMode)
            return false;

        actionMode = PlayerActionMode.Normal;
        return true;
    }

    public bool TryToggleControlMode()
    {
        if (!CanChangeInputMode)
            return false;

        controlMode = controlMode == InputControlMode.WASDMouseAim
            ? InputControlMode.ClickToMove
            : InputControlMode.WASDMouseAim;

        actionMode = PlayerActionMode.Normal;
        motor.StopMovement();
        return true;
    }

    public bool TryEnterPrimaryAim()
    {
        return TryEnterAimMode(PlayerActionMode.AimingPrimary);
    }

    public bool TryEnterMeleeAim()
    {
        return TryEnterAimMode(PlayerActionMode.AimingMelee);
    }

    public bool TryConfirmAimCast(Vector3 aimDirection)
    {
        if (!IsAimingInClickMode || aimDirection.sqrMagnitude <= 0.001f)
            return false;

        bool castStarted = false;
        if (actionMode == PlayerActionMode.AimingPrimary)
            castStarted = skillCaster.TryCastPrimary(aimDirection.normalized);
        else if (actionMode == PlayerActionMode.AimingMelee)
            castStarted = skillCaster.TryCastMelee(aimDirection.normalized);

        if (castStarted)
            actionMode = PlayerActionMode.Normal;

        return castStarted;
    }

    public void ResetRuntimeState()
    {
        actionMode = PlayerActionMode.Normal;
        hasGroundPoint = false;
        lastGroundPoint = Vector3.zero;

        if (motor != null)
            motor.StopMovement();
    }

    private bool TryEnterAimMode(PlayerActionMode targetMode)
    {
        if (!CanChangeInputMode || controlMode != InputControlMode.ClickToMove)
            return false;

        motor.StopMovement();
        actionMode = targetMode;
        return true;
    }
}
