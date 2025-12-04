using System;
using UnityEngine;

namespace Horror.Core
{
    public class PlayerHealth : Health
    {
        [SerializeField] private PlayerController pController;

        protected override void OnEnable()
        {
            base.OnEnable();
            DamagablesRegistry.PlayerDamagable = this;
        }

        protected override void Die()
        {
            base.Die();
            pController.Die();

        }

        public override void Revive(float setHealthTo = -1f)
        {
            throw new NotImplementedException();
        }
    }
}
