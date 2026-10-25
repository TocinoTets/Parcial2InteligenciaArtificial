using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;

namespace IA.Agents
{
    /// <summary>
    /// NPC Cazador.
    ///
    /// Todo su comportamiento está gestionado por una Máquina de Estados Finita
    /// (HunterFSM): Search, Attack, Gather, Flee, Exit y Dead. Este componente sólo aporta
    /// sensores (boids y lobos, con línea de visión), actuadores (movimiento, A* y ataques)
    /// y las variables de contexto. Ninguna transición se dispara desde aquí: los estados y
    /// la propia FSM evalúan sus condiciones y llaman a FSM.ChangeState.
    ///
    /// El componente se reparte en archivos parciales:
    ///   HunterNPC.Sensing.cs  → percepción de boids y lobos
    ///   HunterNPC.Motion.cs   → movimiento, Pursuit y navegación con A*
    ///   HunterNPC.Combat.cs   → ataques cuerpo a cuerpo y a distancia
    ///   HunterNPC.Spawning.cs → objetos de interés generados en Search
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
        [Tooltip("RequiredGathers: recolecciones necesarias para dirigirse a la Salida.")]
        [SerializeField] private int requiredGathers = 3;
        [Tooltip("RespawnTime: tiempo hasta reaparecer en la Zona inicial al ser alcanzado por un lobo.")]
        [SerializeField] private float respawnTime = 5f;
        [Tooltip("SafeZoneWaitTime: tiempo de espera dentro de una Zona segura antes de retomar la actividad.")]
        [SerializeField] private float safeZoneWaitTime = 4f;

        [Header("Percepción (distancia + línea de visión)")]
        [Tooltip("VisionRadius: radio de percepción de boids.")]
        [FormerlySerializedAs("perceptionRadius")]
        [SerializeField] private float visionRadius = 10f;
        [Tooltip("Distancia máxima a la que detecta lobos (sin ángulo de visión).")]
        [SerializeField] private float wolfDetectionRadius = 8f;

        [Header("Movimiento")]
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float acceleration = 16f;
        [SerializeField] private float turnSpeed = 7f;

        [Header("Attack")]
        [Tooltip("Tiempo de anticipación antes de concretar el golpe.")]
        [SerializeField] private float attackWindup = 0.45f;
        [Tooltip("Distancia a la que se detiene para golpear cuerpo a cuerpo.")]
        [SerializeField] private float meleeStopDistance = 1.7f;
        [SerializeField] private float meleeDamage = 34f;
        [SerializeField] private float rangedDamage = 20f;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float projectileSpeed = 18f;

        [Header("Objetos de interés (generados en Search, dentro del Coto)")]
        [SerializeField] private GameObject interestObjectPrefab;
        [SerializeField] private float interestSpawnInterval = 5f;
        [SerializeField] private int maxActiveInterestObjects = 5;
        [SerializeField] private float spawnAreaHalfExtent = 8f;

        [Header("Gather")]
        [Tooltip("Duración de la acción de recolección.")]
        [SerializeField] private float gatherDuration = 2.5f;
        [SerializeField] private float gatherRadius = 1.8f;

        [Header("Escenario")]
        [Tooltip("Mitad del tamaño del mapa completo (límite de movimiento).")]
        [SerializeField] private float arenaHalfExtent = 30f;

        [Header("Feedback visual")]
        [SerializeField] private bool showDebugInfo = false;
        [SerializeField] private bool showDebugLines = true;
        [FormerlySerializedAs("patrolColor")]
        [SerializeField] private Color searchColor = new Color(0.25f, 0.7f, 1f);
        [SerializeField] private Color attackColor = new Color(1f, 0.15f, 0.15f);
        [SerializeField] private Color gatherColor = new Color(0.85f, 0.3f, 0.95f);
        [SerializeField] private Color fleeColor = new Color(1f, 0.9f, 0.1f);
        [SerializeField] private Color exitColor = new Color(0.2f, 1f, 0.4f);
        [SerializeField] private Color deadColor = new Color(0.25f, 0.25f, 0.25f);

        // ---------------- estado interno ----------------
        private readonly List<BoidAgent> detectedBoids = new List<BoidAgent>(16);
        private readonly List<string> recentActions = new List<string>(6);
        private readonly Collider[] overlapBuffer = new Collider[64];
        private readonly AgentLabel label = new AgentLabel();
        private readonly StringBuilder textBuilder = new StringBuilder(320);
        private readonly PathFollower path = new PathFollower();

        private HunterFSM fsm;
        private Collider bodyCollider;
        private Renderer bodyRenderer;
        private MaterialPropertyBlock materialProperties;
        private BoidAgent detectedDeadBoid;
        private BoidAgent closestVisibleBoid;
        private BoidAgent lastMeleeTarget;
        private WolfAgent visibleWolf;
        private Vector3 velocity;
        private Vector3 spawnPosition;
        private int gathers;
        private bool wasCaught;
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
        public float VisionRadius => visionRadius;
        public int RequiredGathers => requiredGathers;
        public float RespawnTime => respawnTime;
        public float SafeZoneWaitTime => safeZoneWaitTime;
        public float AttackWindup => attackWindup;
        public float MeleeStopDistance => meleeStopDistance;
        public float GatherDuration => gatherDuration;
        public float GatherRadius => gatherRadius;
        public float InterestSpawnInterval => interestSpawnInterval;
        public int MaxActiveInterestObjects => maxActiveInterestObjects;

        public IReadOnlyList<BoidAgent> DetectedBoids => detectedBoids;
        public BoidAgent DetectedDeadBoid => detectedDeadBoid;
        public BoidAgent ClosestVisibleBoid => closestVisibleBoid;
        public float AttackTimer => attackTimer;
        public bool IsAttackReady => attackTimer >= tba;

        public WolfAgent VisibleWolf => visibleWolf;
        public int Gathers => gathers;
        /// <summary>Un lobo lo alcanzó (evento percibido; la FSM decide la transición a Dead).</summary>
        public bool WasCaught => wasCaught;
        public PathFollower Path => path;
        public Zone CurrentZone => NavGraph.Instance != null ? NavGraph.Instance.ZoneAt(transform.position) : Zone.Corridor;
        public bool IsInSafeZone => CurrentZone == Zone.Safe;
        /// <summary>Los lobos sólo pueden detectarlo o alcanzarlo si está vivo y fuera de una Zona segura.</summary>
        public bool IsTargetable => fsm != null && fsm.CurrentId != HunterStateId.Dead && !IsInSafeZone;
        /// <summary>Zona segura elegida en Flee (feedback).</summary>
        public NavNode ChosenSafeZone { get; set; }

        /// <summary>Progreso (0-1) de la acción en curso, usado por el feedback visual.</summary>
        public float ActionProgress { get; set; }

        public string LastAction { get; private set; } = "-";
        public IReadOnlyList<string> RecentActions => recentActions;

        /// <summary>Configuración aplicada por el generador de escena.</summary>
        public void Configure(GameObject newInterestObjectPrefab, GameObject newProjectilePrefab, float newArenaHalfExtent, float cotoHalfExtent)
        {
            interestObjectPrefab = newInterestObjectPrefab;
            projectilePrefab = newProjectilePrefab;
            arenaHalfExtent = newArenaHalfExtent;
            spawnAreaHalfExtent = Mathf.Max(2f, cotoHalfExtent - 2f);
        }

        /// <summary>Llamado por un lobo al alcanzarlo.</summary>
        public void NotifyCaught()
        {
            if (IsTargetable) wasCaught = true;
        }

        public void AddGather()
        {
            gathers++;
        }

        public void SetVisible(bool visible)
        {
            if (bodyRenderer != null) bodyRenderer.enabled = visible;
            if (bodyCollider != null) bodyCollider.enabled = visible;
        }

        /// <summary>Reaparece en la Zona inicial con el contador de recolecciones en cero.</summary>
        public void RespawnAtStart()
        {
            transform.position = spawnPosition;
            velocity = Vector3.zero;
            gathers = 0;
            wasCaught = false;
            ActionProgress = 0f;
            ChosenSafeZone = null;
            path.Clear();
            SetVisible(true);
            LogAction("reaparece en la Zona inicial (recolecciones = 0)");
        }

        private void Awake()
        {
            bodyCollider = GetComponent<Collider>();
            bodyRenderer = GetComponentInChildren<Renderer>();
            materialProperties = new MaterialPropertyBlock();
            groundY = transform.position.y;
            baseScale = transform.localScale;
            spawnPosition = transform.position;

            fsm = new HunterFSM(this);
            fsm.Register(new HunterSearchState(this));
            fsm.Register(new HunterAttackState(this));
            fsm.Register(new HunterGatherState(this));
            fsm.Register(new HunterFleeState(this));
            fsm.Register(new HunterExitState(this));
            fsm.Register(new HunterDeadState(this));

            attackTimer = 0f; // el TBA debe cumplirse antes del primer ataque
            fsm.ChangeState(HunterStateId.Search);
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            attackTimer += dt;
            meleePulse = Mathf.Max(0f, meleePulse - dt);

            Sense();
            fsm.Tick(dt); // las transiciones viven dentro de la FSM y sus estados

            UpdateVisual();
            DrawDebugLines();
        }
    }
}
