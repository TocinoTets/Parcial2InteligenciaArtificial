using UnityEngine;

namespace IA.Agents
{
    /// <summary>
    /// Actuadores del boid: composición de fuerzas de steering, traslación con wrap en la
    /// arena, corrección dura anti-superposición y orientación hacia la velocidad.
    /// </summary>
    public partial class BoidAgent
    {
        // ---------------- actuadores (steering) ----------------
        private void ApplySteering(float dt)
        {
            Vector3 acceleration = ComputeSteering(dt);

            velocity += acceleration * dt;
            velocity = AgentMath.Flat(velocity);
            if (velocity.magnitude > maxSpeed) velocity = velocity.normalized * maxSpeed;

            Vector3 position = WrapAroundArena(transform.position + velocity * dt);
            position.y = groundY;
            transform.position = position;

            FaceVelocity(dt);
        }

        private Vector3 ComputeSteering(float dt)
        {
            Vector3 steering = Vector3.zero;

            switch (state)
            {
                case BoidState.Evading:
                    steering += Evade() * evadeWeight;
                    // La separación se mantiene siempre: nunca debe haber superposición.
                    steering += Separation(separationRadius) * separationWeight;
                    break;

                case BoidState.Flocking:
                    steering += Separation(separationRadius) * separationWeight;
                    steering += Alignment() * alignmentWeight;
                    steering += Cohesion() * cohesionWeight;
                    steering += Wander(dt) * wanderWeight;
                    break;

                default: // Arriving / Interacting
                    steering += Separation(separationRadius) * separationWeight;
                    if (targetInterest != null) steering += Arrive(targetInterest.transform.position) * arriveWeight;
                    break;
            }

            steering += SeparationFromDead() * (separationWeight * 0.6f);

            return Vector3.ClampMagnitude(steering, maxForce);
        }

        private Vector3 SteerTo(Vector3 desired)
        {
            Vector3 steer = AgentMath.Flat(desired) - AgentMath.Flat(velocity);
            return Vector3.ClampMagnitude(steer, maxForce);
        }

        private Vector3 Separation(float radius)
        {
            Vector3 force = Vector3.zero;
            int count = 0;
            float sqrRadius = radius * radius;

            for (int i = 0; i < neighbors.Count; i++)
            {
                float sqrDistance = AgentMath.FlatSqrDistance(transform.position, neighbors[i].transform.position);
                if (sqrDistance <= 1e-4f || sqrDistance > sqrRadius) continue;

                Vector3 offset = AgentMath.Flat(transform.position - neighbors[i].transform.position);
                force += offset.normalized / Mathf.Sqrt(sqrDistance);
                count++;
            }

            if (count == 0) return Vector3.zero;

            force /= count;
            return SteerTo(force.normalized * maxSpeed);
        }

        private Vector3 SeparationFromDead()
        {
            Vector3 force = Vector3.zero;
            int count = 0;

            for (int i = 0; i < deadNeighbors.Count; i++)
            {
                float sqrDistance = AgentMath.FlatSqrDistance(transform.position, deadNeighbors[i].transform.position);
                if (sqrDistance <= 1e-4f) continue;

                Vector3 offset = AgentMath.Flat(transform.position - deadNeighbors[i].transform.position);
                force += offset.normalized / Mathf.Sqrt(sqrDistance);
                count++;
            }

            if (count == 0) return Vector3.zero;

            force /= count;
            return SteerTo(force.normalized * maxSpeed);
        }

        private Vector3 Alignment()
        {
            if (neighbors.Count == 0) return Vector3.zero;

            Vector3 sum = Vector3.zero;
            for (int i = 0; i < neighbors.Count; i++) sum += neighbors[i].Velocity;

            sum = AgentMath.Flat(sum / neighbors.Count);
            if (sum.sqrMagnitude < 1e-4f) return Vector3.zero;

            return SteerTo(sum.normalized * maxSpeed);
        }

        private Vector3 Cohesion()
        {
            if (neighbors.Count == 0) return Vector3.zero;

            Vector3 center = Vector3.zero;
            for (int i = 0; i < neighbors.Count; i++) center += neighbors[i].transform.position;
            center /= neighbors.Count;

            Vector3 desired = AgentMath.FlatDirection(transform.position, center) * maxSpeed;
            if (desired == Vector3.zero) return Vector3.zero;

            return SteerTo(desired);
        }

        /// <summary>Evade: huye de la posición futura estimada del Cazador (persecución anticipada).</summary>
        private Vector3 Evade()
        {
            // Con el Cazador a la vista se usan su posición y velocidad reales; si ya salió
            // del rango de visión se sigue huyendo del último punto donde fue detectado.
            Vector3 threatPosition = detectedHunter != null ? detectedHunter.transform.position : lastThreatPosition;
            Vector3 threatVelocity = detectedHunter != null ? detectedHunter.Velocity : Vector3.zero;

            Vector3 toThreat = AgentMath.Flat(threatPosition - transform.position);
            float distance = toThreat.magnitude;

            // Distancia de seguridad: la huida se desvanece al alejarse y se apaga bastante antes
            // de la barrera. Así el boid no sigue empujando contra el borde (y, tras el teletransporte
            // del wrap, continúa hacia dentro de la arena en vez de girar y volver a cruzar).
            float safeDistance = Mathf.Max(viewRadius + 0.01f, evadeSafeDistance);
            if (distance >= safeDistance) return Vector3.zero;

            float speedFactor = Mathf.Clamp01((safeDistance - distance) / (safeDistance - viewRadius));
            float threatSpeed = AgentMath.Flat(threatVelocity).magnitude;
            float lookAhead = Mathf.Clamp(distance / (maxSpeed + threatSpeed + 0.001f), 0f, 1.5f);

            Vector3 predicted = threatPosition + threatVelocity * lookAhead;
            Vector3 away = AgentMath.FlatDirection(predicted, transform.position);
            if (away == Vector3.zero) away = distance > 1e-4f ? -toThreat.normalized : Vector3.forward;

            return SteerTo(away * (maxSpeed * speedFactor));
        }

        /// <summary>Arrive: se aproxima al objetivo frenando según la distancia, sin superponerse con él.</summary>
        private Vector3 Arrive(Vector3 target)
        {
            Vector3 offset = AgentMath.Flat(target - transform.position);
            float distance = offset.magnitude;
            if (distance <= 1e-3f) return Vector3.zero;

            float slowingRadius = interactionDistance * 2.5f;
            float targetSpeed = maxSpeed * Mathf.Clamp01((distance - interactionDistance) / Mathf.Max(0.01f, slowingRadius - interactionDistance));

            Vector3 desired = offset / distance * targetSpeed;
            return Vector3.ClampMagnitude(desired - AgentMath.Flat(velocity), maxForce);
        }

        private Vector3 Wander(float dt)
        {
            wanderAngle += Random.Range(-1f, 1f) * 60f * dt;
            Vector3 direction = new Vector3(Mathf.Cos(wanderAngle * Mathf.Deg2Rad), 0f, Mathf.Sin(wanderAngle * Mathf.Deg2Rad));
            return SteerTo(direction * (maxSpeed * 0.6f));
        }

        /// <summary>
        /// La arena es la barrera que define los límites del mapa: al tocar uno de sus
        /// bordes el boid se transporta al lado opuesto conservando la otra coordenada.
        /// </summary>
        private Vector3 WrapAroundArena(Vector3 position)
        {
            float span = arenaHalfExtent * 2f;

            if (position.x > arenaHalfExtent) position.x -= span;
            else if (position.x < -arenaHalfExtent) position.x += span;

            if (position.z > arenaHalfExtent) position.z -= span;
            else if (position.z < -arenaHalfExtent) position.z += span;

            return position;
        }

        /// <summary>Corrección posicional dura: garantiza que ningún par de agentes se superponga.</summary>
        private void ResolveOverlaps()
        {
            Vector3 correction = Vector3.zero;

            for (int i = 0; i < neighbors.Count; i++) correction += PushAway(neighbors[i].transform.position);
            for (int i = 0; i < deadNeighbors.Count; i++) correction += PushAway(deadNeighbors[i].transform.position);
            if (detectedHunter != null) correction += PushAway(detectedHunter.transform.position);

            if (correction.sqrMagnitude < 1e-8f) return;

            Vector3 position = transform.position + correction;
            position.y = groundY;
            transform.position = position;
        }

        private Vector3 PushAway(Vector3 otherPosition)
        {
            Vector3 offset = AgentMath.Flat(transform.position - otherPosition);
            float distance = offset.magnitude;
            if (distance >= minSeparationDistance) return Vector3.zero;

            Vector3 direction = distance < 1e-3f
                ? new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized
                : offset / distance;

            return direction * ((minSeparationDistance - distance) * 0.5f);
        }

        private void FaceVelocity(float dt)
        {
            Vector3 flat = AgentMath.Flat(velocity);
            if (flat.sqrMagnitude < 0.05f) return;

            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat), turnSpeed * dt);
        }
    }
}
