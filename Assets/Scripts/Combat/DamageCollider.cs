using System.Collections.Generic;
using UnityEngine;

public class DamageCollider : MonoBehaviour
{
    public bool ignoreEnemies, ignorePlayer;
    public float damage;
    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Started! " + other.gameObject.name);
        var isDamagable = DamagablesRegistry.All.TryGetValue(other, out var damagable);
        if (!isDamagable)
        {
            return;
        }
        var isPlayer = damagable == DamagablesRegistry.PlayerDamagable;
        if ((isPlayer && ignorePlayer) || (!isPlayer && ignoreEnemies))
        {
            return;
        }
        damagable.TakeDamage(damage);
    }
}
