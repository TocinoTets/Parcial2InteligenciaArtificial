using UnityEngine;

namespace IA.Agents
{
    //solo entra si el TBA termino y hay un boid en rango
    public class HunterAttackState : HunterState
    {
        private BoidAgent target;
        private float timer;

        public HunterAttackState(HunterNPC npc) : base(npc) { }

        public override HunterStateId Id => HunterStateId.Attack;

        public override void Enter()
        {
            timer = 0f;
            target = npc.ClosestVisibleBoid;
            npc.StopMoving();

            if (target == null)
            {
                npc.FSM.ChangeState(HunterStateId.Search);
                return;
            }
        }

        public override void Tick(float deltaTime)
        {
            timer += deltaTime;

            if (target == null || !target.IsAlive)
            {
                npc.FSM.ChangeState(HunterStateId.Search);
                return;
            }

            float distance = AgentMath.FlatDistance(npc.Position, target.transform.position);

            if (!npc.IsInVision(target.transform.position)) //lo perdio de vista, vuelve a Search sin reiniciar el TBA
            {
                npc.FSM.ChangeState(HunterStateId.Search);
                return;
            }

            if (distance <= npc.MeleeAttackRadius) //cuerpo a cuerpo
            {
                if (distance > npc.MeleeStopDistance)
                {
                    npc.MoveTowards(target.transform.position);
                    return;
                }

                npc.StopMoving();
                if (timer < npc.AttackWindup) return;

                npc.PerformMeleeAttack(target);
                npc.ResetAttackTimer();
                npc.FSM.ChangeState(HunterStateId.Search);
                return;
            }

            if (distance <= npc.RangeAttackRadius) //a distancia
            {
                npc.StopMoving();
                if (timer < npc.AttackWindup) return;

                npc.PerformRangedAttack(target);
                npc.ResetAttackTimer();
                npc.FSM.ChangeState(HunterStateId.Search);
                return;
            }

            npc.Pursue(target); //lo persigue prediciendo donde va a estar
        }
    }
}