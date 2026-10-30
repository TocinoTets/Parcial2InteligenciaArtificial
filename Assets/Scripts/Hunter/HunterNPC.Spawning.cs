using UnityEngine;

namespace IA.Agents
{
    //el cazador genera objetos de interes en Search, sin pasarse del maximo
    public partial class HunterNPC
    {
        public int ActiveInterestObjectCount() //cuantos objetos hay activos (lo lleva el propio InterestObject, sin buscar en toda la escena)
        {
            return InterestObject.ActiveCount;
        }

        public bool SpawnInterestObject()
        {
            if (interestObjectPrefab == null) return false; //sin prefab no genera nada

            Vector3 position = AgentMath.RandomPointInSquare(spawnAreaHalfExtent, InterestObjectY);
            for (int attempt = 0; attempt < 10 && AgentMath.FlatDistance(position, transform.position) < 6f; attempt++) //intenta que no aparezca pegado al cazador
                position = AgentMath.RandomPointInSquare(spawnAreaHalfExtent, InterestObjectY);

            Instantiate(interestObjectPrefab, position, Quaternion.identity);
            return true;
        }
    }
}