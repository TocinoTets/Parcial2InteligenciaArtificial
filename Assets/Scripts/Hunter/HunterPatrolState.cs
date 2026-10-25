using UnityEngine;

namespace IA.Agents
{
    /// <summary>
    /// Estado Patrol: recorre el circuito de waypoints y genera objetos de interés
    /// (siempre que existan menos de maxActiveInterestObjects activos).
    /// Las transiciones a Gather / Attack las decide este mismo estado.
    /// </summary>
    public class HunterPatrolState : HunterState
    {
        private int waypointIndex;
        private int direction = 1;
        private float spawnTimer;

        public HunterPatrolState(HunterNPC npc) : base(npc) { }

        public override HunterStateId Id => HunterStateId.Patrol;

        public override void Enter()
        {
            if (npc.Waypoints != null && npc.Waypoints.Length > 0)
            {
                waypointIndex = Mathf.Clamp(waypointIndex, 0, npc.Waypoints.Length - 1);
                Log($"Patrol: recorriendo waypoint {waypointIndex + 1}/{npc.Waypoints.Length} (modo {npc.LoopMode})");
            }
            else
            {
                Log("Patrol: sin waypoints configurados");
            }
        }

        public override void Tick(float deltaTime)
        {
            // ---- Transiciones (gestionadas por la propia FSM) ----
            if (npc.DetectedDeadBoid != null)
            {
                npc.FSM.ChangeState(HunterStateId.Gather);
                return;
            }

            if (npc.IsAttackReady && npc.ClosestVisibleBoid != null)
            {
                npc.FSM.ChangeState(HunterStateId.Attack);
                return;
            }

            // ---- Desplazamiento por waypoints ----
            if (npc.Waypoints == null || npc.Waypoints.Length == 0)
            {
                npc.StopMoving();
            }
            else
            {
                Transform waypoint = npc.Waypoints[waypointIndex];

                if (waypoint != null && AgentMath.FlatDistance(npc.Position, waypoint.position) <= npc.WaypointTolerance)
                {
                    AdvanceWaypoint();
                    waypoint = npc.Waypoints[waypointIndex];
                }

                if (waypoint != null) npc.MoveTowards(waypoint.position);
            }

            // ---- Generación de objetos de interés ----
            spawnTimer += deltaTime;
            if (spawnTimer < npc.InterestSpawnInterval) return;

            spawnTimer = 0f;

            int active = npc.ActiveInterestObjectCount();
            if (active < npc.MaxActiveInterestObjects) npc.SpawnInterestObject();
            else Log($"Patrol: no genera objeto de interés ({active}/{npc.MaxActiveInterestObjects} activos)");
        }

        private void AdvanceWaypoint()
        {
            if (npc.LoopMode == WaypointLoopMode.Loop)
            {
                waypointIndex = (waypointIndex + 1) % npc.Waypoints.Length; // vuelve al primer waypoint
                Log("Patrol: fin del recorrido → vuelve al primer waypoint");
                return;
            }

            if (npc.Waypoints.Length == 1) return;

            if (waypointIndex + direction >= npc.Waypoints.Length || waypointIndex + direction < 0)
            {
                direction = -direction;
                Log("Patrol: fin del recorrido → recorre los waypoints en sentido inverso");
            }

            waypointIndex += direction;
        }
    }
}
