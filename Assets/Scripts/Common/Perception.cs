using UnityEngine;

namespace IA.Agents
{

    public static class Perception
    {
        public const float EyeHeight = 1f; //altura fija de los ojos, si el piso no esta en 0 falla

        public static readonly int AgentsMask = GetAgentsMask(); //solo boids, cazador, lobos y objetos de interes
        public static readonly int ObstacleMask = ~LayerMask.GetMask("Agents"); //todo menos los agentes, asi el raycast solo choca con paredes

        private static int GetAgentsMask()
        {
            int mask = LayerMask.GetMask("Agents");
            return mask != 0 ? mask : Physics.AllLayers; //si no existe la capa busca en todas
        }

        public static bool HasLineOfSight(Vector3 from, Vector3 to)
        {
            from.y = EyeHeight;
            to.y = EyeHeight;
            return !Physics.Linecast(from, to, ObstacleMask, QueryTriggerInteraction.Ignore);
        }

        public static bool CanSee(Vector3 eye, Vector3 forward, Vector3 target, float range, float angleDeg)
        {
            if (AgentMath.FlatDistance(eye, target) > range) return false; //muy lejos, no lo ve

            if (angleDeg < 360f)
            {
                Vector3 direction = AgentMath.FlatDirection(eye, target);
                if (direction != Vector3.zero && Vector3.Angle(AgentMath.Flat(forward), direction) > angleDeg * 0.5f) return false; //fuera del cono de vision
            }

            return HasLineOfSight(eye, target); //hay algo en el medio, no lo ve
        }
    }
}