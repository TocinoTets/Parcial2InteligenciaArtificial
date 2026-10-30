using UnityEngine;

namespace IA.Agents
{
    //percepcion del cazador: ve boids y lobos solo si no hay paredes en el medio, sin angulo de vision
    public partial class HunterNPC
    {
        [Header("Optimización")]
        [SerializeField] private float senseInterval = 0.1f; //cada cuanto percibe, no hace falta hacerlo todos los frames

        private float senseTimer;

        public bool IsInVision(Vector3 point)
        {
            return Perception.CanSee(transform.position, transform.forward, point, visionRadius, 360f); //360 = ve para todos lados
        }

        private void Sense()
        {
            if (fsm.CurrentId == HunterStateId.Dead) //muerto no percibe nada
            {
                detectedBoids.Clear();
                closestVisibleBoid = null;
                detectedDeadBoid = null;
                visibleWolf = null;
                return;
            }

            //entre percepciones descarta lo que ya no sirve (boid que murio o que ya recolectaron)
            if (closestVisibleBoid != null && !closestVisibleBoid.IsAlive) closestVisibleBoid = null;
            if (detectedDeadBoid != null && !detectedDeadBoid.IsHarvestable) detectedDeadBoid = null;

            senseTimer -= Time.deltaTime;
            if (senseTimer > 0f) return;
            senseTimer = senseInterval;

            detectedBoids.Clear();
            closestVisibleBoid = null;
            detectedDeadBoid = null;
            visibleWolf = null;

            float closestAliveDistance = float.MaxValue;
            float closestDeadDistance = float.MaxValue;
            float closestWolfDistance = float.MaxValue;

            float queryRadius = Mathf.Max(Mathf.Max(visionRadius, rangeAttackRadius), wolfDetectionRadius);
            int count = Physics.OverlapSphereNonAlloc(transform.position, queryRadius, overlapBuffer, Perception.AgentsMask, QueryTriggerInteraction.Collide); //una sola consulta para boids y lobos

            for (int i = 0; i < count; i++)
            {
                Collider other = overlapBuffer[i];
                if (other == null || other == bodyCollider) continue;

                if (other.TryGetComponent(out WolfAgent wolf))
                {
                    if (!Perception.CanSee(transform.position, transform.forward, wolf.Position, wolfDetectionRadius, 360f)) continue;

                    float wolfDistance = AgentMath.FlatDistance(transform.position, wolf.Position);
                    if (wolfDistance >= closestWolfDistance) continue;
                    closestWolfDistance = wolfDistance;
                    visibleWolf = wolf;
                    continue;
                }

                if (!other.TryGetComponent(out BoidAgent boid)) continue;
                if (!IsInVision(boid.transform.position)) continue;

                float distance = AgentMath.FlatDistance(transform.position, boid.transform.position);

                if (boid.IsAlive)
                {
                    detectedBoids.Add(boid);
                    if (distance < closestAliveDistance) //se queda con el boid vivo mas cercano
                    {
                        closestAliveDistance = distance;
                        closestVisibleBoid = boid;
                    }
                }
                else if (boid.IsHarvestable && distance < closestDeadDistance) //y con el muerto mas cercano
                {
                    closestDeadDistance = distance;
                    detectedDeadBoid = boid;
                }
            }
        }
    }
}