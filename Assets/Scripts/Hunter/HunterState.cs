namespace IA.Agents
{
    //clase base de los estados del cazador, cada estado decide sus propias transiciones
    public abstract class HunterState
    {
        protected readonly HunterNPC npc;

        protected HunterState(HunterNPC npc)
        {
            this.npc = npc;
        }

        public abstract HunterStateId Id { get; }

        public virtual void Enter() { } //cuando entra al estado

        public abstract void Tick(float deltaTime); //cada frame

        public virtual void Exit() { } //cuando sale del estado
    }
}