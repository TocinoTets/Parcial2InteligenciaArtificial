using System.Collections.Generic;
using UnityEngine;

namespace IA.Agents
{
    /// <summary>Tipo de celda / zona del escenario.</summary>
    public enum Zone
    {
        Wall,
        Corridor,
        Start,
        Coto,
        Safe,
        Exit
    }

    /// <summary>Nodo del grafo de navegación: una celda transitable del mapa.</summary>
    public class NavNode
    {
        public readonly int Row;
        public readonly int Col;
        public readonly Vector3 Position;
        public readonly Zone Zone;
        /// <summary>Identificador de la Zona segura (0 si el nodo no pertenece a una).</summary>
        public readonly int SafeZoneId;
        public readonly List<NavNode> Neighbours = new List<NavNode>(8);

        public NavNode(int row, int col, Vector3 position, Zone zone, int safeZoneId)
        {
            Row = row;
            Col = col;
            Position = position;
            Zone = zone;
            SafeZoneId = safeZoneId;
        }

        public override string ToString() => $"({Row},{Col}) {Zone}";
    }
}
