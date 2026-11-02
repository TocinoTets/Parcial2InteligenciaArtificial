using UnityEngine;

namespace IA.Agents
{
    //vio un boid muerto, va hasta el y lo recolecta
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
                npc.FSM.ChangeState(HunterStateId.Search);
                return;
            }
        }

        public override void Tick(float deltaTime)
        {
            if (target == null || !target.IsHarvestable) //el objetivo ya no esta, vuelve a Search
            {
                npc.ActionProgress = 0f;
                npc.FSM.ChangeState(HunterStateId.Search);
                return;
            }

            float distance = AgentMath.FlatDistance(npc.Position, target.transform.position);

            if (distance > npc.GatherRadius)
            {
                collecting = false;
                npc.ActionProgress = 0f;
                npc.NavigateTo(target.transform.position, npc.DetectedDeadBoid == target); //seek si lo ve, A* si no
                return;
            }

            npc.StopMoving();

            if (!collecting)
            {
                collecting = true;
                collectTimer = 0f;
            }

            collectTimer += deltaTime;
            npc.ActionProgress = Mathf.Clamp01(collectTimer / Mathf.Max(0.01f, npc.GatherDuration));

            if (collectTimer < npc.GatherDuration) return;

            target.Harvest(); //el boid desaparece
            npc.ActionProgress = 0f;
            npc.AddGather();

            npc.FSM.ChangeState(npc.Gathers >= npc.RequiredGathers ? HunterStateId.Exit : HunterStateId.Search); //si ya junto todo va a la salida
        }

        public override void Exit()
        {
            collecting = false;
            npc.ActionProgress = 0f;
        }
    }
}