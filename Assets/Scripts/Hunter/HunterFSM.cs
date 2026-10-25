using System.Collections.Generic;

namespace IA.Agents
{
    /// <summary>Identificadores de los estados del NPC Cazador.</summary>
    public enum HunterStateId
    {
        Search,
        Attack,
        Gather,
        Flee,
        Exit,
        Dead
    }

    /// <summary>
    /// Máquina de Estados Finita (FSM) del NPC Cazador.
    /// Es la ÚNICA responsable de las transiciones: los estados evalúan sus propias
    /// condiciones y piden el cambio a través de ChangeState, y las transiciones globales
    /// (desde cualquier estado) se evalúan aquí, por prioridad: Dead > Flee.
    /// Ningún sistema externo dispara transiciones.
    /// </summary>
    public class HunterFSM
    {
        private readonly HunterNPC npc;
        private readonly Dictionary<HunterStateId, HunterState> states = new Dictionary<HunterStateId, HunterState>();

        public HunterState CurrentState { get; private set; }
        public HunterStateId CurrentId => CurrentState != null ? CurrentState.Id : HunterStateId.Search;
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

        /// <summary>Cambia de estado. Sólo debe invocarse desde dentro de la FSM o de un estado.</summary>
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

        /// <summary>Evalúa las transiciones globales y luego avanza el estado actual.</summary>
        public void Tick(float deltaTime)
        {
            if (CurrentState == null) return;

            // Transiciones desde cualquier estado, en orden de prioridad.
            if (npc.WasCaught && CurrentId != HunterStateId.Dead)
                ChangeState(HunterStateId.Dead);
            else if (npc.VisibleWolf != null && CurrentId != HunterStateId.Dead && CurrentId != HunterStateId.Flee)
                ChangeState(HunterStateId.Flee);

            TimeInState += deltaTime;
            CurrentState.Tick(deltaTime);
        }
    }
}
