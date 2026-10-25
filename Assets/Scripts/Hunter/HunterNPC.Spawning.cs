using UnityEngine;

namespace IA.Agents
{
    /// <summary>
    /// Objetos de interés que el Cazador genera durante Search (dentro del Coto), respetando el cupo de
    /// maxActiveInterestObjects.
    /// </summary>
    public partial class HunterNPC
    {
        // ---------------- objetos de interés ----------------
        public int ActiveInterestObjectCount()
        {
            InterestObject[] found = Object.FindObjectsByType<InterestObject>(FindObjectsSortMode.None);
            int active = 0;
            for (int i = 0; i < found.Length; i++)
                if (found[i] != null && found[i].IsAvailable) active++;
            return active;
        }

        public bool SpawnInterestObject()
        {
            if (interestObjectPrefab == null)
            {
                LogAction("Search: sin prefab de objeto de interés");
                return false;
            }

            Vector3 position = AgentMath.RandomPointInSquare(spawnAreaHalfExtent, InterestObjectY);
            for (int attempt = 0; attempt < 10 && AgentMath.FlatDistance(position, transform.position) < 6f; attempt++)
                position = AgentMath.RandomPointInSquare(spawnAreaHalfExtent, InterestObjectY);

            Instantiate(interestObjectPrefab, position, Quaternion.identity);

            LogAction($"Search: genera objeto de interés en ({position.x:0.0}, {position.z:0.0}) [{ActiveInterestObjectCount()}/{maxActiveInterestObjects}]");
            return true;
        }
    }
}
