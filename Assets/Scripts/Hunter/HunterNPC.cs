using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace IA.Agents
{
    //NPC cazador, todo su comportamiento lo maneja la FSM (HunterFSM)
    [RequireComponent(typeof(Collider))]
    public partial class HunterNPC : MonoBehaviour
    {
        private const float InterestObjectY = 0.8f;

        [Header("Variables obligatorias")]
        [SerializeField] private float tba = 3f;
        [SerializeField] private float rangeAttackRadius = 12f;
        [SerializeField] private float meleeAttackRadius = 2.6f;
        [SerializeField] private int requiredGathers = 3;
        [SerializeField] private float respawnTime = 5f;
        [SerializeField] private float safeZoneWaitTime = 4f;

        [Header("Percepción (distancia + línea de visión)")]
        [FormerlySerializedAs("perceptionRadius")]
        [SerializeField] private float visionRadius = 10f;
        [SerializeField] private float wolfDetectionRadius = 8f;

        [Header("Movimiento")]
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float acceleration = 16f;
        [SerializeField] private float turnSpeed = 7f;

        [Header("Attack")]
        [SerializeField] private float attackWindup = 0.45f;
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
        [SerializeField] private float gatherDuration = 2.5f;
        [SerializeField] private float gatherRadius = 1.8f;

        [Header("Escenario")]
        [SerializeField] private float arenaHalfExtent = 30f;

        [Header("Feedback visual")]
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
        private readonly Collider[] overlapBuffer = new Collider[64];
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
        public bool WasCaught => wasCaught; //lo alcanzo un lobo, la FSM lo pasa a Dead
        public PathFollower Path => path;
        public Zone CurrentZone => NavGraph.Instance != null ? NavGraph.Instance.ZoneAt(transform.position) : Zone.Corridor;
        public bool IsInSafeZone => CurrentZone == Zone.Safe;
        //los lobos solo lo pueden atrapar si esta vivo y fuera de zona segura
        public bool IsTargetable => fsm != null && fsm.CurrentId != HunterStateId.Dead && !IsInSafeZone;
        public NavNode ChosenSafeZone { get; set; }

        public float ActionProgress { get; set; }

        public void Configure(GameObject newInterestObjectPrefab, GameObject newProjectilePrefab, float newArenaHalfExtent, float cotoHalfExtent)
        {
            interestObjectPrefab = newInterestObjectPrefab;
            projectilePrefab = newProjectilePrefab;
            arenaHalfExtent = newArenaHalfExtent;
            spawnAreaHalfExtent = Mathf.Max(2f, cotoHalfExtent - 2f);
        }

        public void NotifyCaught() //lo llama el lobo cuando lo alcanza
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