#if UNITY_EDITOR
using IA.Agents;
using NUnit.Framework;
using UnityEngine;

namespace IA.EditorTools
{
    //pruebas del grafo y del A* (Window > General > Test Runner > EditMode)
    public class AStarTests
    {
        private const float CellSize = 8f;

        private NavGraph graph;
        private GameObject walls;

        [SetUp]
        public void SetUp()
        {
            graph = new GameObject("TestGraph").AddComponent<NavGraph>();
            walls = new GameObject("TestWalls");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(graph.gameObject);
            Object.DestroyImmediate(walls);
        }

        //crea una pared con collider por cada '#', asi los raycast del grafo tienen contra que chocar
        private void Build(string[] rows)
        {
            graph.Build(rows, CellSize); //primero arma el mapa para poder usar CellCenter
            for (int r = 0; r < rows.Length; r++)
            {
                for (int c = 0; c < rows[r].Length; c++)
                {
                    if (rows[r][c] != '#') continue;
                    GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wall.transform.SetParent(walls.transform);
                    wall.transform.position = graph.CellCenter(r, c) + Vector3.up * 1.5f;
                    wall.transform.localScale = new Vector3(CellSize, 3f, CellSize);
                }
            }
            Physics.SyncTransforms();
            graph.Build(rows, CellSize); //ahora si con las paredes, para que las conexiones las respeten
        }

        private static bool SegmentHitsWall(Vector3 a, Vector3 b)
        {
            return Physics.Linecast(a + Vector3.up, b + Vector3.up, 1, QueryTriggerInteraction.Ignore);
        }

        [Test]
        public void PathGoesAroundWall()
        {
            Build(new[]
            {
                "#####",
                "#S#E#",
                "#.#.#",
                "#...#",
                "#####"
            });

            var path = AStar.FindPath(graph.StartNode, graph.ExitNode, true);

            Assert.IsNotEmpty(path, "tiene que encontrar camino");
            Assert.Greater(path.Count, 2, "no puede ir directo, hay una pared en el medio");
            for (int i = 1; i < path.Count; i++)
                Assert.IsFalse(SegmentHitsWall(path[i - 1].Position, path[i].Position), "una conexion atraviesa una pared");
        }

        [Test]
        public void WolvesNeverUseSafeNodes()
        {
            Build(new[]
            {
                "#####",
                "#S1E#",
                "#.#.#",
                "#...#",
                "#####"
            });

            var hunterPath = AStar.FindPath(graph.StartNode, graph.ExitNode, true);
            var wolfPath = AStar.FindPath(graph.StartNode, graph.ExitNode, false);

            Assert.IsNotEmpty(wolfPath);
            Assert.Greater(wolfPath.Count, hunterPath.Count, "el lobo tiene que dar la vuelta, el Cazador puede cruzar la zona segura");
            foreach (NavNode node in wolfPath) Assert.AreNotEqual(Zone.Safe, node.Zone);
            Assert.IsEmpty(AStar.FindPath(graph.StartNode, graph.SafeZoneTargets[0], false));
        }

        [Test]
        public void DefaultMapConnectsAllZones()
        {
            Build(NavGraph.DefaultRows);

            Assert.AreEqual(2, graph.SafeZoneTargets.Count);
            Assert.IsNotEmpty(AStar.FindPath(graph.StartNode, graph.RandomNodeInZone(Zone.Coto), false));
            Assert.IsNotEmpty(AStar.FindPath(graph.StartNode, graph.ExitNode, false));
            foreach (NavNode safe in graph.SafeZoneTargets) Assert.IsNotEmpty(AStar.FindPath(graph.StartNode, safe, true));
            Assert.AreEqual(Zone.Coto, graph.ZoneAt(Vector3.zero), "el Coto debe estar centrado en el origen (bounds de los boids)");
        }

        [Test]
        public void GraphIsNotAGrid()
        {
            Build(NavGraph.DefaultRows);

            int walkable = 0;
            foreach (string row in NavGraph.DefaultRows)
                foreach (char cell in row)
                    if (cell != '#') walkable++;

            Assert.Less(graph.Nodes.Count, walkable, "tiene que haber menos nodos que celdas: solo puntos clave");
        }
    }
}
#endif