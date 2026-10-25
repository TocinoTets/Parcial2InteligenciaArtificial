#if UNITY_EDITOR
using IA.Agents;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IA.EditorTools
{
    /// <summary>
    /// Genera (o reconstruye) la escena completa del sistema de agentes:
    /// mapa (paredes y pisos por zona a partir del NavGraph), NPC Cazador (FSM), Boids dentro
    /// del Coto de caza y lobos con sus waypoints, junto con sus prefabs y materiales.
    /// Reejecutable desde el menú Tools > IA > Rebuild Flocking Scene.
    /// </summary>
    public static class FlockingSceneBuilder
    {
        private const string RootName = "FlockingSimulation";
        private const string PrefabsFolder = "Assets/Prefabs";
        private const string MaterialsFolder = "Assets/Materials";

        private const float CellSize = 8f; // x2: todas las zonas son el doble de grandes
        private const float WallHeight = 3f;
        private const float CotoHalfExtent = 20f; // el Coto (5x5 celdas) está centrado en el origen: son los bounds de los boids
        private const int BoidCount = 8;
        private const float BoidY = 0.8f;
        private const float HunterY = 1.1f;
        private const float WolfY = 0.9f;

        // Rutas de patrullaje de los lobos, en celdas (fila, columna) del mapa. Todas fuera de Zonas seguras.
        private static readonly Vector2Int[][] WolfRoutes =
        {
            new[] { new Vector2Int(5, 1), new Vector2Int(11, 1), new Vector2Int(11, 5) },
            new[] { new Vector2Int(5, 13), new Vector2Int(11, 13), new Vector2Int(11, 9) },
            new[] { new Vector2Int(3, 9), new Vector2Int(3, 13), new Vector2Int(1, 8) },
            new[] { new Vector2Int(11, 3), new Vector2Int(11, 11), new Vector2Int(13, 10) }
        };

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
            Material wolfMaterial = TintedMaterial("Wolf", groundMaterial, new Color(0.45f, 0.4f, 0.35f));

            EnsureFolder(PrefabsFolder);

            // La escena se reconstruye desde cero.
            GameObject previousRoot = GameObject.Find(RootName);
            if (previousRoot != null) Object.DestroyImmediate(previousRoot);

            GameObject boidPrefab = BuildBoidPrefab(boidMaterial, evadingMaterial, interactingMaterial, deadMaterial);
            GameObject interestPrefab = BuildInterestPrefab(interestMaterial);
            GameObject projectilePrefab = BuildProjectilePrefab(projectileMaterial);

            GameObject root = new GameObject(RootName);
            NavGraph graph = BuildMap(root.transform, groundMaterial);
            HunterNPC hunter = BuildHunter(root.transform, hunterMaterial, graph, interestPrefab, projectilePrefab);
            BuildBoids(root.transform, boidPrefab, boidMaterial, evadingMaterial, interactingMaterial, deadMaterial);
            BuildWolves(root.transform, wolfMaterial, waypointMaterial, graph, hunter);
            FrameCamera();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            Debug.Log($"[FlockingSceneBuilder] Escena reconstruida → mapa {graph.Rows[0].Length}x{graph.Rows.Length} celdas ({graph.Nodes.Count} nodos), " +
                      $"{BoidCount} boids, 1 cazador (FSM Search/Attack/Gather/Flee/Exit/Dead), {WolfRoutes.Length} lobos, " +
                      $"{graph.SafeZoneTargets.Count} zonas seguras.");
        }

        // ---------------- prefabs ----------------
        private static GameObject BuildBoidPrefab(Material boid, Material evading, Material interacting, Material dead)
        {
            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            temp.name = "Boid";
            temp.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            MakeTrigger(temp);

            MeshRenderer meshRenderer = temp.GetComponent<MeshRenderer>();
            if (meshRenderer != null && boid != null) meshRenderer.sharedMaterial = boid;

            BoidAgent agent = temp.AddComponent<BoidAgent>();
            agent.Configure(0, CotoHalfExtent, boid, evading, interacting, dead);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, $"{PrefabsFolder}/Boid.prefab");
            Object.DestroyImmediate(temp);
            return prefab;
        }

        private static GameObject BuildInterestPrefab(Material interestMaterial)
        {
            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            temp.name = "InterestObject";
            temp.transform.localScale = Vector3.one * 1.6f;
            MakeTrigger(temp); // no debe bloquear la línea de visión

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
        /// <summary>Paredes (con collider, bloquean la visión) y pisos coloreados por zona, desde el mapa del NavGraph.</summary>
        private static NavGraph BuildMap(Transform parent, Material groundMaterial)
        {
            GameObject mapRoot = new GameObject("Mapa");
            mapRoot.transform.SetParent(parent, false);

            NavGraph graph = mapRoot.AddComponent<NavGraph>();
            graph.Build(NavGraph.DefaultRows, CellSize);

            for (int r = 0; r < graph.Rows.Length; r++)
            {
                for (int c = 0; c < graph.Rows[r].Length; c++)
                {
                    Zone zone = NavGraph.ZoneOf(graph.CharAt(r, c));
                    Vector3 center = graph.CellCenter(r, c);
                    Material material = TintedMaterial($"Zone_{zone}", groundMaterial, NavGraph.ZoneColor(zone));

                    GameObject cell = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cell.transform.SetParent(mapRoot.transform, false);
                    cell.GetComponent<MeshRenderer>().sharedMaterial = material;

                    if (zone == Zone.Wall)
                    {
                        cell.name = $"Pared_{r}_{c}";
                        cell.transform.position = center + Vector3.up * WallHeight * 0.5f;
                        cell.transform.localScale = new Vector3(CellSize, WallHeight, CellSize);
                    }
                    else
                    {
                        cell.name = $"Piso_{zone}_{r}_{c}";
                        cell.transform.position = center + Vector3.down * 0.05f;
                        cell.transform.localScale = new Vector3(CellSize, 0.1f, CellSize);
                        Object.DestroyImmediate(cell.GetComponent<Collider>());
                    }
                }
            }

            return graph;
        }

        private static HunterNPC BuildHunter(Transform parent, Material hunterMaterial, NavGraph graph, GameObject interestPrefab, GameObject projectilePrefab)
        {
            GameObject hunter = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            hunter.name = "Cazador";
            hunter.transform.SetParent(parent, false);
            hunter.transform.position = graph.StartNode.Position + Vector3.up * HunterY;
            hunter.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
            MakeTrigger(hunter);

            MeshRenderer meshRenderer = hunter.GetComponent<MeshRenderer>();
            if (meshRenderer != null && hunterMaterial != null) meshRenderer.sharedMaterial = hunterMaterial;

            HunterNPC npc = hunter.AddComponent<HunterNPC>();
            npc.Configure(interestPrefab, projectilePrefab, graph.MapHalfExtent, CotoHalfExtent);
            return npc;
        }

        private static void BuildBoids(Transform parent, GameObject boidPrefab, Material boid, Material evading, Material interacting, Material dead)
        {
            GameObject boidsRoot = new GameObject("Boids");
            boidsRoot.transform.SetParent(parent, false);

            for (int i = 0; i < BoidCount; i++)
            {
                float angle = (i / (float)BoidCount) * Mathf.PI * 2f;
                Vector3 position = new Vector3(Mathf.Cos(angle) * 12f, BoidY, Mathf.Sin(angle) * 12f);

                GameObject boidInstance = (GameObject)PrefabUtility.InstantiatePrefab(boidPrefab, boidsRoot.transform);
                boidInstance.name = $"Boid_{i + 1}";
                boidInstance.transform.position = position;
                boidInstance.transform.rotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);

                BoidAgent agent = boidInstance.GetComponent<BoidAgent>();
                if (agent != null) agent.Configure(i + 1, CotoHalfExtent, boid, evading, interacting, dead);
            }
        }

        private static void BuildWolves(Transform parent, Material wolfMaterial, Material waypointMaterial, NavGraph graph, HunterNPC hunter)
        {
            GameObject wolvesRoot = new GameObject("Lobos");
            wolvesRoot.transform.SetParent(parent, false);

            for (int w = 0; w < WolfRoutes.Length; w++)
            {
                Vector2Int[] route = WolfRoutes[w];
                Transform[] waypoints = new Transform[route.Length];

                GameObject routeRoot = new GameObject($"Waypoints_Lobo_{w + 1}");
                routeRoot.transform.SetParent(wolvesRoot.transform, false);

                for (int i = 0; i < route.Length; i++)
                {
                    GameObject waypoint = new GameObject($"WP_{w + 1}_{i + 1}");
                    waypoint.transform.SetParent(routeRoot.transform, false);
                    waypoint.transform.position = graph.CellCenter(route[i].x, route[i].y) + Vector3.up * 0.05f;

                    GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    marker.name = "Marker";
                    marker.transform.SetParent(waypoint.transform, false);
                    marker.transform.localScale = new Vector3(0.9f, 0.05f, 0.9f);
                    Object.DestroyImmediate(marker.GetComponent<Collider>());
                    if (waypointMaterial != null) marker.GetComponent<MeshRenderer>().sharedMaterial = waypointMaterial;

                    waypoints[i] = waypoint.transform;
                }

                GameObject wolf = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                wolf.name = $"Lobo_{w + 1}";
                wolf.transform.SetParent(wolvesRoot.transform, false);
                wolf.transform.position = new Vector3(waypoints[0].position.x, WolfY, waypoints[0].position.z);
                wolf.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
                MakeTrigger(wolf);
                if (wolfMaterial != null) wolf.GetComponent<MeshRenderer>().sharedMaterial = wolfMaterial;

                // Hocico: indica hacia dónde mira (campo de visión).
                GameObject snout = GameObject.CreatePrimitive(PrimitiveType.Cube);
                snout.name = "Hocico";
                snout.transform.SetParent(wolf.transform, false);
                snout.transform.localPosition = new Vector3(0f, 0.4f, 0.55f);
                snout.transform.localScale = new Vector3(0.35f, 0.3f, 0.5f);
                Object.DestroyImmediate(snout.GetComponent<Collider>());

                wolf.AddComponent<WolfAgent>().Configure(waypoints, hunter);
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

            camera.transform.position = new Vector3(0f, 120f, -70f);
            camera.transform.rotation = Quaternion.Euler(60f, 0f, 0f);
        }

        // ---------------- utilidades ----------------
        private static void MakeTrigger(GameObject target)
        {
            // Los agentes usan colliders trigger: los linecasts de percepción los ignoran y sólo chocan con paredes.
            Collider collider = target.GetComponent<Collider>();
            if (collider != null) collider.isTrigger = true;
        }

        private static Material LoadMaterial(string materialName)
        {
            string path = $"{MaterialsFolder}/{materialName}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) Debug.LogWarning($"[FlockingSceneBuilder] No se encontró el material {path}");
            return material;
        }

        /// <summary>Carga o crea (copiando baseMaterial) un material de color.</summary>
        private static Material TintedMaterial(string materialName, Material baseMaterial, Color color)
        {
            string path = $"{MaterialsFolder}/{materialName}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null || baseMaterial == null) return material;

            material = new Material(baseMaterial) { color = color };
            AssetDatabase.CreateAsset(material, path);
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
