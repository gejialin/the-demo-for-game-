using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CharacterMotor3D : MonoBehaviour
{
    [SerializeField] private float stoppingDistance = 0.12f;
    [SerializeField] private bool freezeBodyRotation = true;

    private Rigidbody rb;
    private CharacterStats stats;
    private CharacterStateController stateController;

    private Vector3 manualMoveInput;
    private Vector3 moveTarget;
    private bool hasMoveTarget;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        stats = GetComponent<CharacterStats>();
        stateController = GetComponent<CharacterStateController>();

        if (freezeBodyRotation)
            rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void FixedUpdate()
    {
        if (stateController != null && !stateController.CanMove)
        {
            if (stateController.currentState != CharacterState.Knocked)
                SetPlanarVelocity(Vector3.zero);

            stateController.SetMoving(false);
            return;
        }

        Vector3 moveDirection = GetMoveDirection();
        float speed = stats != null ? stats.currentMoveSpeed : 6f;

        if (moveDirection.sqrMagnitude > 0.001f)
        {
            SetPlanarVelocity(moveDirection.normalized * speed);
            if (stateController != null)
                stateController.SetMoving(true);
        }
        else
        {
            SetPlanarVelocity(Vector3.zero);
            if (stateController != null)
                stateController.SetMoving(false);
        }
    }

    public void SetManualMoveInput(Vector3 input)
    {
        input.y = 0f;
        manualMoveInput = Vector3.ClampMagnitude(input, 1f);

        if (manualMoveInput.sqrMagnitude > 0.001f)
            hasMoveTarget = false;
    }

    public void SetMoveTarget(Vector3 target)
    {
        target.y = transform.position.y;
        moveTarget = target;
        hasMoveTarget = true;
        manualMoveInput = Vector3.zero;
    }

    public bool HasMoveTarget()
    {
        return hasMoveTarget;
    }

    public void StopMovement()
    {
        manualMoveInput = Vector3.zero;
        hasMoveTarget = false;

        if (stateController == null || stateController.currentState != CharacterState.Knocked)
            SetPlanarVelocity(Vector3.zero);

        if (stateController != null && stateController.CanMove)
            stateController.SetMoving(false);
    }

    private Vector3 GetMoveDirection()
    {
        if (manualMoveInput.sqrMagnitude > 0.001f)
            return manualMoveInput;

        if (!hasMoveTarget)
            return Vector3.zero;

        Vector3 toTarget = moveTarget - transform.position;
        toTarget.y = 0f;

        if (toTarget.magnitude <= stoppingDistance)
        {
            hasMoveTarget = false;
            return Vector3.zero;
        }

        return toTarget.normalized;
    }

    private void SetPlanarVelocity(Vector3 planarVelocity)
    {
        rb.velocity = new Vector3(planarVelocity.x, rb.velocity.y, planarVelocity.z);
    }
}
