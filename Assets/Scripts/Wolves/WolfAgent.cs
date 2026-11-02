using System.Collections.Generic;
using UnityEngine;

namespace IA.Agents
{
    //lobo: patrulla, detecta al cazador con FOV y LOS, aulla para avisar a la manada y lo persigue
    //nunca pasa por zonas seguras, si el cazador entra a una lo deja
    [RequireComponent(typeof(Collider))]
    public partial class WolfAgent : MonoBehaviour
    {
        public static readonly List<WolfAgent> All = new List<WolfAgent>(); //todos los lobos, se usa para el aullido

        [Header("Movimiento (debe ser más lento que el Cazador en todos sus estados)")]
        [SerializeField] private float patrolSpeed = 4f;
        [SerializeField] private float chaseSpeed = 5.5f;
        [SerializeField] private float acceleration = 12f;
        [SerializeField] private float turnSpeed = 6f;

        [Header("Percepción (Field of View / Line of Sight)")]
        [SerializeField] private float viewDistance = 13f;
        [SerializeField, Range(10f, 360f)] private float viewAngle = 90f;

        [Header("Captura")]
        [SerializeField] private float captureRadius = 1.3f;

        [Header("Patrullaje")]
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private float waypointTolerance = 1f;
        [SerializeField] private WaypointLoopMode loopMode = WaypointLoopMode.Loop;

        [Header("Búsqueda y alerta")]
        [SerializeField] private float searchDuration = 6f;
        [SerializeField] private float searchRadius = 16f;
        [SerializeField] private float howlCooldown = 3f;

        private readonly PathFollower path = new PathFollower();
        private readonly Collider[] overlapBuffer = new Collider[16];
        private HunterNPC hunter;
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

        public void Configure(Transform[] newWaypoints)
        {
            waypoints = newWaypoints;
        }

        private void OnEnable() => All.Add(this);

        private void OnDisable() => All.Remove(this);

        private void Awake()
        {
            groundY = transform.position.y;
            InitFeedback();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            stateTimer += dt;

            HunterNPC sensed = SenseHunter();
            if (sensed != null) hunter = sensed;

            bool targetable = hunter != null && hunter.IsTargetable;
            seesHunter = sensed != null && targetable && Perception.CanSee(Position, transform.forward, sensed.Position, viewDistance, viewAngle);

            if (!targetable && (state != WolfState.Patrol || hasLastKnown)) LoseTarget();
            else if (seesHunter) OnHunterSeen();

            switch (state)
            {
                case WolfState.Patrol: TickPatrol(); break;
                case WolfState.Chase: TickChase(); break;
                case WolfState.GoToLastKnown: TickGoToLastKnown(); break;
                case WolfState.Search: TickSearch(); break;
            }

            if (sensed != null && targetable && AgentMath.FlatDistance(Position, sensed.Position) <= captureRadius)
            {
                sensed.NotifyCaught(); //lo alcanzo
            }

            UpdateFeedback();
        }

        // ---------------- percepción y comunicación ----------------
        private HunterNPC SenseHunter()
        {
            int count = Physics.OverlapSphereNonAlloc(Position, Mathf.Max(viewDistance, captureRadius), overlapBuffer, Perception.AgentsMask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
                if (overlapBuffer[i].TryGetComponent(out HunterNPC found)) return found;
            return null;
        }

        private void OnHunterSeen()
        {
            lastKnownPosition = hunter.Position;
            hasLastKnown = true;

            if (state != WolfState.Chase)
            {
                SetState(WolfState.Chase);
                Howl();
            }
            else if (Time.time - lastHowlTime >= howlCooldown)
            {
                Howl();
            }
        }

        private void Howl() //les pasa a los otros lobos donde esta el cazador
        {
            lastHowlTime = Time.time;

            foreach (WolfAgent other in All)
                if (other != this) other.ReceiveAlert(lastKnownPosition, hunter);
        }

        public void ReceiveAlert(Vector3 position, HunterNPC target)
        {
            alertReceivedTime = Time.time;
            if (state == WolfState.Chase) return; //si ya lo esta viendo ignora el aviso

            hunter = target;
            lastKnownPosition = position;
            hasLastKnown = true;
            SetState(WolfState.GoToLastKnown);
        }

        private void LoseTarget() //el cazador murio o entro a zona segura, vuelve a patrullar
        {
            hasLastKnown = false;
            hunter = null;
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

            if (!path.IsActive) RequestPath(waypoint.position); //no ve el waypoint, usa A*
            MoveTowards(path.NextPoint(Position), patrolSpeed);
        }

        private void TickChase()
        {
            if (seesHunter)
            {
                MoveTowards(hunter.Position, chaseSpeed);
                return;
            }

            SetState(WolfState.GoToLastKnown); //lo perdio de vista, va a la ultima posicion
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
                SetState(WolfState.Search);
                return;
            }

            if (!path.IsActive && !RequestPath(lastKnownPosition))
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
                hasLastKnown = false;
                SetState(WolfState.Patrol);
                return;
            }

            if (!path.IsActive && NavGraph.Instance != null)
            {
                NavNode node = NavGraph.Instance.RandomNodeNear(lastKnownPosition, searchRadius, false);
                if (node != null) RequestPath(node.Position); //busca por nodos random cerca
            }

            MoveTowards(path.NextPoint(Position), patrolSpeed);
        }

        private void AdvanceWaypoint()
        {
            if (loopMode == WaypointLoopMode.Loop)
            {
                waypointIndex = (waypointIndex + 1) % waypoints.Length; //vuelve al primero
                return;
            }

            if (waypoints.Length == 1) return;
            if (waypointIndex + direction >= waypoints.Length || waypointIndex + direction < 0) direction = -direction;
            waypointIndex += direction;
        }

        // ---------------- movimiento ----------------
        private bool RequestPath(Vector3 target) //A* sin pasar por zonas seguras
        {
            return path.SetPath(Position, target, false);
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