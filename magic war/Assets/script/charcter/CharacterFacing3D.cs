using UnityEngine;

public class CharacterFacing3D : MonoBehaviour
{
    [SerializeField] private float turnSpeed = 720f;
    [SerializeField] private bool snapRotation;

    public void FaceDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

        if (snapRotation || turnSpeed <= 0f)
        {
            transform.rotation = targetRotation;
        }
        else
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime
            );
        }
    }
}
