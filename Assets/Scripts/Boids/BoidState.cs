namespace IA.Agents
{
    /// <summary>
    /// Estados locales de un agente Boid.
    /// La consigna los declara OPCIONALES: se usan sólo para organizar el comportamiento
    /// del propio agente (no hay coordinación entre agentes ni controlador global).
    /// </summary>
    public enum BoidState
    {
        /// <summary>Separation + Alignment + Cohesion.</summary>
        Flocking,

        /// <summary>Evade al NPC Cazador detectado dentro del rango de visión (prioridad sobre flocking).</summary>
        Evading,

        /// <summary>Arrive hacia el objeto de interés: se acerca sin superponerse con él.</summary>
        Arriving,

        /// <summary>Interactúa con el objeto de interés reduciéndole la vida.</summary>
        Interacting,

        /// <summary>Vida 0: queda inactivo en el lugar donde fue eliminado, sin desplazarse.</summary>
        Dead,

        /// <summary>Recolectado por el Cazador: desaparece y reaparece luego en una posición aleatoria.</summary>
        Respawning
    }
}
