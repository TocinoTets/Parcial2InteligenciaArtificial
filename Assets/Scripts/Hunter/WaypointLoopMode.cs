using UnityEngine;

namespace IA.Agents
{
    /// <summary>Modo de recorrido de waypoints del patrullaje de los lobos.</summary>
    public enum WaypointLoopMode
    {
        /// <summary>Al llegar al último waypoint vuelve al primero.</summary>
        Loop,

        /// <summary>Al llegar a un extremo recorre los waypoints en sentido inverso.</summary>
        PingPong
    }
}
