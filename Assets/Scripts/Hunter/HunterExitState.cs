namespace IA.Agents
{
    /// <summary>
    /// Estado Exit: con RequiredGathers completas, se dirige a la Salida por A* sin atacar ni
    /// recolectar. Al llegar completa su objetivo y reaparece en la Zona inicial (Search).
    /// </summary>
    public class HunterExitState : HunterState
    {
        public HunterExitState(HunterNPC npc) : base(npc) { }

        public override HunterStateId Id => HunterStateId.Exit;

        public override void Enter()
        {
            npc.Path.Clear();
            Log($"Exit: {npc.Gathers}/{npc.RequiredGathers} recolecciones → A* hacia la Salida");
        }

        public override void Tick(float deltaTime)
        {
            NavGraph graph = NavGraph.Instance;
            if (graph == null || graph.ExitNode == null)
            {
                npc.StopMoving();
                return;
            }

            if (npc.CurrentZone == Zone.Exit)
            {
                Log("Exit: llegó a la Salida → objetivo cumplido");
                npc.RespawnAtStart();
                npc.FSM.ChangeState(HunterStateId.Search);
                return;
            }

            if (!npc.Path.IsActive) npc.RequestPath(graph.ExitNode.Position);
            npc.FollowPath();
        }
    }
}
