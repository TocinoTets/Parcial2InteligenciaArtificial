using UnityEngine;

namespace IA.Agents
{
    /// <summary>Proyectil del ataque a distancia del NPC Cazador (estado Attack).</summary>
    public class HunterProjectile : MonoBehaviour
    {
        [SerializeField] private float hitRadius = 0.7f;
        [SerializeField] private float maxLifetime = 5f;

        private BoidAgent target;
        private float speed = 18f;
        private float damage = 15f;
        private float life;

        public void Initialize(BoidAgent newTarget, float newSpeed, float newDamage)
        {
            target = newTarget;
            speed = newSpeed;
            damage = newDamage;
            life = 0f;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            life += dt;

            if (life >= maxLifetime || target == null || !target.IsAlive)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 aim = target.transform.position + Vector3.up * 0.4f;
            Vector3 toTarget = aim - transform.position;
            float distance = toTarget.magnitude;
            float step = speed * dt;

            // El desplazamiento de este frame se compara con la distancia que falta: si el
            // paso alcanza el radio de impacto el golpe se resuelve igualmente. Comprobar sólo
            // la distancia actual dejaba atravesar al boid sin efecto cuando el paso era grande.
            if (distance - step <= hitRadius)
            {
                target.TakeDamage(damage, "ataque a distancia");
                Destroy(gameObject);
                return;
            }

            Vector3 direction = toTarget / distance;
            transform.position += direction * step;
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }
}
