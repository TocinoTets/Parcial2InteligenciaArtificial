using System.Collections.Generic;
using UnityEngine;

namespace IA.Agents
{
    /// <summary>
    /// Lobo: patrulla waypoints, detecta al Cazador con Field of View / Line of Sight,
    /// aúlla para alertar a la manada, lo persigue y usa A* para ir a la última posición
    /// conocida. Nunca usa nodos de Zonas seguras y abandona la persecución cuando el
    /// Cazador entra en una.
    ///
    /// Su comportamiento se organiza con una FSM simple (WolfState): las transiciones las
    /// deciden las condiciones del propio lobo (lo que percibe y las alertas que recibe).
    /// El feedback visual está en WolfAgent.Feedback.cs.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public partial class WolfAgent : MonoBehaviour
    {
        /// <summary>Manada: los lobos activos, usada para propagar el aullido.</summary>
        public static readonly List<WolfAgent> All = new List<WolfAgent>();

        [Header("Movimiento (debe ser más lento que el Cazador en todos sus estados)")]
        [SerializeField] private float patrolSpeed = 4f;
        [SerializeField] private float chaseSpeed = 5.5f;
        [SerializeField] private float acceleration = 12f;
        [SerializeField] private float turnSpeed = 6f;

        [Header("Percepción (Field of View / Line of Sight)")]
        [SerializeField] private float viewDistance = 13f;
        [SerializeField, Range(10f, 360f)] private float viewAngle = 90f;

        [Header("Captura")]
        [Tooltip("Si llega a menos de esta distancia del Cazador, lo alcanza (Cazador → Dead).")]
        [SerializeField] private float captureRadius = 1.3f;

        [Header("Patrullaje")]
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private float waypointTolerance = 1f;
        [SerializeField] private WaypointLoopMode loopMode = WaypointLoopMode.Loop;

        [Header("Búsqueda y alerta")]
        [SerializeField] private float searchDuration = 6f;
        [SerializeField] private float searchRadius = 16f;
        [Tooltip("Tiempo mínimo entre aullidos mientras sigue viendo al Cazador.")]
        [SerializeField] private float howlCooldown = 3f;

        [Header("Referencias")]
        [SerializeField] private HunterNPC hunter;

        private readonly PathFollower path = new PathFollower();
        private WolfState state = WolfState.Patrol;
        private Vector3 velocity;
        private Vector3 lastKnownPosition;
        private bool hasLastKnown;
        private bool seesHunter;
        private int waypointIndex;
        private int direction = 1;
        private float groundY;
        private float stateTimer;

        public Vector3 Position => transform.position;
        public WolfState State => state;

        /// <summary>Configuración aplicada por el generador de escena.</summary>
        public void Configure(Transform[] newWaypoints, HunterNPC newHunter)
        {
            waypoints = newWaypoints;
            hunter = newHunter;
        }

        private void OnEnable() => All.Add(this);

        private void OnDisable() => All.Remove(this);

        private void Awake()
        {
            groundY = transform.position.y;
            if (hunter == null) hunter = FindFirstObjectByType<HunterNPC>();
            InitFeedback();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            stateTimer += dt;

            bool targetable = hunter != null && hunter.IsTargetable;
            seesHunter = targetable && Perception.CanSee(Position, transform.forward, hunter.Position, viewDistance, viewAngle);

            if (!targetable && (state != WolfState.Patrol || hasLastKnown)) LoseTarget();
            else if (seesHunter) OnHunterSeen();

            switch (state)
            {
                case WolfState.Patrol: TickPatrol(); break;
                case WolfState.Chase: TickChase(); break;
                case WolfState.GoToLastKnown: TickGoToLastKnown(); break;
                case WolfState.Search: TickSearch(); break;
            }

            if (targetable && AgentMath.FlatDistance(Position, hunter.Position) <= captureRadius)
            {
                Log("alcanza al Cazador");
                hunter.NotifyCaught();
            }

            UpdateFeedback();
        }

        // ---------------- percepción y comunicación ----------------
        private void OnHunterSeen()
        {
            lastKnownPosition = hunter.Position;
            hasLastKnown = true;

            if (state != WolfState.Chase)
            {
                Log($"DETECTA al Cazador en ({lastKnownPosition.x:0.0}, {lastKnownPosition.z:0.0})");
                SetState(WolfState.Chase);
                Howl();
            }
            else if (Time.time - lastHowlTime >= howlCooldown)
            {
                Howl();
            }
        }

        /// <summary>Aullido: transmite la posición actual del Cazador como última posición conocida.</summary>
        private void Howl()
        {
            lastHowlTime = Time.time;
            Log($"¡AUUU! aullido de alerta → posición ({lastKnownPosition.x:0.0}, {lastKnownPosition.z:0.0})");

            foreach (WolfAgent other in All)
                if (other != this) other.ReceiveAlert(lastKnownPosition, this);
        }

        public void ReceiveAlert(Vector3 position, WolfAgent sender)
        {
            alertReceivedTime = Time.time;
            if (state == WolfState.Chase) return; // ya lo ve: sigue con su propia información

            lastKnownPosition = position;
            hasLastKnown = true;
            Log($"recibe la alerta de {sender.name} → A* hacia ({position.x:0.0}, {position.z:0.0})");
            SetState(WolfState.GoToLastKnown);
        }

        private void LoseTarget()
        {
            hasLastKnown = false;
            Log(hunter != null && hunter.FSM != null && hunter.FSM.CurrentId == HunterStateId.Dead
                ? "el Cazador fue alcanzado → vuelve a patrullar"
                : "el Cazador entró en una Zona segura → pierde el objetivo y vuelve a patrullar");
            SetState(WolfState.Patrol);
        }

        private void SetState(WolfState next)
        {
            state = next;
            stateTimer = 0f;
            path.Clear();
        }

        // ---------------- estados ----------------
        private void TickPatrol()
        {
            if (waypoints == null || waypoints.Length == 0)
            {
                MoveTowards(Position, 0f);
                return;
            }

            Transform waypoint = waypoints[waypointIndex];
            if (AgentMath.FlatDistance(Position, waypoint.position) <= waypointTolerance)
            {
                AdvanceWaypoint();
                waypoint = waypoints[waypointIndex];
                path.Clear();
            }

            if (Perception.HasLineOfSight(Position, waypoint.position))
            {
                path.Clear();
                MoveTowards(waypoint.position, patrolSpeed);
                return;
            }

            if (!path.IsActive) RequestPath(waypoint.position, $"waypoint {waypoint.name} fuera de línea de visión");
            MoveTowards(path.NextPoint(Position), patrolSpeed);
        }

        private void TickChase()
        {
            if (seesHunter)
            {
                MoveTowards(hunter.Position, chaseSpeed);
                return;
            }

            Log("pierde de vista al Cazador → A* a la última posición conocida");
            SetState(WolfState.GoToLastKnown);
        }

        private void TickGoToLastKnown()
        {
            if (!hasLastKnown)
            {
                SetState(WolfState.Patrol);
                return;
            }

            if (AgentMath.FlatDistance(Position, lastKnownPosition) <= 1.5f)
            {
                Log("llegó a la última posición conocida → busca en la zona");
                SetState(WolfState.Search);
                return;
            }

            if (!path.IsActive && !RequestPath(lastKnownPosition, "última posición conocida"))
            {
                SetState(WolfState.Search);
                return;
            }

            MoveTowards(path.NextPoint(Position), chaseSpeed);
        }

        private void TickSearch()
        {
            if (stateTimer >= searchDuration)
            {
                Log("no encontró al Cazador → descarta la posición y vuelve a patrullar");
                hasLastKnown = false;
                SetState(WolfState.Patrol);
                return;
            }

            if (!path.IsActive && NavGraph.Instance != null)
            {
                NavNode node = NavGraph.Instance.RandomNodeNear(lastKnownPosition, searchRadius, false);
                if (node != null) RequestPath(node.Position, "búsqueda alrededor de la última posición");
            }

            MoveTowards(path.NextPoint(Position), patrolSpeed);
        }

        private void AdvanceWaypoint()
        {
            if (loopMode == WaypointLoopMode.Loop)
            {
                waypointIndex = (waypointIndex + 1) % waypoints.Length; // vuelve al primer waypoint
                return;
            }

            if (waypoints.Length == 1) return;
            if (waypointIndex + direction >= waypoints.Length || waypointIndex + direction < 0) direction = -direction;
            waypointIndex += direction;
        }

        // ---------------- movimiento ----------------
        /// <summary>A* sin nodos de Zonas seguras.</summary>
        private bool RequestPath(Vector3 target, string reason)
        {
            bool found = path.SetPath(Position, target, false);
            Log(found ? $"A* ({reason}): {path.Points.Count} puntos" : $"A* ({reason}): sin camino");
            return found;
        }

        private void MoveTowards(Vector3 target, float speed)
        {
            float dt = Time.deltaTime;
            Vector3 desired = AgentMath.FlatDirection(Position, target) * speed;
            velocity = Vector3.MoveTowards(velocity, desired, acceleration * dt);

            Vector3 position = Position + velocity * dt;
            position.y = groundY;
            transform.position = position;

            Vector3 flat = AgentMath.Flat(velocity);
            if (flat.sqrMagnitude > 0.05f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat), turnSpeed * dt);
        }
    }
}
