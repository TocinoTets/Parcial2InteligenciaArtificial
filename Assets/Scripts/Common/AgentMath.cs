using UnityEngine;

namespace IA.Agents
{
    /// <summary>Utilidades de steering compartidas por los agentes (trabajan en el plano XZ).</summary>
    public static class AgentMath
    {
        public static float FlatDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        public static float FlatSqrDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return dx * dx + dz * dz;
        }

        public static Vector3 FlatDirection(Vector3 from, Vector3 to)
        {
            Vector3 direction = new Vector3(to.x - from.x, 0f, to.z - from.z);
            return direction.sqrMagnitude < 1e-6f ? Vector3.zero : direction.normalized;
        }

        public static Vector3 Flat(Vector3 v)
        {
            return new Vector3(v.x, 0f, v.z);
        }

        public static Vector3 RandomPointInSquare(float halfExtent, float y)
        {
            return new Vector3(Random.Range(-halfExtent, halfExtent), y, Random.Range(-halfExtent, halfExtent));
        }
    }
}
