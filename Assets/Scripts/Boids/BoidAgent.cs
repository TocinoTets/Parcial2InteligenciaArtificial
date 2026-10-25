using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace IA.Agents
{
    /// <summary>
    /// Agente autónomo tipo Boid: Separation, Alignment, Cohesion, Evade y Arrive.
    ///
    /// Cada agente decide EXCLUSIVAMENTE con la información que obtienen sus propios
    /// sensores (Physics.OverlapSphere sobre sus radios locales). No existe controlador
    /// global ni líder: ningún agente conoce la posición/velocidad de un Boid que esté
    /// fuera de su rango de percepción.
    ///
    /// El comportamiento se reparte en archivos parciales:
    ///   BoidAgent.Sensing.cs   → sensores locales y decisión de estado
    ///   BoidAgent.Steering.cs  → actuadores: steering, wrap de la arena y anti-superposición
    ///   BoidAgent.Lifecycle.cs → vida, muerte, recolección y reaparición
    ///   BoidAgent.Feedback.cs  → feedback visual y depuración
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public partial class BoidAgent : MonoBehaviour
    {
        [Header("Identidad")]
        [SerializeField] private int agentId = 1;

        [Header("Radios de percepción locales")]
        [Tooltip("Separation: radio MENOR y distinto al de Alignment y Cohesion.")]
        [SerializeField] private float separationRadius = 2.5f;
        [Tooltip("Alignment + Cohesion: radio de percepción del grupo.")]
        [SerializeField] private float perceptionRadius = 7f;
        [Tooltip("Rango de visión con el que detecta al NPC Cazador (dispara Evade).")]
        [SerializeField] private float viewRadius = 8f;
        [Tooltip("Alcance limitado con el que detecta los objetos de interés del escenario.")]
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
        [Tooltip("Memoria de huida: segundos que el boid sigue escapando tras perder de vista al Cazador.")]
        [SerializeField] private float evadeMemoryDuration = 2.5f;
        [Tooltip("Distancia (m) a la que el boid da la huida por terminada: la fuerza se desvanece entre viewRadius y este valor.")]
        [SerializeField] private float evadeSafeDistance = 18f;

        [Header("Anti-superposición")]
        [SerializeField] private float minSeparationDistance = 1.4f;

        [Header("Vida e interacción")]
        [SerializeField] private float maxHealth = 50f;
        [SerializeField] private float interactionDistance = 2f;
        [SerializeField] private float interactionInterval = 1f;
        [SerializeField] private float interactionDamage = 25f;
        [SerializeField] private float respawnDelay = 6f;

        [Header("Escenario")]
        [SerializeField] private float arenaHalfExtent = 22f;

        [Header("Feedback visual")]
        [SerializeField] private bool showDebugInfo = false;
        [SerializeField] private bool showDebugLines = true;
        [SerializeField] private Material flockingMaterial;
        [SerializeField] private Material evadingMaterial;
        [SerializeField] private Material interactingMaterial;
        [SerializeField] private Material deadMaterial;

        // ---------------- estado interno ----------------
        private readonly List<BoidAgent> neighbors = new List<BoidAgent>(16);
        private readonly List<BoidAgent> deadNeighbors = new List<BoidAgent>(8);
        private readonly List<InterestObject> visibleInterests = new List<InterestObject>(8);
        private readonly List<string> recentEvents = new List<string>(4);
        private readonly Collider[] overlapBuffer = new Collider[64];
        private readonly AgentLabel label = new AgentLabel();
        private readonly StringBuilder textBuilder = new StringBuilder(320);

        private Collider bodyCollider;
        private Renderer bodyRenderer;
        private HunterNPC detectedHunter;
        private Vector3 lastThreatPosition;
        private InterestObject targetInterest;
        private BoidState state = BoidState.Flocking;
        private Vector3 velocity;
        private float health;
        private float interactionTimer;
        private float respawnTimer;
        private float wanderAngle;
        private float groundY;
        private float evadeMemoryTimer;

        public int AgentId => agentId;
        public BoidState State => state;
        public float Health => health;
        public float MaxHealth => maxHealth;
        public Vector3 Velocity => velocity;
        public bool IsAlive => state != BoidState.Dead && state != BoidState.Respawning;
        public bool IsHarvestable => state == BoidState.Dead;
        public HunterNPC DetectedHunter => detectedHunter;
        public InterestObject TargetInterest => targetInterest;
        public IReadOnlyList<BoidAgent> Neighbors => neighbors;
        public string LastEvent { get; private set; } = "-";

        /// <summary>Configuración aplicada por el generador de escena (materiales, id, límites del escenario).</summary>
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
            health = maxHealth;
            wanderAngle = Random.Range(0f, 360f);
            LogEvent("inicia (Flocking)");
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (state == BoidState.Dead || state == BoidState.Respawning)
            {
                TickInactive(dt);
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
