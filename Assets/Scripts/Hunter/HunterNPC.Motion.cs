using UnityEngine;

namespace IA.Agents
{
    /// <summary>
    /// Actuadores de movimiento del Cazador: Seek directo (MoveTowards), Pursuit con
    /// predicción y recorrido de caminos A* sobre el grafo. El Cazador queda confinado
    /// dentro del mapa (clamp a ±arenaHalfExtent), no se teletransporta.
    /// </summary>
    public partial class HunterNPC
    {
        // ---------------- movimiento ----------------
        public void MoveTowards(Vector3 worldTarget)
        {
            Vector3 desired = AgentMath.FlatDirection(transform.position, worldTarget) * moveSpeed;

            velocity = Vector3.MoveTowards(velocity, desired, acceleration * Time.deltaTime);
            ApplyMotion();
        }

        public void StopMoving()
        {
            velocity = Vector3.MoveTowards(velocity, Vector3.zero, acceleration * 1.4f * Time.deltaTime);
            ApplyMotion();
        }

        /// <summary>Pursuit: persigue la posición futura estimada del objetivo.</summary>
        public void Pursue(BoidAgent target)
        {
            Vector3 targetPosition = target.transform.position;
            float prediction = AgentMath.FlatDistance(transform.position, targetPosition) / Mathf.Max(0.1f, moveSpeed);
            MoveTowards(targetPosition + AgentMath.Flat(target.Velocity) * prediction);
        }

        /// <summary>Calcula con A* un camino hacia target (puede atravesar Zonas seguras).</summary>
        public bool RequestPath(Vector3 target)
        {
            bool found = path.SetPath(transform.position, target, true);
            LogAction(found
                ? $"A*: camino hacia ({target.x:0.0}, {target.z:0.0}) con {path.Points.Count} puntos"
                : $"A*: sin camino hacia ({target.x:0.0}, {target.z:0.0})");
            return found;
        }

        public void FollowPath()
        {
            MoveTowards(path.NextPoint(transform.position));
        }

        /// <summary>
        /// Va hacia target: en línea recta (Seek) si está en línea de visión; si no, por A*.
        /// </summary>
        public void NavigateTo(Vector3 target)
        {
            if (Perception.HasLineOfSight(transform.position, target))
            {
                path.Clear();
                MoveTowards(target);
                return;
            }

            if (!path.IsActive || AgentMath.FlatDistance(path.Destination, target) > 1.5f) RequestPath(target);
            FollowPath();
        }

        private void ApplyMotion()
        {
            Vector3 position = transform.position + velocity * Time.deltaTime;
            position.x = Mathf.Clamp(position.x, -arenaHalfExtent, arenaHalfExtent);
            position.z = Mathf.Clamp(position.z, -arenaHalfExtent, arenaHalfExtent);
            position.y = groundY;
            transform.position = position;

            Vector3 flat = AgentMath.Flat(velocity);
            if (flat.sqrMagnitude > 0.05f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat), turnSpeed * Time.deltaTime);
        }
    }
}
