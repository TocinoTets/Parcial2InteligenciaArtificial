using UnityEngine;

namespace IA.Agents
{
    /// <summary>
    /// Estado Gather: se alcanza cuando el Cazador detecta un Boid eliminado dentro de
    /// su rango de percepción. Se dirige a él, ejecuta la recolección durante un tiempo
    /// determinado y el agente recolectado desaparece.
    /// Si el objetivo deja de estar disponible antes de completar la recolección,
    /// abandona la acción y vuelve al comportamiento correspondiente (Patrol).
    /// </summary>
    public class HunterGatherState : HunterState
    {
        private BoidAgent target;
        private float collectTimer;
        private bool collecting;

        public HunterGatherState(HunterNPC npc) : base(npc) { }

        public override HunterStateId Id => HunterStateId.Gather;

        public override void Enter()
        {
            collectTimer = 0f;
            collecting = false;
            target = npc.DetectedDeadBoid;
            npc.ActionProgress = 0f;

            if (target == null)
            {
                Log("Gather: sin agente eliminado disponible");
                npc.FSM.ChangeState(HunterStateId.Patrol);
                return;
            }

            Log($"Gather: agente eliminado detectado {target.name} → se dirige a recolectarlo");
        }

        public override void Tick(float deltaTime)
        {
            if (target == null || !target.IsHarvestable)
            {
                Log("Gather: el objetivo dejó de estar disponible → abandona la recolección");
                npc.ActionProgress = 0f;
                npc.FSM.ChangeState(HunterStateId.Patrol);
                return;
            }

            float distance = AgentMath.FlatDistance(npc.Position, target.transform.position);

            if (distance > npc.GatherRadius)
            {
                collecting = false;
                npc.ActionProgress = 0f;
                npc.MoveTowards(target.transform.position);
                return;
            }

            npc.StopMoving();

            if (!collecting)
            {
                collecting = true;
                collectTimer = 0f;
                Log($"Gather: iniciando recolección de {target.name} ({npc.GatherDuration:0.#} s)");
            }

            collectTimer += deltaTime;
            npc.ActionProgress = Mathf.Clamp01(collectTimer / Mathf.Max(0.01f, npc.GatherDuration));

            if (collectTimer < npc.GatherDuration) return;

            BoidAgent harvested = target;
            harvested.Harvest();
            npc.ActionProgress = 0f;
            Log($"Gather: {harvested.name} recolectado → desaparece del escenario");

            npc.FSM.ChangeState(HunterStateId.Patrol);
        }

        public override void Exit()
        {
            collecting = false;
            npc.ActionProgress = 0f;
        }
    }
}
