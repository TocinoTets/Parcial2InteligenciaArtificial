using UnityEngine;

namespace IA.Agents
{
    /// <summary>
    /// Actuadores de movimiento del Cazador. A diferencia de los boids, el Cazador queda
    /// confinado dentro de la arena (clamp a ±arenaHalfExtent), no se teletransporta.
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
