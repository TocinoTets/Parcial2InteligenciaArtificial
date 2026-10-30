using System.Collections.Generic;
using UnityEngine;

namespace IA.Agents
{
    //A* sobre el grafo, lo usan el cazador y los lobos (los lobos no pueden pasar por zonas seguras)
    public static class AStar
    {
        public static List<NavNode> FindPath(NavNode start, NavNode goal, bool allowSafe)
        {
            List<NavNode> path = new List<NavNode>();
            if (start == null || goal == null) return path;
            if (!allowSafe && (start.Zone == Zone.Safe || goal.Zone == Zone.Safe)) return path;

            List<NavNode> open = new List<NavNode> { start }; //nodos por revisar
            Dictionary<NavNode, NavNode> cameFrom = new Dictionary<NavNode, NavNode>();
            Dictionary<NavNode, float> g = new Dictionary<NavNode, float> { [start] = 0f }; //costo desde el inicio
            Dictionary<NavNode, float> f = new Dictionary<NavNode, float> { [start] = Heuristic(start, goal) }; //costo + heuristica

            while (open.Count > 0)
            {
                int best = 0;
                for (int i = 1; i < open.Count; i++)
                    if (f[open[i]] < f[open[best]]) best = i; //agarra el de menor f

                NavNode current = open[best];
                if (current == goal) //llego, arma el camino para atras
                {
                    path.Add(current);
                    while (cameFrom.TryGetValue(current, out current)) path.Add(current);
                    path.Reverse();
                    return path;
                }

                open.RemoveAt(best);

                foreach (NavNode neighbour in current.Neighbours)
                {
                    if (!allowSafe && neighbour.Zone == Zone.Safe) continue;

                    float tentative = g[current] + Vector3.Distance(current.Position, neighbour.Position);
                    if (g.TryGetValue(neighbour, out float known) && tentative >= known) continue; //ya tenia un camino mejor

                    cameFrom[neighbour] = current;
                    g[neighbour] = tentative;
                    f[neighbour] = tentative + Heuristic(neighbour, goal);
                    if (!open.Contains(neighbour)) open.Add(neighbour);
                }
            }

            return path; //no hay camino, devuelve vacio
        }

        public static float PathLength(List<NavNode> path) //largo del camino, infinito si no hay
        {
            if (path == null || path.Count == 0) return float.PositiveInfinity;

            float length = 0f;
            for (int i = 1; i < path.Count; i++) length += Vector3.Distance(path[i - 1].Position, path[i].Position);
            return length;
        }

        private static float Heuristic(NavNode a, NavNode b) //distancia en linea recta
        {
            return Vector3.Distance(a.Position, b.Position);
        }
    }
}