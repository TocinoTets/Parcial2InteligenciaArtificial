using UnityEngine;

namespace IA.Agents
{
    /// <summary>
    /// Estado Flee: la FSM lo activa (prioridad sobre todo salvo Dead) cuando el Cazador
    /// percibe un lobo. Elige la Zona segura más cercana según la longitud del camino A*,
    /// la recorre sin atacar ni recolectar, y espera SafeZoneWaitTime dentro de ella antes
    /// de volver a Exit (si ya completó las recolecciones) o a Search.
    /// </summary>
    public class HunterFleeState : HunterState
    {
        private float waitTimer;
        private bool waiting;

        public HunterFleeState(HunterNPC npc) : base(npc) { }

        public override HunterStateId Id => HunterStateId.Flee;

        public override void Enter()
        {
            waiting = false;
            waitTimer = 0f;
            npc.ActionProgress = 0f;
            npc.ChosenSafeZone = ChooseSafeZone(out float length);

            string wolfName = npc.VisibleWolf != null ? npc.VisibleWolf.name : "?";
            if (npc.ChosenSafeZone == null)
            {
                Log($"Flee: lobo {wolfName} detectado, pero no hay Zona segura alcanzable");
                return;
            }

            npc.Path.SetPath(npc.Position, npc.ChosenSafeZone.Position, true);
            Log($"Flee: lobo {wolfName} detectado → huye a la Zona segura {npc.ChosenSafeZone.SafeZoneId} (A* {length:0.0} m)");
        }

        public override void Tick(float deltaTime)
        {
            NavNode safeZone = npc.ChosenSafeZone;
            if (safeZone == null)
            {
                npc.FSM.ChangeState(HunterStateId.Search);
                return;
            }

            if (!waiting)
            {
                if (!npc.IsInSafeZone || npc.Path.IsActive)
                {
                    if (!npc.Path.IsActive) npc.Path.SetPath(npc.Position, safeZone.Position, true);
                    npc.FollowPath();
                    return;
                }

                waiting = true;
                Log($"Flee: dentro de la Zona segura {safeZone.SafeZoneId} → espera {npc.SafeZoneWaitTime:0.#} s");
            }

            npc.StopMoving();
            waitTimer += deltaTime;
            npc.ActionProgress = Mathf.Clamp01(waitTimer / Mathf.Max(0.01f, npc.SafeZoneWaitTime));
            if (waitTimer < npc.SafeZoneWaitTime) return;

            Log("Flee: terminó la espera → retoma su actividad");
            npc.FSM.ChangeState(npc.Gathers >= npc.RequiredGathers ? HunterStateId.Exit : HunterStateId.Search);
        }

        public override void Exit()
        {
            npc.ActionProgress = 0f;
            npc.ChosenSafeZone = null;
            npc.Path.Clear();
        }

        /// <summary>Zona segura con el camino A* más corto desde la posición actual.</summary>
        private NavNode ChooseSafeZone(out float bestLength)
        {
            bestLength = float.PositiveInfinity;
            NavGraph graph = NavGraph.Instance;
            if (graph == null) return null;

            NavNode start = graph.ClosestNode(npc.Position, true);
            NavNode best = null;
            foreach (NavNode target in graph.SafeZoneTargets)
            {
                float length = AStar.PathLength(AStar.FindPath(start, target, true));
                if (length >= bestLength) continue;
                bestLength = length;
                best = target;
            }
            return best;
        }
    }
}
