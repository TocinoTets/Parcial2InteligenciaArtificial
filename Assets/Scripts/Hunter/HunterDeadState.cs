using UnityEngine;

namespace IA.Agents
{
    //lo mato un lobo, desaparece y reaparece en la zona inicial
    public class HunterDeadState : HunterState
    {
        private float timer;

        public HunterDeadState(HunterNPC npc) : base(npc) { }

        public override HunterStateId Id => HunterStateId.Dead;

        public override void Enter()
        {
            timer = 0f;
            npc.Path.Clear();
            npc.SetVisible(false);
        }

        public override void Tick(float deltaTime)
        {
            timer += deltaTime;
            npc.ActionProgress = Mathf.Clamp01(timer / Mathf.Max(0.01f, npc.RespawnTime));
            if (timer < npc.RespawnTime) return;

            npc.RespawnAtStart(); //vuelve con las recolecciones en cero
            npc.FSM.ChangeState(HunterStateId.Search);
        }

        public override void Exit()
        {
            npc.ActionProgress = 0f;
        }
    }
}