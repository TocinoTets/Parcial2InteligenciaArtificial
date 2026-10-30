using UnityEngine;

namespace IA.Agents
{

    public partial class BoidAgent
    {
        [Header("Optimización")]
        [SerializeField] private float senseInterval = 0.1f; //cada cuanto percibe, no hace falta hacerlo todos los frames

        private float senseTimer;

        // ---------------- sensores ----------------
        private void Sense()
        {
            senseTimer -= Time.deltaTime;
            if (senseTimer > 0f) return;
            senseTimer = senseInterval * Random.Range(0.8f, 1.2f); //un poco random para que no perciban todos en el mismo frame

            neighbors.Clear();
            deadNeighbors.Clear();
            visibleInterests.Clear();
            detectedHunter = null;

            float queryRadius = Mathf.Max(Mathf.Max(perceptionRadius, viewRadius), Mathf.Max(interestSightRadius, separationRadius));
            int count = Physics.OverlapSphereNonAlloc(transform.position, queryRadius, overlapBuffer, Perception.AgentsMask, QueryTriggerInteraction.Collide); //solo devuelve agentes

            float closestHunter = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Collider other = overlapBuffer[i];
                if (other == null || other == bodyCollider) continue;

                if (other.TryGetComponent(out BoidAgent otherBoid))
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

                if (other.TryGetComponent(out HunterNPC hunter))
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