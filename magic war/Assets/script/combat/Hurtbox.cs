using UnityEngine;

public class Hurtbox : MonoBehaviour
{
    [SerializeField] private DamageReceiver damageReceiver;

    private void Awake()
    {
        if (damageReceiver == null)
            damageReceiver = GetComponentInParent<DamageReceiver>();
    }

    public void ReceiveDamage(DamageInfo info)
    {
        if (damageReceiver != null)
            damageReceiver.ReceiveDamage(info);
    }
}
