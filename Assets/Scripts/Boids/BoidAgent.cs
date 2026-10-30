using System.Collections.Generic;
using UnityEngine;

namespace IA.Agents
{

    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(BoidHealth))]
    public partial class BoidAgent : MonoBehaviour
    {
        [Header("Identidad")]
        [SerializeField] private int agentId = 1;

        [Header("Radios de percepción locales")]
        [SerializeField] private float separationRadius = 2.5f;
        [SerializeField] private float perceptionRadius = 7f;
        [SerializeField] private float viewRadius = 8f;
        [SerializeField] private float interestSightRadius = 12f;

        [Header("Movimiento")]
        [SerializeField] private float maxSpeed = 6.5f;
        [SerializeField] private float maxForce = 22f;
        [SerializeField] private float turnSpeed = 9f;

        [Header("Pesos de los comportamientos")]
        [SerializeField] private float separationWeight = 2.2f;
        [SerializeField] private float alignmentWeight = 1f;
        [SerializeField] private float cohesionWeight = 1f;
        [SerializeField] private float evadeWeight = 5f;
        [SerializeField] private float arriveWeight = 2.6f;
        [SerializeField] private float wanderWeight = 0.8f;
        [SerializeField] private float evadeMemoryDuration = 2.5f;
        [SerializeField] private float evadeSafeDistance = 18f;

        [Header("Anti-superposición")]
        [SerializeField] private float minSeparationDistance = 1.4f;

        [Header("Interacción con objetos de interés")]
        [SerializeField] private float interactionDistance = 2f;
        [SerializeField] private float interactionInterval = 1f;
        [SerializeField] private float interactionDamage = 25f;

        [Header("Escenario")]
        [SerializeField] private float arenaHalfExtent = 22f;

        [Header("Feedback visual")]
        [SerializeField] private bool showDebugLines = true;
        [SerializeField] private Material flockingMaterial;
        [SerializeField] private Material evadingMaterial;
        [SerializeField] private Material interactingMaterial;
        [SerializeField] private Material deadMaterial;

        // ---------------- estado interno ----------------
        private readonly List<BoidAgent> neighbors = new List<BoidAgent>(16);
        private readonly List<BoidAgent> deadNeighbors = new List<BoidAgent>(8);
        private readonly List<InterestObject> visibleInterests = new List<InterestObject>(8);
        private readonly Collider[] overlapBuffer = new Collider[64];

        private BoidHealth life; //la vida, muerte y respawn estan en su propio script
        private Collider bodyCollider;
        private Renderer bodyRenderer;
        private HunterNPC detectedHunter;
        private Vector3 lastThreatPosition;
        private InterestObject targetInterest;
        private BoidState state = BoidState.Flocking;
        private Vector3 velocity;
        private float interactionTimer;
        private float wanderAngle;
        private float groundY;
        private float evadeMemoryTimer;

        public int AgentId => agentId;
        public BoidState State => state;
        public float Health => life.Health;
        public float MaxHealth => life.MaxHealth;
        public Vector3 Velocity => velocity;
        public bool IsAlive => state != BoidState.Dead && state != BoidState.Respawning;
        public bool IsHarvestable => state == BoidState.Dead;
        public HunterNPC DetectedHunter => detectedHunter;
        public InterestObject TargetInterest => targetInterest;
        public IReadOnlyList<BoidAgent> Neighbors => neighbors;

        public void Configure(int newAgentId, float newArenaHalfExtent, Material newFlocking, Material newEvading, Material newInteracting, Material newDead)
        {
            agentId = newAgentId;
            arenaHalfExtent = newArenaHalfExtent;
            flockingMaterial = newFlocking;
            evadingMaterial = newEvading;
            interactingMaterial = newInteracting;
            deadMaterial = newDead;
        }

        private void Awake()
        {
            bodyCollider = GetComponent<Collider>();
            bodyRenderer = GetComponentInChildren<Renderer>();
            groundY = transform.position.y;
            wanderAngle = Random.Range(0f, 360f);

            life = GetComponent<BoidHealth>();
            if (life == null) life = gameObject.AddComponent<BoidHealth>(); //por si el boid no lo tenia agregado
            life.Died += OnDied;
            life.Harvested += OnHarvested;
            life.Respawned += OnRespawned;
        }

        private void OnDestroy()
        {
            if (life == null) return;
            life.Died -= OnDied;
            life.Harvested -= OnHarvested;
            life.Respawned -= OnRespawned;
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (state == BoidState.Dead || state == BoidState.Respawning) //muerto no hace nada, el respawn lo maneja BoidHealth
            {
                DrawDebugLines();
                return;
            }

            Sense();
            UpdateState(dt);
            ApplySteering(dt);
            ResolveOverlaps();
            TickInteraction(dt);
            UpdateVisual();
            DrawDebugLines();
        }
    }
}