using UnityEngine;

namespace IA.Agents
{
    //movimiento del cazador: seek, pursuit y caminos A*, no puede salir del mapa
    public partial class HunterNPC
    {
        public void MoveTowards(Vector3 worldTarget) //seek directo
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

        public void Pursue(BoidAgent target) //va a donde va a estar el boid, no a donde esta
        {
            Vector3 targetPosition = target.transform.position;
            float prediction = AgentMath.FlatDistance(transform.position, targetPosition) / Mathf.Max(0.1f, moveSpeed);
            MoveTowards(targetPosition + AgentMath.Flat(target.Velocity) * prediction);
        }

        public bool RequestPath(Vector3 target) //calcula el camino con A*
        {
            return path.SetPath(transform.position, target, true);
        }

        public void FollowPath()
        {
            MoveTowards(path.NextPoint(transform.position));
        }

        public void NavigateTo(Vector3 target)
        {
            if (Perception.HasLineOfSight(transform.position, target)) //si lo ve va directo
            {
                path.Clear();
                MoveTowards(target);
                return;
            }

            if (!path.IsActive || AgentMath.FlatDistance(path.Destination, target) > 1.5f) RequestPath(target); //si no lo ve usa A*
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