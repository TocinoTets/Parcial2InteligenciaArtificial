using UnityEngine;

namespace IA.Agents
{
    /// <summary>
    /// Feedback del lobo: color según estado, etiqueta (detección, aullido, alerta recibida,
    /// última posición conocida), líneas de depuración del camino A* y gizmo del campo de visión.
    /// </summary>
    public partial class WolfAgent
    {
        private const float FlashDuration = 1.5f;

        [Header("Feedback visual")]
        [SerializeField] private bool showDebugInfo = false;
        [SerializeField] private Color patrolColor = new Color(0.45f, 0.4f, 0.35f);
        [SerializeField] private Color chaseColor = new Color(1f, 0.1f, 0.1f);
        [SerializeField] private Color goToLastKnownColor = new Color(1f, 0.5f, 0.1f);
        [SerializeField] private Color searchColor = new Color(0.9f, 0.75f, 0.2f);

        private readonly AgentLabel label = new AgentLabel();
        private Renderer bodyRenderer;
        private MaterialPropertyBlock materialProperties;
        private float lastHowlTime = -100f;
        private float alertReceivedTime = -100f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private void InitFeedback()
        {
            bodyRenderer = GetComponentInChildren<Renderer>();
            materialProperties = new MaterialPropertyBlock();
        }

        private void Log(string message)
        {
            Debug.Log($"[{name}][{state}] {message}", this);
        }

        private Color StateColor()
        {
            switch (state)
            {
                case WolfState.Chase: return chaseColor;
                case WolfState.GoToLastKnown: return goToLastKnownColor;
                case WolfState.Search: return searchColor;
                default: return patrolColor;
            }
        }

        private void UpdateFeedback()
        {
            if (bodyRenderer != null && bodyRenderer.sharedMaterial != null)
            {
                int propertyId = bodyRenderer.sharedMaterial.HasProperty(BaseColorId) ? BaseColorId : ColorId;
                materialProperties.SetColor(propertyId, StateColor());
                bodyRenderer.SetPropertyBlock(materialProperties);
            }

            Vector3 origin = Position + Vector3.up;
            if (seesHunter) Debug.DrawLine(origin, hunter.Position + Vector3.up, chaseColor);
            if (hasLastKnown) Debug.DrawLine(origin, lastKnownPosition + Vector3.up * 0.2f, goToLastKnownColor);
            path.DrawDebug(Position, StateColor());

            if (Time.time - lastHowlTime < FlashDuration) DrawCircle(Position, captureRadius + (Time.time - lastHowlTime) * 10f, chaseColor);
        }

        private static void DrawCircle(Vector3 center, float radius, Color color)
        {
            const int segments = 24;
            Vector3 previous = center + new Vector3(radius, 0.2f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                Vector3 next = center + new Vector3(Mathf.Cos(angle) * radius, 0.2f, Mathf.Sin(angle) * radius);
                Debug.DrawLine(previous, next, color);
                previous = next;
            }
        }

        private void OnGUI()
        {
            if (!showDebugInfo) return;

            string text = $"{name} [{state}]";
            if (seesHunter) text += "\n¡VE AL CAZADOR!";
            if (Time.time - lastHowlTime < FlashDuration) text += "\n¡AUUU! (alerta emitida)";
            if (Time.time - alertReceivedTime < FlashDuration) text += "\nalerta recibida";
            if (hasLastKnown) text += $"\núltima pos: ({lastKnownPosition.x:0.0}, {lastKnownPosition.z:0.0})";

            label.Draw(transform.position + Vector3.up * 2.2f, text, StateColor(), 200f, 80f, 11);
        }

        private void OnDrawGizmos()
        {
            // Campo de visión: distancia máxima + ángulo.
            Gizmos.color = seesHunter ? chaseColor : new Color(1f, 1f, 1f, 0.5f);
            Vector3 origin = transform.position + Vector3.up * 0.2f;
            Vector3 forward = AgentMath.Flat(transform.forward).normalized;
            float half = viewAngle * 0.5f;
            const int segments = 16;

            Vector3 previous = origin + Quaternion.Euler(0f, -half, 0f) * forward * viewDistance;
            Gizmos.DrawLine(origin, previous);
            for (int i = 1; i <= segments; i++)
            {
                Vector3 next = origin + Quaternion.Euler(0f, -half + viewAngle * i / segments, 0f) * forward * viewDistance;
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
            Gizmos.DrawLine(origin, previous);

            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, captureRadius);

            if (!hasLastKnown) return;
            Gizmos.color = goToLastKnownColor;
            Gizmos.DrawWireSphere(lastKnownPosition, 0.8f);
        }
    }
}
