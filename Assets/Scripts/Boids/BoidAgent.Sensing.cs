using UnityEngine;

namespace IA.Agents
{
    /// <summary>
    /// Sensores locales del boid y decisión de estado. No hay coordinación global:
    /// todo lo que el agente sabe proviene de una única consulta de física sobre sus
    /// propios radios.
    /// </summary>
    public partial class BoidAgent
    {
        // ---------------- sensores ----------------
        private void Sense()
        {
            neighbors.Clear();
            deadNeighbors.Clear();
            visibleInterests.Clear();
            detectedHunter = null;

            float queryRadius = Mathf.Max(Mathf.Max(perceptionRadius, viewRadius), Mathf.Max(interestSightRadius, separationRadius));
            int count = Physics.OverlapSphereNonAlloc(transform.position, queryRadius, overlapBuffer);

            float closestHunter = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Collider other = overlapBuffer[i];
                if (other == null || other == bodyCollider) continue;

                BoidAgent otherBoid = other.GetComponentInParent<BoidAgent>();
                if (otherBoid != null)
                {
                    if (otherBoid == this) continue;

                    float sqrDistance = AgentMath.FlatSqrDistance(transform.position, otherBoid.transform.position);

                    if (otherBoid.IsAlive)
                    {
                        if (sqrDistance <= perceptionRadius * perceptionRadius) neighbors.Add(otherBoid);
                    }
                    else if (otherBoid.IsHarvestable && sqrDistance <= separationRadius * separationRadius)
                    {
                        deadNeighbors.Add(otherBoid);
                    }
                    continue;
                }

                HunterNPC hunter = other.GetComponentInParent<HunterNPC>();
                if (hunter != null)
                {
                    float distance = AgentMath.FlatDistance(transform.position, hunter.transform.position);
                    if (distance <= viewRadius && distance < closestHunter)
                    {
                        closestHunter = distance;
                        detectedHunter = hunter;
                    }
                    continue;
                }

                InterestObject interest = other.GetComponentInParent<InterestObject>();
                if (interest != null && interest.IsAvailable &&
                    AgentMath.FlatSqrDistance(transform.position, interest.transform.position) <= interestSightRadius * interestSightRadius)
                {
                    visibleInterests.Add(interest);
                }
            }
        }

        // ---------------- decisión (local, sin coordinación) ----------------
        private void UpdateState(float dt)
        {
            BoidState previous = state;

            if (detectedHunter != null)
            {
                // Evade tiene prioridad absoluta sobre los comportamientos de flocking.
                lastThreatPosition = detectedHunter.transform.position;
                evadeMemoryTimer = evadeMemoryDuration;
                state = BoidState.Evading;
            }
            else if (evadeMemoryTimer > 0f)
            {
                // Memoria de huida: sigue escapando del último punto donde vio al Cazador.
                // Sin esto se detenía justo al salir de su rango de visión y la cohesión
                // lo devolvía al borde, quedándose "al margen" del Cazador.
                evadeMemoryTimer = Mathf.Max(0f, evadeMemoryTimer - dt);
                state = BoidState.Evading;
            }
            else
            {
                bool targetValid = targetInterest != null && targetInterest.IsAvailable &&
                                   AgentMath.FlatSqrDistance(transform.position, targetInterest.transform.position) <=
                                   interestSightRadius * interestSightRadius * 1.7f;

                if (!targetValid) targetInterest = SelectInterest();

                if (targetInterest == null)
                {
                    state = BoidState.Flocking;
                }
                else
                {
                    float sqrDistance = AgentMath.FlatSqrDistance(transform.position, targetInterest.transform.position);
                    state = sqrDistance <= interactionDistance * interactionDistance ? BoidState.Interacting : BoidState.Arriving;
                }
            }

            if (previous != state)
            {
                interactionTimer = 0f;
                LogEvent($"cambia a {state}");
            }
        }

        private InterestObject SelectInterest()
        {
            InterestObject best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < visibleInterests.Count; i++)
            {
                InterestObject candidate = visibleInterests[i];
                if (candidate == null || !candidate.IsAvailable) continue;

                float distance = AgentMath.FlatSqrDistance(transform.position, candidate.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            return best;
        }
    }
}
