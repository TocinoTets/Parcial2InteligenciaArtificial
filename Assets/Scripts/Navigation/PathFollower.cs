using System.Collections.Generic;
using UnityEngine;

namespace IA.Agents
{
    /// <summary>
    /// Calcula un camino con A* sobre el NavGraph y lo recorre punto a punto.
    /// Si el punto siguiente ya está en línea de visión, salta al siguiente (suavizado).
    /// </summary>
    public class PathFollower
    {
        private readonly List<Vector3> points = new List<Vector3>(32);
        private int index;

        public float Tolerance = 0.8f;

        public IReadOnlyList<Vector3> Points => points;
        public Vector3 Destination { get; private set; }
        /// <summary>Hay un camino pendiente de recorrer.</summary>
        public bool IsActive => index < points.Count;

        public bool SetPath(Vector3 from, Vector3 to, bool allowSafe)
        {
            Clear();
            Destination = to;

            NavGraph graph = NavGraph.Instance;
            if (graph == null) return false;

            List<NavNode> nodes = AStar.FindPath(graph.ClosestNode(from, allowSafe), graph.ClosestNode(to, allowSafe), allowSafe);
            if (nodes.Count == 0) return false;

            foreach (NavNode node in nodes) points.Add(node.Position);
            if (allowSafe || graph.ZoneAt(to) != Zone.Safe) points.Add(to);
            return true;
        }

        public void Clear()
        {
            points.Clear();
            index = 0;
        }

        public Vector3 NextPoint(Vector3 position)
        {
            while (index < points.Count && AgentMath.FlatDistance(position, points[index]) <= Tolerance) index++;
            while (index < points.Count - 1 && Perception.HasLineOfSight(position, points[index + 1])) index++;
            return index < points.Count ? points[index] : position;
        }

        public void DrawDebug(Vector3 from, Color color)
        {
            if (!IsActive) return;

            Vector3 lift = Vector3.up * 0.3f;
            Debug.DrawLine(from + lift, points[index] + lift, color);
            for (int i = index + 1; i < points.Count; i++) Debug.DrawLine(points[i - 1] + lift, points[i] + lift, color);
        }
    }
}
