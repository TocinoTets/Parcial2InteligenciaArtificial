using System.Collections.Generic;
using UnityEngine;

namespace IA.Agents
{
    public enum Zone //tipos de zona del mapa
    {
        Wall,
        Corridor,
        Start,
        Coto,
        Safe,
        Exit
    }

    //nodo del grafo: un punto clave del mapa (cruce, esquina, puerta o centro de zona)
    public class NavNode
    {
        public readonly Vector3 Position;
        public readonly Zone Zone;
        public readonly int SafeZoneId; //0 si no es zona segura
        public readonly List<NavNode> Neighbours = new List<NavNode>(8);

        public NavNode(Vector3 position, Zone zone, int safeZoneId)
        {
            Position = position;
            Zone = zone;
            SafeZoneId = safeZoneId;
        }
    }
}