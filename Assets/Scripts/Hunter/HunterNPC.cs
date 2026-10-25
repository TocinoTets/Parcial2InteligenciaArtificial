using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace IA.Agents
{
    /// <summary>
    /// NPC Cazador.
    ///
    /// Todo su comportamiento está gestionado por una Máquina de Estados Finita
    /// (HunterFSM): Patrol, Attack y Gather. Este componente sólo aporta sensores
    /// (percepción limitada por radio), actuadores (movimiento y ataques) y las
    /// variables de contexto. Ninguna transición se dispara desde aquí: cada estado
    /// evalúa sus condiciones y llama a FSM.ChangeState.
    ///
    /// El componente se reparte en archivos parciales:
    ///   HunterNPC.Sensing.cs  → percepción local de boids
    ///   HunterNPC.Motion.cs   → movimiento y orientación
    ///   HunterNPC.Combat.cs   → ataques cuerpo a cuerpo y a distancia
    ///   HunterNPC.Spawning.cs → objetos de interés generados en Patrol
    ///   HunterNPC.Feedback.cs → feedback visual, etiqueta de depuración y gizmos
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public partial class HunterNPC : MonoBehaviour
    {
        private const float InterestObjectY = 0.8f;

        [Header("Variables obligatorias")]
        [Tooltip("TBA - Time Between Attacks: tiempo mínimo entre ataques (segundos).")]
        [SerializeField] private float tba = 3f;
        [Tooltip("RangeAttackRadius: distancia máxima para realizar un ataque a distancia.")]
        [SerializeField] private float rangeAttackRadius = 12f;
        [Tooltip("MeleeAttackRadius: distancia máxima para realizar un ataque cuerpo a cuerpo.")]
        [SerializeField] private float meleeAttackRadius = 2.6f;

        [Header("Percepción (limitada por radio)")]
        [SerializeField] private float perceptionRadius = 18f;

        [Header("Movimiento")]
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float acceleration = 16f;
        [SerializeField] private float turnSpeed = 7f;

        [Header("Patrol: waypoints")]
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private float waypointTolerance = 1.2f;
        [SerializeField] private WaypointLoopMode loopMode = WaypointLoopMode.PingPong;

        [Header("Attack")]
        [Tooltip("Tiempo de anticipación antes de concretar el golpe.")]
        [SerializeField] private float attackWindup = 0.45f;
        [Tooltip("Distancia a la que se detiene para golpear cuerpo a cuerpo.")]
        [SerializeField] private float meleeStopDistance = 1.7f;
        [SerializeField] private float meleeDamage = 34f;
        [SerializeField] private float rangedDamage = 20f;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float projectileSpeed = 18f;

        [Header("Objetos de interés (generados en Patrol)")]
        [SerializeField] private GameObject interestObjectPrefab;
        [SerializeField] private float interestSpawnInterval = 5f;
        [SerializeField] private int maxActiveInterestObjects = 5;
        [SerializeField] private float spawnAreaHalfExtent = 20f;

        [Header("Gather")]
        [Tooltip("Duración de la acción de recolección.")]
        [SerializeField] private float gatherDuration = 2.5f;
        [SerializeField] private float gatherRadius = 1.8f;

        [Header("Escenario")]
        [SerializeField] private float arenaHalfExtent = 22f;

        [Header("Feedback visual")]
        [SerializeField] private bool showDebugInfo = false;
        [SerializeField] private bool showDebugLines = true;
        [SerializeField] private Color patrolColor = new Color(0.25f, 0.7f, 1f);
        [SerializeField] private Color attackColor = new Color(1f, 0.15f, 0.15f);
        [SerializeField] private Color gatherColor = new Color(0.85f, 0.3f, 0.95f);

        // ---------------- estado interno ----------------
        private readonly List<BoidAgent> detectedBoids = new List<BoidAgent>(16);
        private readonly List<string> recentActions = new List<string>(6);
        private readonly Collider[] overlapBuffer = new Collider[64];
        private readonly AgentLabel label = new AgentLabel();
        private readonly StringBuilder textBuilder = new StringBuilder(320);

        private HunterFSM fsm;
        private Collider bodyCollider;
        private Renderer bodyRenderer;
        private MaterialPropertyBlock materialProperties;
        private BoidAgent detectedDeadBoid;
        private BoidAgent closestVisibleBoid;
        private BoidAgent lastMeleeTarget;
        private Vector3 velocity;
        private float attackTimer;
        private float groundY;
        private float meleePulse;
        private float lastMeleeTime = -10f;
        private Vector3 baseScale = Vector3.one;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public HunterFSM FSM => fsm;
        public Vector3 Position => transform.position;
        public Vector3 Velocity => velocity;

        public float TBA => tba;
        public float RangeAttackRadius => rangeAttackRadius;
        public float MeleeAttackRadius => meleeAttackRadius;
        public float PerceptionRadius => perceptionRadius;
        public float AttackWindup => attackWindup;
        public float MeleeStopDistance => meleeStopDistance;
        public float GatherDuration => gatherDuration;
        public float GatherRadius => gatherRadius;
        public float InterestSpawnInterval => interestSpawnInterval;
        public int MaxActiveInterestObjects => maxActiveInterestObjects;
        public Transform[] Waypoints => waypoints;
        public float WaypointTolerance => waypointTolerance;
        public WaypointLoopMode LoopMode => loopMode;

        public IReadOnlyList<BoidAgent> DetectedBoids => detectedBoids;
        public BoidAgent DetectedDeadBoid => detectedDeadBoid;
        public BoidAgent ClosestVisibleBoid => closestVisibleBoid;
        public float AttackTimer => attackTimer;
        public bool IsAttackReady => attackTimer >= tba;

        /// <summary>Progreso (0-1) de la acción en curso, usado por el feedback visual.</summary>
        public float ActionProgress { get; set; }

        public string LastAction { get; private set; } = "-";
        public IReadOnlyList<string> RecentActions => recentActions;

        /// <summary>Configuración aplicada por el generador de escena.</summary>
        public void Configure(Transform[] newWaypoints, GameObject newInterestObjectPrefab, GameObject newProjectilePrefab, float newArenaHalfExtent)
        {
            waypoints = newWaypoints;
            interestObjectPrefab = newInterestObjectPrefab;
            projectilePrefab = newProjectilePrefab;
            arenaHalfExtent = newArenaHalfExtent;
            spawnAreaHalfExtent = Mathf.Max(4f, newArenaHalfExtent - 2f);
        }

        private void Awake()
        {
            bodyCollider = GetComponent<Collider>();
            bodyRenderer = GetComponentInChildren<Renderer>();
            materialProperties = new MaterialPropertyBlock();
            groundY = transform.position.y;
            baseScale = transform.localScale;

            fsm = new HunterFSM(this);
            fsm.Register(new HunterPatrolState(this));
            fsm.Register(new HunterAttackState(this));
            fsm.Register(new HunterGatherState(this));

            attackTimer = 0f; // el TBA debe cumplirse antes del primer ataque
            fsm.ChangeState(HunterStateId.Patrol);
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            attackTimer += dt;
            meleePulse = Mathf.Max(0f, meleePulse - dt);

            Sense();
            fsm.Tick(dt); // las transiciones viven dentro de los estados de la FSM

            UpdateVisual();
            DrawDebugLines();
        }
    }
}
