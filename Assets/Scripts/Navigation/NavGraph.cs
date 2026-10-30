using System.Collections.Generic;
using UnityEngine;

namespace IA.Agents
{
    //grafo de navegacion: los nodos van solo en puntos clave (cruces, esquinas, puertas y centro de cada zona)
    //las conexiones se hacen con raycast: si no hay pared en el medio, los dos nodos se conectan
    //'#' pared, '.' pasillo, 'S' inicio, 'C' coto, '1'..'9' zonas seguras, 'E' salida
    public class NavGraph : MonoBehaviour
    {
        public static readonly string[] DefaultRows =
        {
            "###############",
            "#SSS#11#....EE#",
            "#SSS#11#.##.EE#",
            "#SS.....#.....#",
            "###.#####.###.#",
            "#...#CCCCC#...#",
            "#.#..CCCCC..#.#",
            "#.#.#CCCCC#.#.#",
            "#.#..CCCCC..#.#",
            "#...#CCCCC#...#",
            "#.###.###.###.#",
            "#.............#",
            "#.##2##.###.#.#",
            "#..#22#.#...#.#",
            "###############"
        };

        [SerializeField] private string[] rows = (string[])DefaultRows.Clone();
        [SerializeField] private float cellSize = 8f;
        [SerializeField] private float linkHeight = 1f; //altura del raycast de las conexiones
        [SerializeField] private float linkRadius = 0.6f; //grosor del raycast, asi las conexiones no pasan pegadas a las esquinas
        [SerializeField] private LayerMask obstacleMask = 1; //capas que cortan una conexion (las paredes estan en Default)
        [SerializeField] private bool drawGraph = true;

        private readonly List<NavNode> nodes = new List<NavNode>(64);
        private readonly List<NavNode> safeZoneTargets = new List<NavNode>(4);
        private readonly List<NavNode> buffer = new List<NavNode>(64); //lista reutilizable, asi no crea listas nuevas en cada busqueda
        private int rowCount;
        private int colCount;

        public static NavGraph Instance { get; private set; }

        public IReadOnlyList<NavNode> Nodes => nodes;
        public IReadOnlyList<NavNode> SafeZoneTargets => safeZoneTargets; //un nodo central por cada zona segura
        public NavNode StartNode { get; private set; }
        public NavNode ExitNode { get; private set; }
        public float CellSize => cellSize;
        public float MapHalfExtent => Mathf.Max(rowCount, colCount) * cellSize * 0.5f;
        public string[] Rows => rows;

        private void Awake()
        {
            Instance = this;
            Build(rows, cellSize);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnValidate()
        {
            if (!Application.isPlaying) nodes.Clear(); //si cambias algo en el inspector se vuelve a armar
        }

        // ---------------- construcción ----------------
        public void Build(string[] newRows, float newCellSize)
        {
            rows = newRows;
            cellSize = newCellSize;
            rowCount = rows.Length;
            colCount = rowCount > 0 ? rows[0].Length : 0;
            nodes.Clear();
            safeZoneTargets.Clear();

            Dictionary<int, NavNode> byCell = new Dictionary<int, NavNode>(); //solo para no repetir nodos mientras se arma

            for (int r = 0; r < rowCount; r++)
                for (int c = 0; c < colCount; c++)
                    if (IsKeyCell(r, c)) AddNode(r, c, byCell);

            StartNode = AddZoneCenter(ch => ch == 'S', byCell);
            AddZoneCenter(ch => ch == 'C', byCell);
            ExitNode = AddZoneCenter(ch => ch == 'E', byCell);
            for (char id = '1'; id <= '9'; id++)
            {
                char safeId = id;
                NavNode target = AddZoneCenter(ch => ch == safeId, byCell);
                if (target != null) safeZoneTargets.Add(target);
            }

            for (int i = 0; i < nodes.Count; i++) //conecta cada par de nodos que se ven sin pared en el medio
            {
                for (int j = i + 1; j < nodes.Count; j++)
                {
                    if (!CanLink(nodes[i], nodes[j])) continue;
                    nodes[i].Neighbours.Add(nodes[j]);
                    nodes[j].Neighbours.Add(nodes[i]);
                }
            }
        }

        private bool IsKeyCell(int r, int c)
        {
            char cell = CharAt(r, c);
            Zone zone = ZoneOf(cell);
            if (zone == Zone.Wall) return false;

            bool n = IsWalkable(r - 1, c), s = IsWalkable(r + 1, c), e = IsWalkable(r, c + 1), w = IsWalkable(r, c - 1);
            int count = (n ? 1 : 0) + (s ? 1 : 0) + (e ? 1 : 0) + (w ? 1 : 0);

            if (zone == Zone.Corridor) return !(count == 2 && ((n && s) || (e && w))); //en los pasillos todo menos los tramos rectos

            if (TouchesOtherZone(r, c, cell)) return true; //puerta de la zona

            bool sn = CharAt(r - 1, c) == cell, ss = CharAt(r + 1, c) == cell, se = CharAt(r, c + 1) == cell, sw = CharAt(r, c - 1) == cell;
            int same = (sn ? 1 : 0) + (ss ? 1 : 0) + (se ? 1 : 0) + (sw ? 1 : 0);
            return same == 2 && !((sn && ss) || (se && sw)); //esquina de la zona
        }

        private bool TouchesOtherZone(int r, int c, char cell)
        {
            return IsOtherWalkable(r - 1, c, cell) || IsOtherWalkable(r + 1, c, cell) || IsOtherWalkable(r, c + 1, cell) || IsOtherWalkable(r, c - 1, cell);
        }

        private bool IsOtherWalkable(int r, int c, char cell) => IsWalkable(r, c) && CharAt(r, c) != cell;

        private bool IsWalkable(int r, int c) => ZoneOf(CharAt(r, c)) != Zone.Wall;

        private NavNode AddNode(int r, int c, Dictionary<int, NavNode> byCell)
        {
            int key = r * colCount + c;
            if (byCell.TryGetValue(key, out NavNode existing)) return existing;

            char cell = CharAt(r, c);
            Zone zone = ZoneOf(cell);
            NavNode node = new NavNode(CellCenter(r, c), zone, zone == Zone.Safe ? cell - '0' : 0);
            byCell[key] = node;
            nodes.Add(node);
            return node;
        }

        private NavNode AddZoneCenter(System.Func<char, bool> match, Dictionary<int, NavNode> byCell) //agrega un nodo en el centro de la zona
        {
            Vector3 sum = Vector3.zero;
            int count = 0;
            for (int r = 0; r < rowCount; r++)
                for (int c = 0; c < colCount; c++)
                    if (match(CharAt(r, c))) { sum += CellCenter(r, c); count++; }

            if (count == 0) return null;

            Vector3 centroid = sum / count;
            int bestR = 0, bestC = 0;
            float bestDistance = float.MaxValue;
            for (int r = 0; r < rowCount; r++)
            {
                for (int c = 0; c < colCount; c++)
                {
                    if (!match(CharAt(r, c))) continue;
                    float distance = AgentMath.FlatSqrDistance(CellCenter(r, c), centroid);
                    if (distance >= bestDistance) continue;
                    bestDistance = distance;
                    bestR = r;
                    bestC = c;
                }
            }
            return AddNode(bestR, bestC, byCell);
        }

        private bool CanLink(NavNode a, NavNode b)
        {
            Vector3 from = a.Position + Vector3.up * linkHeight;
            Vector3 to = b.Position + Vector3.up * linkHeight;
            Vector3 direction = to - from;
            float distance = direction.magnitude;
            if (distance < 1e-3f) return false;
            if (CrossesSafeZone(a, b)) return false; //asi los lobos nunca atraviesan una zona segura

            return !Physics.SphereCast(from, linkRadius, direction / distance, out _, distance, obstacleMask, QueryTriggerInteraction.Ignore);
        }

        private bool CrossesSafeZone(NavNode a, NavNode b)
        {
            if (a.Zone == Zone.Safe || b.Zone == Zone.Safe) return false; //si uno de los dos es de la zona segura, la conexion puede entrar

            float distance = AgentMath.FlatDistance(a.Position, b.Position);
            int steps = Mathf.CeilToInt(distance / (cellSize * 0.25f));
            for (int i = 1; i < steps; i++)
                if (ZoneAt(Vector3.Lerp(a.Position, b.Position, i / (float)steps)) == Zone.Safe) return true;
            return false;
        }

        public static Zone ZoneOf(char cell)
        {
            switch (cell)
            {
                case '#': return Zone.Wall;
                case 'S': return Zone.Start;
                case 'C': return Zone.Coto;
                case 'E': return Zone.Exit;
                default: return cell >= '1' && cell <= '9' ? Zone.Safe : Zone.Corridor;
            }
        }

        public static Color ZoneColor(Zone zone)
        {
            switch (zone)
            {
                case Zone.Start: return new Color(0.35f, 0.55f, 1f);
                case Zone.Coto: return new Color(0.35f, 0.75f, 0.3f);
                case Zone.Safe: return new Color(0.2f, 0.9f, 0.9f);
                case Zone.Exit: return new Color(1f, 0.85f, 0.2f);
                case Zone.Wall: return new Color(0.3f, 0.3f, 0.33f);
                default: return new Color(0.55f, 0.5f, 0.45f);
            }
        }

        // ---------------- consultas ----------------
        public Vector3 CellCenter(int row, int col)
        {
            return new Vector3((col - (colCount - 1) * 0.5f) * cellSize, 0f, ((rowCount - 1) * 0.5f - row) * cellSize);
        }

        public char CharAt(int row, int col)
        {
            if (row < 0 || row >= rowCount || col < 0 || col >= rows[row].Length) return '#';
            return rows[row][col];
        }

        public Zone ZoneAt(Vector3 position) //en que zona esta una posicion
        {
            int col = Mathf.RoundToInt(position.x / cellSize + (colCount - 1) * 0.5f);
            int row = Mathf.RoundToInt((rowCount - 1) * 0.5f - position.z / cellSize);
            return ZoneOf(CharAt(row, col));
        }

        public NavNode ClosestNode(Vector3 position, bool allowSafe) //el nodo mas cercano que se ve sin pared en el medio
        {
            NavNode closest = null, closestVisible = null;
            float closestDistance = float.MaxValue, closestVisibleDistance = float.MaxValue;

            foreach (NavNode node in nodes)
            {
                if (!allowSafe && node.Zone == Zone.Safe) continue;

                float distance = AgentMath.FlatSqrDistance(position, node.Position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = node;
                }

                if (distance >= closestVisibleDistance) continue; //solo hace el raycast si este nodo esta mas cerca
                if (!Perception.HasLineOfSight(position, node.Position)) continue;
                closestVisibleDistance = distance;
                closestVisible = node;
            }

            return closestVisible != null ? closestVisible : closest;
        }

        public NavNode RandomNodeInZone(Zone zone)
        {
            buffer.Clear();
            foreach (NavNode node in nodes)
                if (node.Zone == zone) buffer.Add(node);
            return buffer.Count > 0 ? buffer[Random.Range(0, buffer.Count)] : null;
        }

        public NavNode RandomNodeNear(Vector3 position, float radius, bool allowSafe)
        {
            buffer.Clear();
            foreach (NavNode node in nodes)
                if ((allowSafe || node.Zone != Zone.Safe) && AgentMath.FlatDistance(node.Position, position) <= radius) buffer.Add(node);
            return buffer.Count > 0 ? buffer[Random.Range(0, buffer.Count)] : null;
        }

        private void OnDrawGizmos() //dibuja los nodos y las conexiones en la escena
        {
            if (!drawGraph) return;
            if (nodes.Count == 0) Build(rows, cellSize);

            foreach (NavNode node in nodes)
            {
                Gizmos.color = ZoneColor(node.Zone);
                Gizmos.DrawSphere(node.Position + Vector3.up * 0.2f, 0.5f);

                Gizmos.color = new Color(1f, 1f, 1f, 0.35f);
                foreach (NavNode other in node.Neighbours)
                    Gizmos.DrawLine(node.Position + Vector3.up * 0.2f, other.Position + Vector3.up * 0.2f);
            }
        }
    }
}