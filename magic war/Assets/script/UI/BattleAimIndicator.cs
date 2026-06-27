using UnityEngine;

public class BattleAimIndicator : MonoBehaviour
{
    private const float BaseMoveRadius = 0.6f;
    private const float LineHeight = 0.08f;

    private PlayerController3D playerController;
    private SkillCaster3D skillCaster;
    private CharacterStateController stateController;

    private Transform ringTransform;
    private Renderer ringRenderer;
    private Transform lineTransform;
    private Renderer lineRenderer;

    private void Awake()
    {
        EnsureVisuals();
    }

    private void Update()
    {
        if (playerController == null)
        {
            SetVisible(false);
            return;
        }

        bool visible = playerController.ControlMode == InputControlMode.ClickToMove
            && playerController.HasGroundPoint
            && (stateController == null || stateController.CanCast);
        if (!visible)
        {
            SetVisible(false);
            return;
        }

        Vector3 point = playerController.GroundPoint;
        point.y += 0.03f;
        transform.position = point;

        UpdateRing();
        UpdateLine(point);
        SetVisible(true);
    }

    public void BindPlayer(PlayerController3D controller)
    {
        if (ReferenceEquals(playerController, controller))
            return;

        playerController = controller;
        skillCaster = controller != null ? controller.GetComponent<SkillCaster3D>() : null;
        stateController = controller != null ? controller.GetComponent<CharacterStateController>() : null;
    }

    public bool HasBoundPlayer()
    {
        return playerController != null;
    }

    private void OnDisable()
    {
        SetVisible(false);
    }

    private void EnsureVisuals()
    {
        if (ringTransform == null)
        {
            GameObject ringObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ringObject.name = "AimRing";
            ringObject.transform.SetParent(transform, false);
            ringTransform = ringObject.transform;
            ringRenderer = ringObject.GetComponent<Renderer>();

            Collider collider = ringObject.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            if (ringRenderer != null)
            {
                ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                ringRenderer.receiveShadows = false;
                ringRenderer.material = CreateRuntimeMaterial(new Color(0.92f, 0.92f, 0.92f, 0.72f));
            }
        }

        if (lineTransform == null)
        {
            GameObject lineObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lineObject.name = "AimLine";
            lineObject.transform.SetParent(transform, false);
            lineTransform = lineObject.transform;
            lineRenderer = lineObject.GetComponent<Renderer>();

            Collider collider = lineObject.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            if (lineRenderer != null)
            {
                lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lineRenderer.receiveShadows = false;
                lineRenderer.material = CreateRuntimeMaterial(new Color(0.92f, 0.92f, 0.92f, 0.52f));
            }
        }
    }

    private void UpdateRing()
    {
        PlayerActionMode mode = playerController.ActionMode;
        float radius = GetRingRadius(mode);
        Color color = GetColorForMode(mode);

        if (ringTransform != null)
            ringTransform.localScale = new Vector3(radius, 0.02f, radius);

        if (ringRenderer != null)
            ringRenderer.material.color = color;
    }

    private void UpdateLine(Vector3 groundPoint)
    {
        if (lineTransform == null || playerController == null)
            return;

        Vector3 from = playerController.transform.position;
        from.y = groundPoint.y + LineHeight;
        Vector3 to = groundPoint;
        to.y = groundPoint.y + LineHeight;

        Vector3 direction = to - from;
        float distance = direction.magnitude;
        if (distance <= 0.01f)
        {
            lineTransform.gameObject.SetActive(false);
            return;
        }

        lineTransform.gameObject.SetActive(true);
        lineTransform.position = from + direction * 0.5f;
        lineTransform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        lineTransform.localScale = new Vector3(0.08f, 0.04f, distance);

        if (lineRenderer != null)
            lineRenderer.material.color = GetLineColorForMode(playerController.ActionMode);
    }

    private float GetRingRadius(PlayerActionMode mode)
    {
        switch (mode)
        {
            case PlayerActionMode.AimingPrimary:
                return Mathf.Max(BaseMoveRadius, skillCaster != null ? skillCaster.primarySkill.radius * 2.4f : 0.9f);
            case PlayerActionMode.AimingMelee:
                return Mathf.Max(BaseMoveRadius, skillCaster != null ? skillCaster.meleeSkill.radius * 2.2f : 1.0f);
            default:
                return BaseMoveRadius;
        }
    }

    private Color GetColorForMode(PlayerActionMode mode)
    {
        switch (mode)
        {
            case PlayerActionMode.AimingPrimary:
                return new Color(1f, 0.48f, 0.12f, 0.95f);
            case PlayerActionMode.AimingMelee:
                return new Color(0.36f, 0.72f, 1f, 0.95f);
            default:
                return new Color(0.92f, 0.92f, 0.92f, 0.72f);
        }
    }

    private Color GetLineColorForMode(PlayerActionMode mode)
    {
        switch (mode)
        {
            case PlayerActionMode.AimingPrimary:
                return new Color(1f, 0.68f, 0.28f, 0.72f);
            case PlayerActionMode.AimingMelee:
                return new Color(0.48f, 0.82f, 1f, 0.72f);
            default:
                return new Color(0.86f, 0.86f, 0.86f, 0.42f);
        }
    }

    private Material CreateRuntimeMaterial(Color color)
    {
        Material material = new Material(Shader.Find("Standard"));
        material.color = color;
        material.SetFloat("_Mode", 3f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
        return material;
    }

    private void SetVisible(bool visible)
    {
        if (ringTransform != null)
            ringTransform.gameObject.SetActive(visible);

        if (lineTransform != null)
            lineTransform.gameObject.SetActive(visible);

        if (ringRenderer != null)
            ringRenderer.enabled = visible;

        if (lineRenderer != null)
            lineRenderer.enabled = visible;
    }
}
