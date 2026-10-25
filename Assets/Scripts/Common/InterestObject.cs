using UnityEngine;

namespace IA.Agents
{
    /// <summary>
    /// Objeto de interés generado por el NPC Cazador durante el estado Patrol.
    /// Los agentes Boid se dirigen hacia él con Arrive y, al llegar, lo golpean
    /// reduciéndole la vida hasta destruirlo.
    /// </summary>
    public class InterestObject : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;
        [Tooltip("0 = sin caducidad: sólo lo destruyen los Boids.")]
        [SerializeField] private float lifetime = 0f;

        private float age;
        private Vector3 initialScale;

        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
        public float HealthNormalized => maxHealth <= 0f ? 0f : Mathf.Clamp01(CurrentHealth / maxHealth);

        /// <summary>El objeto sigue disponible para ser consumido por un Boid.</summary>
        public bool IsAvailable => CurrentHealth > 0f;

        private void Awake()
        {
            CurrentHealth = maxHealth;
            initialScale = transform.localScale;
        }

        private void Update()
        {
            if (lifetime <= 0f) return;
            age += Time.deltaTime;
            if (age >= lifetime) Destroy(gameObject);
        }

        /// <summary>Reduce la vida del objeto. Devuelve true si quedó destruido.</summary>
        public bool ApplyDamage(float amount)
        {
            if (!IsAvailable) return false;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - Mathf.Max(0f, amount));

            if (CurrentHealth <= 0f)
            {
                Destroy(gameObject);
                return true;
            }

            // Feedback funcional: el objeto se encoge a medida que pierde vida.
            transform.localScale = initialScale * Mathf.Lerp(0.45f, 1f, HealthNormalized);
            return false;
        }
    }
}
