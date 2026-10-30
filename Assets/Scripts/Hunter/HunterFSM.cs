using System.Collections.Generic;

namespace IA.Agents
{
    public enum HunterStateId //estados del cazador
    {
        Search,
        Attack,
        Gather,
        Flee,
        Exit,
        Dead
    }

    //FSM del cazador, es la unica que maneja los cambios de estado
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

        public void ChangeState(HunterStateId id)
        {
            if (!states.TryGetValue(id, out HunterState next)) return;
            if (CurrentState != null && CurrentState.Id == id) return; //si ya esta en ese estado no hace nada

            CurrentState?.Exit();

            CurrentState = next;
            TimeInState = 0f;

            CurrentState.Enter();
        }

        public void Tick(float deltaTime)
        {
            if (CurrentState == null) return;

            //transiciones desde cualquier estado, Dead tiene prioridad sobre Flee
            if (npc.WasCaught && CurrentId != HunterStateId.Dead)
                ChangeState(HunterStateId.Dead);
            else if (npc.VisibleWolf != null && CurrentId != HunterStateId.Dead && CurrentId != HunterStateId.Flee)
                ChangeState(HunterStateId.Flee);

            TimeInState += deltaTime;
            CurrentState.Tick(deltaTime);
        }
    }
}