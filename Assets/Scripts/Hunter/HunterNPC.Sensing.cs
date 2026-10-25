using UnityEngine;

namespace IA.Agents
{
    /// <summary>
    /// Percepción del Cazador: boids dentro de VisionRadius y lobos dentro de
    /// wolfDetectionRadius, siempre con línea de visión (las paredes bloquean).
    /// No hay ángulo de visión.
    /// </summary>
    public partial class HunterNPC
    {
        // ---------------- sensores ----------------
        public bool IsInVision(Vector3 point)
        {
            return Perception.CanSee(transform.position, transform.forward, point, visionRadius, 360f);
        }

        private void Sense()
        {
            detectedBoids.Clear();
            closestVisibleBoid = null;
            detectedDeadBoid = null;
            visibleWolf = null;

            if (fsm.CurrentId == HunterStateId.Dead) return; // fuera de escena: no percibe nada

            float closestAliveDistance = float.MaxValue;
            float closestDeadDistance = float.MaxValue;

            int count = Physics.OverlapSphereNonAlloc(transform.position, Mathf.Max(visionRadius, rangeAttackRadius), overlapBuffer);

            for (int i = 0; i < count; i++)
            {
                Collider other = overlapBuffer[i];
                if (other == null || other == bodyCollider) continue;

                BoidAgent boid = other.GetComponentInParent<BoidAgent>();
                if (boid == null) continue;

                if (!IsInVision(boid.transform.position)) continue;
                float distance = AgentMath.FlatDistance(transform.position, boid.transform.position);

                if (boid.IsAlive)
                {
                    detectedBoids.Add(boid);
                    if (distance < closestAliveDistance)
                    {
                        closestAliveDistance = distance;
                        closestVisibleBoid = boid;
                    }
                }
                else if (boid.IsHarvestable && distance < closestDeadDistance)
                {
                    closestDeadDistance = distance;
                    detectedDeadBoid = boid;
                }
            }

            float closestWolfDistance = float.MaxValue;
            foreach (WolfAgent wolf in WolfAgent.All)
            {
                if (!Perception.CanSee(transform.position, transform.forward, wolf.Position, wolfDetectionRadius, 360f)) continue;

                float distance = AgentMath.FlatDistance(transform.position, wolf.Position);
                if (distance >= closestWolfDistance) continue;
                closestWolfDistance = distance;
                visibleWolf = wolf;
            }
        }
    }
}
