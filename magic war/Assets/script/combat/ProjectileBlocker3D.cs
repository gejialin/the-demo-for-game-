using UnityEngine;

public class ProjectileBlocker3D : MonoBehaviour
{
    [SerializeField] private bool blocksProjectiles = true;

    public bool BlocksProjectiles => blocksProjectiles;

    public void SetBlocksProjectiles(bool shouldBlock)
    {
        blocksProjectiles = shouldBlock;
    }
}
