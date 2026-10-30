namespace IA.Agents
{
    //va al Coto por A*, lo recorre por nodos al azar y genera objetos de interes
    public class HunterSearchState : HunterState
    {
        private float spawnTimer;

        public HunterSearchState(HunterNPC npc) : base(npc) { }

        public override HunterStateId Id => HunterStateId.Search;

        public override void Enter()
        {
            npc.Path.Clear();
        }

        public override void Tick(float deltaTime)
        {
            if (npc.Gathers >= npc.RequiredGathers) //ya junto todo, va a la salida
            {
                npc.FSM.ChangeState(HunterStateId.Exit);
                return;
            }

            if (npc.DetectedDeadBoid != null) //vio un boid muerto, lo va a recolectar
            {
                npc.FSM.ChangeState(HunterStateId.Gather);
                return;
            }

            if (npc.IsAttackReady && npc.ClosestVisibleBoid != null) //TBA cumplido y hay un boid a la vista
            {
                npc.FSM.ChangeState(HunterStateId.Attack);
                return;
            }

            NavGraph graph = NavGraph.Instance;
            if (graph == null)
            {
                npc.StopMoving();
                return;
            }

            bool inCoto = npc.CurrentZone == Zone.Coto;

            if (!npc.Path.IsActive) //si no tiene camino elige un nodo random del Coto
            {
                NavNode next = graph.RandomNodeInZone(Zone.Coto);
                if (next != null) npc.RequestPath(next.Position);
            }

            npc.FollowPath();

            if (!inCoto) return; //solo genera objetos dentro del Coto

            spawnTimer += deltaTime;
            if (spawnTimer < npc.InterestSpawnInterval) return;

            spawnTimer = 0f;

            if (npc.ActiveInterestObjectCount() < npc.MaxActiveInterestObjects) npc.SpawnInterestObject();
        }
    }
}