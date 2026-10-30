using UnityEngine;

namespace IA.Agents
{
    public enum WaypointLoopMode //como recorren los waypoints los lobos
    {
        Loop, //del ultimo vuelve al primero

        PingPong //llega al final y vuelve para atras
    }
}