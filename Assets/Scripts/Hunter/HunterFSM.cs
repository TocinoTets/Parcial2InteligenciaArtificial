using System.Collections.Generic;

namespace IA.Agents
{
    /// <summary>Identificadores de los estados del NPC Cazador.</summary>
    public enum HunterStateId
    {
        Patrol,
        Attack,
        Gather
    }

    /// <summary>
    /// Máquina de Estados Finita (FSM) del NPC Cazador.
    /// Es la ÚNICA responsable de las transiciones: los estados evalúan sus propias
    /// condiciones y piden el cambio a través de ChangeState. Ningún sistema externo
    /// dispara transiciones.
    /// </summary>
    public class HunterFSM
    {
        private readonly HunterNPC npc;
        private readonly Dictionary<HunterStateId, HunterState> states = new Dictionary<HunterStateId, HunterState>();

        public HunterState CurrentState { get; private set; }
        public HunterStateId CurrentId => CurrentState != null ? CurrentState.Id : HunterStateId.Patrol;
        public float TimeInState { get; private set; }

        public HunterFSM(HunterNPC npc)
        {
            this.npc = npc;
        }

        public void Register(HunterState state)
        {
            if (state == null) return;
            states[state.Id] = state;
        }

        /// <summary>Cambia de estado. Sólo debe invocarse desde dentro de un estado.</summary>
        public void ChangeState(HunterStateId id)
        {
            if (!states.TryGetValue(id, out HunterState next)) return;
            if (CurrentState != null && CurrentState.Id == id) return;

            CurrentState?.Exit();

            CurrentState = next;
            TimeInState = 0f;

            npc.LogAction($"FSM → {id}");
            CurrentState.Enter();
        }

        /// <summary>Avanza el estado actual. La evaluación de permanencia/transición vive dentro del estado.</summary>
        public void Tick(float deltaTime)
        {
            if (CurrentState == null) return;
            TimeInState += deltaTime;
            CurrentState.Tick(deltaTime);
        }
    }
}
