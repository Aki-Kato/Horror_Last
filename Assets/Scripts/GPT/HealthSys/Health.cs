using UnityEngine;
using UnityEngine.Events;

namespace Horror.Core
{
    /// <summary>
    /// Базовое здоровье для любых существ. Поддерживает GodMode.
    /// </summary>
    public class Health : MonoBehaviour, IDamagable
    {
        [Header("Health")]
        [SerializeField] protected float maxHealth = 100f;
        [SerializeField] protected float health = 100f;

        [Header("Flags")]
        [Tooltip("Если включено — урон не проходит вообще.")]
        [SerializeField] protected bool godMode = false;

        [Header("Events")]
        public UnityEvent<float, float> OnHealthChanged; // (current, max)
        public UnityEvent<float> OnDamaged;              // damage amount actually applied
        public UnityEvent<float> OnHealed;               // heal amount actually applied
        public UnityEvent OnDied;
        private Collider _collider;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => health;
        public bool IsDead { get; protected set; }
        public bool GodMode
        {
            get => godMode;
            set => godMode = value;
        }

        protected virtual void Awake()
        {
            health = Mathf.Clamp(health, 0f, maxHealth);
            IsDead = health <= 0f;
            _collider = GetComponent<Collider>();
        }

        protected virtual void OnEnable()
        {
            // Синхронизируем слушателям текущее состояние при старте
            OnHealthChanged?.Invoke(health, maxHealth);
            DamagablesRegistry.All.Add(_collider, this);
        }

        protected virtual void OnDisable()
        {
            DamagablesRegistry.All.Remove(_collider);
        }
        public virtual void SetMaxHealth(float newMax, bool clampCurrent = true)
        {
            maxHealth = Mathf.Max(1f, newMax);
            if (clampCurrent) health = Mathf.Clamp(health, 0f, maxHealth);
            OnHealthChanged?.Invoke(health, maxHealth);
        }

        public virtual void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            float before = health;
            health = Mathf.Clamp(health + amount, 0f, maxHealth);
            float applied = health - before;
            if (applied > 0f)
            {
                OnHealed?.Invoke(applied);
                OnHealthChanged?.Invoke(health, maxHealth);
            }
        }

       
        public virtual float TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f || godMode) return 0f;

            float before = health;
            health = Mathf.Clamp(health - amount, 0f, maxHealth);
            float applied = before - health;
            OnDamaged?.Invoke(applied);
            OnHealthChanged?.Invoke(health, maxHealth);
            if (health <= 0f && !IsDead)
            {
                    IsDead = true;
                    Die();
            }
            
            return applied;
        }

        protected virtual void Die()
        {
            OnDied?.Invoke();
            // Базовая реализация ничего не делает — переопредели в наследниках.
        }

        // Утилиты
        public virtual void Revive(float setHealthTo = -1f)
        {
            IsDead = false;
            if (setHealthTo < 0f) setHealthTo = maxHealth;
            health = Mathf.Clamp(setHealthTo, 1f, maxHealth);
            OnHealthChanged?.Invoke(health, maxHealth);
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            health = Mathf.Clamp(health, 0f, maxHealth);
        }
#endif
    }
}