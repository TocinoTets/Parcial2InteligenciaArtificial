using UnityEngine;

namespace IA.Agents
{
    /// <summary>
    /// Percepción compartida por lobos y Cazador: distancia máxima, ángulo de visión
    /// (opcional, 360 = sin ángulo) y línea de visión bloqueada por paredes.
    /// Los agentes usan colliders trigger, así que el linecast (que ignora triggers) sólo
    /// choca contra las paredes del escenario.
    /// </summary>
    public static class Perception
    {
        public const float EyeHeight = 1f;

        public static bool HasLineOfSight(Vector3 from, Vector3 to)
        {
            from.y = EyeHeight;
            to.y = EyeHeight;
            return !Physics.Linecast(from, to, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }

        public static bool CanSee(Vector3 eye, Vector3 forward, Vector3 target, float range, float angleDeg)
        {
            if (AgentMath.FlatDistance(eye, target) > range) return false;

            if (angleDeg < 360f)
            {
                Vector3 direction = AgentMath.FlatDirection(eye, target);
                if (direction != Vector3.zero && Vector3.Angle(AgentMath.Flat(forward), direction) > angleDeg * 0.5f) return false;
            }

            return HasLineOfSight(eye, target);
        }
    }
}
