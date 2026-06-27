using UnityEngine;

[RequireComponent(typeof(Collider))]
public class GroundPlane : MonoBehaviour
{
    [SerializeField] private string groundLayerName = "Ground";

    private void Reset()
    {
        ApplyGroundLayer();
    }

    private void Awake()
    {
        ApplyGroundLayer();
    }

    private void OnValidate()
    {
        ApplyGroundLayer();
    }

    private void ApplyGroundLayer()
    {
        if (string.IsNullOrWhiteSpace(groundLayerName))
            return;

        int layer = LayerMask.NameToLayer(groundLayerName);
        if (layer >= 0)
            gameObject.layer = layer;
    }
}
