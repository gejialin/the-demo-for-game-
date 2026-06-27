using UnityEngine;

public class ArenaBoundaryManager : MonoBehaviour
{
    [SerializeField] private Vector2 xBounds = new Vector2(-12f, 12f);
    [SerializeField] private Vector2 zBounds = new Vector2(-12f, 12f);
    [SerializeField] private Transform[] trackedCharacters;
    [SerializeField] private bool autoFindCharacters = true;

    private void Awake()
    {
        NormalizeBounds();
        ResolveTrackedCharacters();
    }

    private void OnValidate()
    {
        NormalizeBounds();
    }

    private void LateUpdate()
    {
        if (!HasTrackedCharacters())
        {
            ResolveTrackedCharacters();
            if (!HasTrackedCharacters())
                return;
        }

        for (int i = 0; i < trackedCharacters.Length; i++)
        {
            Transform target = trackedCharacters[i];
            if (target == null)
                continue;

            Vector3 position = target.position;
            position.x = Mathf.Clamp(position.x, xBounds.x, xBounds.y);
            position.z = Mathf.Clamp(position.z, zBounds.x, zBounds.y);
            target.position = position;
        }
    }

    private void ResolveTrackedCharacters()
    {
        if (!autoFindCharacters || HasTrackedCharacters())
            return;

        CharacterMotor3D[] motors = FindObjectsOfType<CharacterMotor3D>();
        trackedCharacters = new Transform[motors.Length];
        for (int i = 0; i < motors.Length; i++)
            trackedCharacters[i] = motors[i].transform;
    }

    private bool HasTrackedCharacters()
    {
        if (trackedCharacters == null || trackedCharacters.Length == 0)
            return false;

        for (int i = 0; i < trackedCharacters.Length; i++)
        {
            if (trackedCharacters[i] != null)
                return true;
        }

        return false;
    }

    private void NormalizeBounds()
    {
        if (xBounds.x > xBounds.y)
            xBounds = new Vector2(xBounds.y, xBounds.x);

        if (zBounds.x > zBounds.y)
            zBounds = new Vector2(zBounds.y, zBounds.x);
    }
}
