using UnityEngine;

public class LayerMaskConfig : MonoBehaviour
{
    public static LayerMaskConfig Instance { get; private set; }

    public LayerMask groundMask;
    public LayerMask hittableMask;
    public LayerMask lavaMask;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public static LayerMask GetGroundMask()
    {
        if (Instance != null && Instance.groundMask.value != 0)
            return Instance.groundMask;

        int groundLayer = LayerMask.NameToLayer("Ground");
        return groundLayer >= 0 ? 1 << groundLayer : Physics.DefaultRaycastLayers;
    }

    public static LayerMask GetHittableMask()
    {
        if (Instance != null && Instance.hittableMask.value != 0)
            return Instance.hittableMask;

        return Physics.DefaultRaycastLayers;
    }

    public static LayerMask GetLavaMask()
    {
        if (Instance != null && Instance.lavaMask.value != 0)
            return Instance.lavaMask;

        int lavaLayer = LayerMask.NameToLayer("Lava");
        return lavaLayer >= 0 ? 1 << lavaLayer : Physics.DefaultRaycastLayers;
    }
}
