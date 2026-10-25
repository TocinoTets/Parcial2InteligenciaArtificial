using UnityEngine;

namespace IA.Agents
{
    /// <summary>
    /// Percepción del Cazador: una única consulta de física sobre su radio de percepción.
    /// No hay información compartida con los boids ni consultas globales de escena.
    /// </summary>
    public partial class HunterNPC
    {
        // ---------------- sensores ----------------
        private void Sense()
        {
            detectedBoids.Clear();
            closestVisibleBoid = null;
            detectedDeadBoid = null;

            float closestAliveDistance = float.MaxValue;
            float closestDeadDistance = float.MaxValue;

            int count = Physics.OverlapSphereNonAlloc(transform.position, Mathf.Max(perceptionRadius, rangeAttackRadius), overlapBuffer);

            for (int i = 0; i < count; i++)
            {
                Collider other = overlapBuffer[i];
                if (other == null || other == bodyCollider) continue;

                BoidAgent boid = other.GetComponentInParent<BoidAgent>();
                if (boid == null) continue;

                float distance = AgentMath.FlatDistance(transform.position, boid.transform.position);
                if (distance > perceptionRadius) continue;

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
        }
    }
}
