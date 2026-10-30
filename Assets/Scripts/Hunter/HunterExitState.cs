namespace IA.Agents
{
    //junto todas las recolecciones, va a la salida por A* sin atacar ni recolectar
    public class HunterExitState : HunterState
    {
        public HunterExitState(HunterNPC npc) : base(npc) { }

        public override HunterStateId Id => HunterStateId.Exit;

        public override void Enter()
        {
            npc.Path.Clear();
        }

        public override void Tick(float deltaTime)
        {
            NavGraph graph = NavGraph.Instance;
            if (graph == null || graph.ExitNode == null) //si no hay grafo o salida se queda quieto
            {
                npc.StopMoving();
                return;
            }

            if (npc.CurrentZone == Zone.Exit) //llego a la salida, objetivo cumplido
            {
                npc.RespawnAtStart();
                npc.FSM.ChangeState(HunterStateId.Search);
                return;
            }

            if (!npc.Path.IsActive) npc.RequestPath(graph.ExitNode.Position);
            npc.FollowPath();
        }
    }
}