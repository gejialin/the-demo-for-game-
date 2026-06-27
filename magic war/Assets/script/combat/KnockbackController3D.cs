using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class KnockbackController3D : MonoBehaviour
{
    private Rigidbody rb;
    private CharacterMotor3D motor;
    private CharacterStateController stateController;
    private Coroutine knockbackRoutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        motor = GetComponent<CharacterMotor3D>();
        stateController = GetComponent<CharacterStateController>();
    }

    public void ApplyKnockback(Vector3 direction, float force, float duration)
    {
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
            return;

        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f || force <= 0f || duration <= 0f)
            return;

        if (knockbackRoutine != null)
            StopCoroutine(knockbackRoutine);

        if (motor != null)
            motor.StopMovement();

        knockbackRoutine = StartCoroutine(KnockbackRoutine(direction.normalized, force, duration));
    }

    private IEnumerator KnockbackRoutine(Vector3 direction, float force, float duration)
    {
        if (stateController != null)
            stateController.SetState(CharacterState.Knocked);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float currentForce = Mathf.Lerp(force, 0f, t);
            Vector3 velocity = direction * currentForce;
            rb.velocity = new Vector3(velocity.x, rb.velocity.y, velocity.z);

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        rb.velocity = new Vector3(0f, rb.velocity.y, 0f);

        if (stateController != null)
            stateController.ReturnToIdleIfNotDead();

        knockbackRoutine = null;
    }

    public void CancelKnockback()
    {
        if (knockbackRoutine != null)
        {
            StopCoroutine(knockbackRoutine);
            knockbackRoutine = null;
        }

        rb.velocity = new Vector3(0f, rb.velocity.y, 0f);

        if (stateController != null && stateController.currentState == CharacterState.Knocked)
            stateController.ReturnToIdleIfNotDead();
    }
}
