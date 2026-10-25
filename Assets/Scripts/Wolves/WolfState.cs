namespace IA.Agents
{
    /// <summary>Estados de la FSM de un lobo.</summary>
    public enum WolfState
    {
        /// <summary>Recorre sus waypoints (A* si el siguiente no está en línea de visión).</summary>
        Patrol,
        /// <summary>Ve al Cazador y lo persigue directamente.</summary>
        Chase,
        /// <summary>Va por A* a la última posición conocida (propia o recibida por aullido).</summary>
        GoToLastKnown,
        /// <summary>Busca alrededor de la última posición conocida.</summary>
        Search
    }
}
