using UnityEngine;

namespace IA.Agents
{
    /// <summary>
    /// Estado Attack: sólo se alcanza si el TBA ya finalizó y existe al menos un Boid
    /// dentro del rango de percepción (condición evaluada en Search).
    /// Al concretar un ataque exitoso reinicia el TBA y abandona el estado; si el
    /// objetivo sale del rango de visión vuelve a Search SIN reiniciar el TBA.
    /// </summary>
    public class HunterAttackState : HunterState
    {
        private BoidAgent target;
        private float timer;
        private float chaseLogTimer;

        public HunterAttackState(HunterNPC npc) : base(npc) { }

        public override HunterStateId Id => HunterStateId.Attack;

        public override void Enter()
        {
            timer = 0f;
            chaseLogTimer = 0f;
            target = npc.ClosestVisibleBoid;
            npc.StopMoving();

            if (target == null)
            {
                npc.FSM.ChangeState(HunterStateId.Search);
                return;
            }

            float distance = AgentMath.FlatDistance(npc.Position, target.transform.position);
            int detected = npc.DetectedBoids.Count;
            Log($"Attack: objetivo {target.name} a {distance:0.0} m | TBA cumplido | boids detectados {detected}");
        }

        public override void Tick(float deltaTime)
        {
            timer += deltaTime;

            if (target == null || !target.IsAlive)
            {
                Log("Attack: el objetivo ya no está vivo → abandona Attack");
                npc.FSM.ChangeState(HunterStateId.Search);
                return;
            }

            float distance = AgentMath.FlatDistance(npc.Position, target.transform.position);

            // El objetivo abandona el rango de visión → Search SIN reiniciar el TBA.
            if (!npc.IsInVision(target.transform.position))
            {
                Log($"Attack: objetivo fuera del rango de visión ({distance:0.0} m) → Search (TBA no se reinicia)");
                npc.FSM.ChangeState(HunterStateId.Search);
                return;
            }

            // Dentro de MeleeAttackRadius: perseguir hasta quedar en rango y atacar cuerpo a cuerpo.
            if (distance <= npc.MeleeAttackRadius)
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
                Log("Attack: ataque cuerpo a cuerpo concretado → reinicia TBA y abandona Attack");
                npc.FSM.ChangeState(HunterStateId.Search);
                return;
            }

            // Fuera de melee pero dentro de RangeAttackRadius: ataque a distancia.
            if (distance <= npc.RangeAttackRadius)
            {
                npc.StopMoving();
                if (timer < npc.AttackWindup) return;

                npc.PerformRangedAttack(target);
                npc.ResetAttackTimer();
                Log("Attack: ataque a distancia concretado → reinicia TBA y abandona Attack");
                npc.FSM.ChangeState(HunterStateId.Search);
                return;
            }

            // Fuera de todo rango de ataque: perseguir hasta poder atacar.
            npc.Pursue(target); // Pursuit con predicción de su posición futura

            chaseLogTimer += deltaTime;
            if (chaseLogTimer < 2f) return;

            chaseLogTimer = 0f;
            Log($"Attack: persiguiendo a {target.name} ({distance:0.0} m > RangeAttackRadius {npc.RangeAttackRadius:0.0})");
        }
    }
}
