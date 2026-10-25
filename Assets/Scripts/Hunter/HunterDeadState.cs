using UnityEngine;

namespace IA.Agents
{
    /// <summary>
    /// Estado Dead: la FSM lo activa desde cualquier estado cuando un lobo alcanza al Cazador.
    /// Desaparece de la escena sin responder a nada y, pasado RespawnTime, reaparece en la
    /// Zona inicial con las recolecciones en cero y vuelve a Search.
    /// </summary>
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
            Log($"Dead: alcanzado por un lobo → desaparece, reaparece en {npc.RespawnTime:0.#} s");
        }

        public override void Tick(float deltaTime)
        {
            timer += deltaTime;
            npc.ActionProgress = Mathf.Clamp01(timer / Mathf.Max(0.01f, npc.RespawnTime));
            if (timer < npc.RespawnTime) return;

            npc.RespawnAtStart();
            npc.FSM.ChangeState(HunterStateId.Search);
        }

        public override void Exit()
        {
            npc.ActionProgress = 0f;
        }
    }
}
