using UnityEngine;

namespace IA.Agents
{

    public class InterestObject : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float lifetime = 0f;

        private float age;
        private Vector3 initialScale;

        public static int ActiveCount { get; private set; } //cuantos objetos hay en escena, lo usa el cazador para no pasarse de 5

        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
        public float HealthNormalized => maxHealth <= 0f ? 0f : Mathf.Clamp01(CurrentHealth / maxHealth);

        public bool IsAvailable => CurrentHealth > 0f; //si no tiene vida los boids lo ignoran

        private void OnEnable() => ActiveCount++;

        private void OnDisable() => ActiveCount--;

        private void Awake()
        {
            CurrentHealth = maxHealth;
            initialScale = transform.localScale;
        }

        private void Update()
        {
            if (lifetime <= 0f) return;
            age += Time.deltaTime;
            if (age >= lifetime) Destroy(gameObject); //se destruye solo cuando pasa el tiempo
        }

        public bool ApplyDamage(float amount)
        {
            if (!IsAvailable) return false;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - Mathf.Max(0f, amount));

            if (CurrentHealth <= 0f)
            {
                Destroy(gameObject);
                return true; //avisa al boid que lo destruyo
            }

            transform.localScale = initialScale * Mathf.Lerp(0.45f, 1f, HealthNormalized); //se achica segun la vida que le queda
            return false;
        }
    }
}