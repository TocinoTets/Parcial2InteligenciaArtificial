namespace IA.Agents
{
    /// <summary>
    /// Estado de la FSM del Cazador. Cada estado concreto decide sus propias
    /// transiciones (llamando a npc.FSM.ChangeState) además de su comportamiento.
    /// </summary>
    public abstract class HunterState
    {
        protected readonly HunterNPC npc;

        protected HunterState(HunterNPC npc)
        {
            this.npc = npc;
        }

        public abstract HunterStateId Id { get; }

        public virtual void Enter() { }

        public abstract void Tick(float deltaTime);

        public virtual void Exit() { }

        protected void Log(string action)
        {
            npc.LogAction(action);
        }
    }
}
