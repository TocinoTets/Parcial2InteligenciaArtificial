#if UNITY_EDITOR
using System.Collections.Generic;
using IA.Agents;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IA.EditorTools
{
    /// <summary>
    /// Genera (o reconstruye) la escena completa del sistema de agentes:
    /// arena, waypoints, NPC Cazador (FSM) y los agentes Boid, junto con sus prefabs.
    /// Reejecutable desde el menú Tools > IA > Rebuild Flocking Scene.
    /// </summary>
    public static class FlockingSceneBuilder
    {
        private const string RootName = "FlockingSimulation";
        private const string PrefabsFolder = "Assets/Prefabs";
        private const string MaterialsFolder = "Assets/Materials";

        private const float ArenaHalfExtent = 25f; // la barrera coincide con el borde visible de la arena (50x50)
        private const int BoidCount = 8;
        private const float BoidY = 0.8f;
        private const float HunterY = 1.1f;

        [MenuItem("Tools/IA/Rebuild Flocking Scene")]
        public static void Execute()
        {
            Material boidMaterial = LoadMaterial("Boid");
            Material evadingMaterial = LoadMaterial("BoidEvading");
            Material interactingMaterial = LoadMaterial("BoidInteracting");
            Material deadMaterial = LoadMaterial("BoidDead");
            Material hunterMaterial = LoadMaterial("Hunter");
            Material interestMaterial = LoadMaterial("InterestObject");
            Material projectileMaterial = LoadMaterial("Projectile");
            Material groundMaterial = LoadMaterial("Ground");
            Material waypointMaterial = LoadMaterial("Waypoint");

            EnsureFolder(PrefabsFolder);

            // La escena se reconstruye desde cero.
            GameObject previousRoot = GameObject.Find(RootName);
            if (previousRoot != null) Object.DestroyImmediate(previousRoot);

            GameObject boidPrefab = BuildBoidPrefab(boidMaterial, evadingMaterial, interactingMaterial, deadMaterial);
            GameObject interestPrefab = BuildInterestPrefab(interestMaterial);
            GameObject projectilePrefab = BuildProjectilePrefab(projectileMaterial);

            GameObject root = new GameObject(RootName);
            BuildArena(root.transform, groundMaterial);
            Transform[] waypoints = BuildWaypoints(root.transform, waypointMaterial);
            BuildHunter(root.transform, hunterMaterial, waypoints, interestPrefab, projectilePrefab);
            BuildBoids(root.transform, boidPrefab, boidMaterial, evadingMaterial, interactingMaterial, deadMaterial);
            FrameCamera();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            Debug.Log($"[FlockingSceneBuilder] Escena reconstruida → {BoidCount} boids, 1 cazador (FSM Patrol/Attack/Gather), " +
                      $"{waypoints.Length} waypoints, arena {ArenaHalfExtent * 2f}x{ArenaHalfExtent * 2f}.");
        }

        // ---------------- prefabs ----------------
        private static GameObject BuildBoidPrefab(Material boid, Material evading, Material interacting, Material dead)
        {
            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            temp.name = "Boid";
            temp.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

            MeshRenderer meshRenderer = temp.GetComponent<MeshRenderer>();
            if (meshRenderer != null && boid != null) meshRenderer.sharedMaterial = boid;

            BoidAgent agent = temp.AddComponent<BoidAgent>();
            agent.Configure(0, ArenaHalfExtent, boid, evading, interacting, dead);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, $"{PrefabsFolder}/Boid.prefab");
            Object.DestroyImmediate(temp);
            return prefab;
        }

        private static GameObject BuildInterestPrefab(Material interestMaterial)
        {
            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            temp.name = "InterestObject";
            temp.transform.localScale = Vector3.one * 1.6f;

            MeshRenderer meshRenderer = temp.GetComponent<MeshRenderer>();
            if (meshRenderer != null && interestMaterial != null) meshRenderer.sharedMaterial = interestMaterial;

            temp.AddComponent<InterestObject>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, $"{PrefabsFolder}/InterestObject.prefab");
            Object.DestroyImmediate(temp);
            return prefab;
        }

        private static GameObject BuildProjectilePrefab(Material projectileMaterial)
        {
            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            temp.name = "HunterProjectile";
            temp.transform.localScale = Vector3.one * 0.5f;

            MeshRenderer meshRenderer = temp.GetComponent<MeshRenderer>();
            if (meshRenderer != null && projectileMaterial != null) meshRenderer.sharedMaterial = projectileMaterial;

            // El impacto se resuelve por distancia: el proyectil no necesita collider.
            Collider collider = temp.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);

            temp.AddComponent<HunterProjectile>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, $"{PrefabsFolder}/HunterProjectile.prefab");
            Object.DestroyImmediate(temp);
            return prefab;
        }

        // ---------------- escena ----------------
        private static void BuildArena(Transform parent, Material groundMaterial)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Arena";
            ground.transform.SetParent(parent, false);
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(5f, 1f, 5f); // plano de 10x10 unidades → 50x50

            MeshRenderer meshRenderer = ground.GetComponent<MeshRenderer>();
            if (meshRenderer != null && groundMaterial != null) meshRenderer.sharedMaterial = groundMaterial;
        }

        private static Transform[] BuildWaypoints(Transform parent, Material waypointMaterial)
        {
            GameObject waypointsRoot = new GameObject("Waypoints");
            waypointsRoot.transform.SetParent(parent, false);

            Vector3[] positions =
            {
                new Vector3(-15f, 0f, -15f),
                new Vector3(0f, 0f, -17f),
                new Vector3(15f, 0f, -15f),
                new Vector3(17f, 0f, 0f),
                new Vector3(15f, 0f, 15f),
                new Vector3(0f, 0f, 17f),
                new Vector3(-15f, 0f, 15f),
                new Vector3(-17f, 0f, 0f)
            };

            List<Transform> points = new List<Transform>(positions.Length);

            for (int i = 0; i < positions.Length; i++)
            {
                GameObject waypoint = new GameObject($"WP_{i + 1}");
                waypoint.transform.SetParent(waypointsRoot.transform, false);
                waypoint.transform.position = positions[i] + Vector3.up * 0.5f;

                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                marker.name = "Marker";
                marker.transform.SetParent(waypoint.transform, false);
                marker.transform.localPosition = Vector3.zero;
                marker.transform.localScale = new Vector3(0.9f, 0.05f, 0.9f);

                Collider markerCollider = marker.GetComponent<Collider>();
                if (markerCollider != null) Object.DestroyImmediate(markerCollider);

                MeshRenderer markerRenderer = marker.GetComponent<MeshRenderer>();
                if (markerRenderer != null && waypointMaterial != null) markerRenderer.sharedMaterial = waypointMaterial;

                points.Add(waypoint.transform);
            }

            return points.ToArray();
        }

        private static void BuildHunter(Transform parent, Material hunterMaterial, Transform[] waypoints, GameObject interestPrefab, GameObject projectilePrefab)
        {
            GameObject hunter = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            hunter.name = "Cazador";
            hunter.transform.SetParent(parent, false);
            hunter.transform.position = new Vector3(0f, HunterY, 0f);
            hunter.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);

            MeshRenderer meshRenderer = hunter.GetComponent<MeshRenderer>();
            if (meshRenderer != null && hunterMaterial != null) meshRenderer.sharedMaterial = hunterMaterial;

            HunterNPC npc = hunter.AddComponent<HunterNPC>();
            npc.Configure(waypoints, interestPrefab, projectilePrefab, ArenaHalfExtent);
        }

        private static void BuildBoids(Transform parent, GameObject boidPrefab, Material boid, Material evading, Material interacting, Material dead)
        {
            GameObject boidsRoot = new GameObject("Boids");
            boidsRoot.transform.SetParent(parent, false);

            for (int i = 0; i < BoidCount; i++)
            {
                float angle = (i / (float)BoidCount) * Mathf.PI * 2f;
                Vector3 position = new Vector3(Mathf.Cos(angle) * 7f, BoidY, Mathf.Sin(angle) * 7f);

                GameObject boidInstance = (GameObject)PrefabUtility.InstantiatePrefab(boidPrefab, boidsRoot.transform);
                boidInstance.name = $"Boid_{i + 1}";
                boidInstance.transform.position = position;
                boidInstance.transform.rotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);

                BoidAgent agent = boidInstance.GetComponent<BoidAgent>();
                if (agent != null) agent.Configure(i + 1, ArenaHalfExtent, boid, evading, interacting, dead);
            }
        }

        private static void FrameCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = GameObject.Find("Main Camera");
                if (cameraObject != null) camera = cameraObject.GetComponent<Camera>();
            }
            if (camera == null) return;

            camera.transform.position = new Vector3(0f, 32f, -28f);
            camera.transform.rotation = Quaternion.Euler(48f, 0f, 0f);
        }

        // ---------------- utilidades ----------------
        private static Material LoadMaterial(string materialName)
        {
            string path = $"{MaterialsFolder}/{materialName}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) Debug.LogWarning($"[FlockingSceneBuilder] No se encontró el material {path}");
            return material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            AssetDatabase.CreateFolder("Assets", System.IO.Path.GetFileName(path));
        }
    }
}
#endif
