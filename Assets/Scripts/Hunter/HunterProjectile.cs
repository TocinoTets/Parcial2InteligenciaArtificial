using UnityEngine;

namespace IA.Agents
{
    //proyectil del ataque a distancia del cazador
    public class HunterProjectile : MonoBehaviour
    {
        [SerializeField] private float hitRadius = 0.7f;
        [SerializeField] private float maxLifetime = 5f;

        private readonly Collider[] hits = new Collider[8];
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

            if (life >= maxLifetime || target == null || !target.IsAlive) //se destruye si paso el tiempo o el objetivo murio
            {
                Destroy(gameObject);
                return;
            }

            //le pega al primer boid que toque y se rompe contra las paredes
            int count = Physics.OverlapSphereNonAlloc(transform.position, hitRadius, hits);
            for (int i = 0; i < count; i++)
            {
                BoidAgent boid = hits[i].GetComponentInParent<BoidAgent>();
                if (boid != null && boid.IsAlive) boid.TakeDamage(damage, "ataque a distancia");
                else if (boid != null || hits[i].isTrigger) continue;

                Destroy(gameObject);
                return;
            }

            Vector3 aim = target.transform.position + Vector3.up * 0.4f;
            Vector3 toTarget = aim - transform.position;
            float distance = toTarget.magnitude;
            float step = speed * dt;

            if (distance - step <= hitRadius) //si en este frame lo alcanza le pega, asi no lo atraviesa
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