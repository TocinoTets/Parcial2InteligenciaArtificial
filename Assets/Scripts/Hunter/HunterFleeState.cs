using UnityEngine;

namespace IA.Agents
{
    //vio un lobo, huye a la zona segura mas cercana por A* y espera ahi
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
            npc.ChosenSafeZone = ChooseSafeZone(out _);

            if (npc.ChosenSafeZone == null) return; //no hay zona segura alcanzable

            npc.Path.SetPath(npc.Position, npc.ChosenSafeZone.Position, true);
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
                if (!npc.IsInSafeZone || npc.Path.IsActive) //todavia no llego, sigue el camino
                {
                    if (!npc.Path.IsActive) npc.Path.SetPath(npc.Position, safeZone.Position, true);
                    npc.FollowPath();
                    return;
                }

                waiting = true;
            }

            npc.StopMoving();
            waitTimer += deltaTime;
            npc.ActionProgress = Mathf.Clamp01(waitTimer / Mathf.Max(0.01f, npc.SafeZoneWaitTime));
            if (waitTimer < npc.SafeZoneWaitTime) return;

            npc.FSM.ChangeState(npc.Gathers >= npc.RequiredGathers ? HunterStateId.Exit : HunterStateId.Search); //si ya junto todo va a la salida
        }

        public override void Exit()
        {
            npc.ActionProgress = 0f;
            npc.ChosenSafeZone = null;
            npc.Path.Clear();
        }

        private NavNode ChooseSafeZone(out float bestLength) //elige la zona con el camino A* mas corto
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