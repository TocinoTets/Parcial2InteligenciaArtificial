using System.Collections.Generic;
using UnityEngine;

namespace IA.Agents
{
    //calcula el camino con A* y lo recorre punto por punto
    public class PathFollower
    {
        private readonly List<Vector3> points = new List<Vector3>(32);
        private int index;

        public float Tolerance = 0.8f; //que tan cerca tiene que llegar para pasar al siguiente punto

        public IReadOnlyList<Vector3> Points => points;
        public Vector3 Destination { get; private set; }
        public bool IsActive => index < points.Count; //todavia le queda camino

        public bool SetPath(Vector3 from, Vector3 to, bool allowSafe)
        {
            Clear();
            Destination = to;

            NavGraph graph = NavGraph.Instance;
            if (graph == null) return false;

            List<NavNode> nodes = AStar.FindPath(graph.ClosestNode(from, allowSafe), graph.ClosestNode(to, allowSafe), allowSafe);
            if (nodes.Count == 0) return false; //no hay camino

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
            if (index < points.Count - 1 && Perception.HasLineOfSight(position, points[index + 1])) index++; //si ya ve el siguiente punto se saltea el actual (un raycast por frame)
            return index < points.Count ? points[index] : position;
        }

        public void DrawDebug(Vector3 from, Color color) //dibuja el camino en la escena
        {
            if (!IsActive) return;

            Vector3 lift = Vector3.up * 0.3f;
            Debug.DrawLine(from + lift, points[index] + lift, color);
            for (int i = index + 1; i < points.Count; i++) Debug.DrawLine(points[i - 1] + lift, points[i] + lift, color);
        }
    }
}