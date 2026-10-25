namespace IA.Agents
{
    /// <summary>
    /// Estado Search (reemplaza a Patrol): calcula con A* el camino hasta el Coto de caza y,
    /// una vez dentro, lo recorre yendo a nodos del Coto elegidos al azar. Mientras está en
    /// el Coto genera objetos de interés (siempre que existan menos de maxActiveInterestObjects).
    /// Las transiciones a Exit / Gather / Attack las decide este mismo estado.
    /// </summary>
    public class HunterSearchState : HunterState
    {
        private float spawnTimer;

        public HunterSearchState(HunterNPC npc) : base(npc) { }

        public override HunterStateId Id => HunterStateId.Search;

        public override void Enter()
        {
            npc.Path.Clear();
            Log($"Search: recolecciones {npc.Gathers}/{npc.RequiredGathers}");
        }

        public override void Tick(float deltaTime)
        {
            // ---- Transiciones (gestionadas por la propia FSM) ----
            if (npc.Gathers >= npc.RequiredGathers)
            {
                npc.FSM.ChangeState(HunterStateId.Exit);
                return;
            }

            if (npc.DetectedDeadBoid != null)
            {
                npc.FSM.ChangeState(HunterStateId.Gather);
                return;
            }

            if (npc.IsAttackReady && npc.ClosestVisibleBoid != null)
            {
                npc.FSM.ChangeState(HunterStateId.Attack);
                return;
            }

            // ---- Desplazamiento por A*: hacia el Coto o entre nodos del Coto ----
            NavGraph graph = NavGraph.Instance;
            if (graph == null)
            {
                npc.StopMoving();
                return;
            }

            bool inCoto = npc.CurrentZone == Zone.Coto;

            if (!npc.Path.IsActive)
            {
                NavNode next = graph.RandomNodeInZone(Zone.Coto);
                if (next != null)
                {
                    Log(inCoto ? "Search: sin boids a la vista → recorre el Coto (nodo al azar)" : "Search: A* hacia el Coto de caza");
                    npc.RequestPath(next.Position);
                }
            }

            npc.FollowPath();

            // ---- Generación de objetos de interés (sólo dentro del Coto) ----
            if (!inCoto) return;

            spawnTimer += deltaTime;
            if (spawnTimer < npc.InterestSpawnInterval) return;

            spawnTimer = 0f;

            int active = npc.ActiveInterestObjectCount();
            if (active < npc.MaxActiveInterestObjects) npc.SpawnInterestObject();
            else Log($"Search: no genera objeto de interés ({active}/{npc.MaxActiveInterestObjects} activos)");
        }
    }
}
