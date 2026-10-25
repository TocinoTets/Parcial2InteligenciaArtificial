#if UNITY_EDITOR
using IA.Agents;
using NUnit.Framework;
using UnityEngine;

namespace IA.EditorTools
{
    /// <summary>Pruebas EditMode del grafo y del A* (Window > General > Test Runner).</summary>
    public class AStarTests
    {
        private NavGraph graph;

        [SetUp]
        public void SetUp()
        {
            graph = new GameObject("TestGraph").AddComponent<NavGraph>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(graph.gameObject);
        }

        [Test]
        public void PathGoesAroundWallWithoutCuttingCorners()
        {
            graph.Build(new[]
            {
                "#####",
                "#S#E#",
                "#.#.#",
                "#...#",
                "#####"
            }, 1f);

            var path = AStar.FindPath(graph.StartNode, graph.ExitNode, true);

            Assert.AreEqual(7, path.Count, "debe bordear la pared por abajo");
            foreach (NavNode node in path) Assert.AreNotEqual('#', graph.CharAt(node.Row, node.Col));
            for (int i = 1; i < path.Count; i++)
            {
                NavNode a = path[i - 1], b = path[i];
                if (a.Row != b.Row && a.Col != b.Col)
                    Assert.IsTrue(graph.NodeAt(a.Row, b.Col) != null && graph.NodeAt(b.Row, a.Col) != null, "diagonal que corta una pared");
            }
        }

        [Test]
        public void WolvesNeverUseSafeNodes()
        {
            graph.Build(new[]
            {
                "#####",
                "#S1E#",
                "#.#.#",
                "#...#",
                "#####"
            }, 1f);

            var hunterPath = AStar.FindPath(graph.StartNode, graph.ExitNode, true);
            var wolfPath = AStar.FindPath(graph.StartNode, graph.ExitNode, false);

            Assert.AreEqual(3, hunterPath.Count, "el Cazador puede cruzar la zona segura");
            Assert.IsTrue(wolfPath.Count > 3);
            foreach (NavNode node in wolfPath) Assert.AreNotEqual(Zone.Safe, node.Zone);
            Assert.IsEmpty(AStar.FindPath(graph.StartNode, graph.SafeZoneTargets[0], false));
        }

        [Test]
        public void DefaultMapConnectsAllZones()
        {
            graph.Build(NavGraph.DefaultRows, 8f);

            Assert.AreEqual(2, graph.SafeZoneTargets.Count);
            Assert.IsNotEmpty(AStar.FindPath(graph.StartNode, graph.RandomNodeInZone(Zone.Coto), false));
            Assert.IsNotEmpty(AStar.FindPath(graph.StartNode, graph.ExitNode, false));
            foreach (NavNode safe in graph.SafeZoneTargets) Assert.IsNotEmpty(AStar.FindPath(graph.StartNode, safe, true));
            Assert.AreEqual(Zone.Coto, graph.ZoneAt(Vector3.zero), "el Coto debe estar centrado en el origen (bounds de los boids)");
        }
    }
}
#endif
