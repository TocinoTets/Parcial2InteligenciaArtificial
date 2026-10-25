using System.Collections.Generic;
using UnityEngine;

namespace IA.Agents
{
    /// <summary>
    /// Grafo de navegación generado a partir del mapa en texto del escenario.
    /// El mismo mapa lo usa el generador de escena para colocar paredes y pisos, así que
    /// grafo y nivel siempre coinciden.
    ///
    /// Leyenda: '#' pared, '.' pasillo, 'S' Zona inicial, 'C' Coto de caza,
    ///          '1'..'9' Zonas seguras (id), 'E' Salida.
    ///
    /// Conexiones: cada celda transitable se une con sus 8 vecinas transitables; una
    /// diagonal sólo existe si las dos celdas ortogonales también son transitables, de modo
    /// que ninguna conexión atraviesa paredes ni corta esquinas.
    /// </summary>
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
        [SerializeField] private bool drawGraph = true;

        private readonly List<NavNode> nodes = new List<NavNode>(256);
        private readonly List<NavNode> safeZoneTargets = new List<NavNode>(4);
        private NavNode[,] grid;
        private int rowCount;
        private int colCount;

        public static NavGraph Instance { get; private set; }

        public IReadOnlyList<NavNode> Nodes => nodes;
        /// <summary>Un nodo central por cada Zona segura.</summary>
        public IReadOnlyList<NavNode> SafeZoneTargets => safeZoneTargets;
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

        // ---------------- construcción ----------------
        public void Build(string[] newRows, float newCellSize)
        {
            rows = newRows;
            cellSize = newCellSize;
            rowCount = rows.Length;
            colCount = rowCount > 0 ? rows[0].Length : 0;
            grid = new NavNode[rowCount, colCount];
            nodes.Clear();
            safeZoneTargets.Clear();

            for (int r = 0; r < rowCount; r++)
            {
                for (int c = 0; c < colCount; c++)
                {
                    char cell = CharAt(r, c);
                    Zone zone = ZoneOf(cell);
                    if (zone == Zone.Wall) continue;

                    NavNode node = new NavNode(r, c, CellCenter(r, c), zone, zone == Zone.Safe ? cell - '0' : 0);
                    grid[r, c] = node;
                    nodes.Add(node);
                }
            }

            foreach (NavNode node in nodes)
            {
                for (int dr = -1; dr <= 1; dr++)
                {
                    for (int dc = -1; dc <= 1; dc++)
                    {
                        if (dr == 0 && dc == 0) continue;
                        NavNode other = NodeAt(node.Row + dr, node.Col + dc);
                        if (other == null) continue;
                        // diagonal: sólo si no corta la esquina de una pared
                        if (dr != 0 && dc != 0 && (NodeAt(node.Row + dr, node.Col) == null || NodeAt(node.Row, node.Col + dc) == null)) continue;
                        node.Neighbours.Add(other);
                    }
                }
            }

            StartNode = CenterNode(n => n.Zone == Zone.Start);
            ExitNode = CenterNode(n => n.Zone == Zone.Exit);
            for (int id = 1; id <= 9; id++)
            {
                int safeId = id;
                NavNode target = CenterNode(n => n.SafeZoneId == safeId);
                if (target != null) safeZoneTargets.Add(target);
            }
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

        public NavNode NodeAt(int row, int col)
        {
            if (grid == null || row < 0 || row >= rowCount || col < 0 || col >= colCount) return null;
            return grid[row, col];
        }

        public Zone ZoneAt(Vector3 position)
        {
            WorldToCell(position, out int row, out int col);
            return ZoneOf(CharAt(row, col));
        }

        public NavNode ClosestNode(Vector3 position, bool allowSafe)
        {
            WorldToCell(position, out int row, out int col);
            NavNode cellNode = NodeAt(row, col);
            if (cellNode != null && (allowSafe || cellNode.Zone != Zone.Safe)) return cellNode;

            NavNode best = null;
            float bestDistance = float.MaxValue;
            foreach (NavNode node in nodes)
            {
                if (!allowSafe && node.Zone == Zone.Safe) continue;
                float distance = AgentMath.FlatSqrDistance(position, node.Position);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = node;
            }
            return best;
        }

        public NavNode RandomNodeInZone(Zone zone)
        {
            List<NavNode> candidates = nodes.FindAll(n => n.Zone == zone);
            return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;
        }

        public NavNode RandomNodeNear(Vector3 position, float radius, bool allowSafe)
        {
            List<NavNode> candidates = nodes.FindAll(n => (allowSafe || n.Zone != Zone.Safe) && AgentMath.FlatDistance(n.Position, position) <= radius);
            return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;
        }

        private void WorldToCell(Vector3 position, out int row, out int col)
        {
            col = Mathf.RoundToInt(position.x / cellSize + (colCount - 1) * 0.5f);
            row = Mathf.RoundToInt((rowCount - 1) * 0.5f - position.z / cellSize);
        }

        private NavNode CenterNode(System.Predicate<NavNode> match)
        {
            List<NavNode> group = nodes.FindAll(match);
            if (group.Count == 0) return null;

            Vector3 centroid = Vector3.zero;
            foreach (NavNode node in group) centroid += node.Position;
            centroid /= group.Count;

            NavNode best = group[0];
            foreach (NavNode node in group)
                if (AgentMath.FlatSqrDistance(node.Position, centroid) < AgentMath.FlatSqrDistance(best.Position, centroid)) best = node;
            return best;
        }

        // ---------------- gizmos ----------------
        private void OnDrawGizmos()
        {
            if (!drawGraph) return;
            if (grid == null || (!Application.isPlaying && nodes.Count == 0)) Build(rows, cellSize);

            foreach (NavNode node in nodes)
            {
                Gizmos.color = ZoneColor(node.Zone);
                Gizmos.DrawSphere(node.Position + Vector3.up * 0.1f, 0.25f);

                Gizmos.color = new Color(1f, 1f, 1f, 0.15f);
                foreach (NavNode other in node.Neighbours)
                    Gizmos.DrawLine(node.Position + Vector3.up * 0.1f, other.Position + Vector3.up * 0.1f);
            }
        }
    }
}
