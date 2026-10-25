using System.Collections.Generic;
using UnityEngine;

namespace IA.Agents
{
    /// <summary>
    /// Pathfinding A* sobre el grafo de navegación. Lo usan tanto el Cazador como los lobos.
    /// Los lobos lo invocan con allowSafe = false: los nodos de Zonas seguras se excluyen.
    /// </summary>
    public static class AStar
    {
        public static List<NavNode> FindPath(NavNode start, NavNode goal, bool allowSafe)
        {
            List<NavNode> path = new List<NavNode>();
            if (start == null || goal == null) return path;
            if (!allowSafe && (start.Zone == Zone.Safe || goal.Zone == Zone.Safe)) return path;

            // shortcut: open set como lista lineal, alcanza para grafos de pocos cientos de nodos.
            List<NavNode> open = new List<NavNode> { start };
            HashSet<NavNode> closed = new HashSet<NavNode>();
            Dictionary<NavNode, NavNode> cameFrom = new Dictionary<NavNode, NavNode>();
            Dictionary<NavNode, float> g = new Dictionary<NavNode, float> { [start] = 0f };
            Dictionary<NavNode, float> f = new Dictionary<NavNode, float> { [start] = Heuristic(start, goal) };

            while (open.Count > 0)
            {
                int best = 0;
                for (int i = 1; i < open.Count; i++)
                    if (f[open[i]] < f[open[best]]) best = i;

                NavNode current = open[best];
                if (current == goal)
                {
                    path.Add(current);
                    while (cameFrom.TryGetValue(current, out current)) path.Add(current);
                    path.Reverse();
                    return path;
                }

                open.RemoveAt(best);
                closed.Add(current);

                foreach (NavNode neighbour in current.Neighbours)
                {
                    if (closed.Contains(neighbour)) continue;
                    if (!allowSafe && neighbour.Zone == Zone.Safe) continue;

                    float tentative = g[current] + Vector3.Distance(current.Position, neighbour.Position);
                    if (g.TryGetValue(neighbour, out float known) && tentative >= known) continue;

                    cameFrom[neighbour] = current;
                    g[neighbour] = tentative;
                    f[neighbour] = tentative + Heuristic(neighbour, goal);
                    if (!open.Contains(neighbour)) open.Add(neighbour);
                }
            }

            return path; // sin camino
        }

        /// <summary>Longitud del camino; infinito si no hay camino.</summary>
        public static float PathLength(List<NavNode> path)
        {
            if (path == null || path.Count == 0) return float.PositiveInfinity;

            float length = 0f;
            for (int i = 1; i < path.Count; i++) length += Vector3.Distance(path[i - 1].Position, path[i].Position);
            return length;
        }

        private static float Heuristic(NavNode a, NavNode b)
        {
            return Vector3.Distance(a.Position, b.Position);
        }
    }
}
