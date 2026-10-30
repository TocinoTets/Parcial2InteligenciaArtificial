using UnityEngine;

namespace IA.Agents
{
    //color segun el estado, lineas de debug y gizmo del campo de vision del lobo
    public partial class WolfAgent
    {
        private const float FlashDuration = 1.5f;

        [Header("Feedback visual")]
        [SerializeField] private Color patrolColor = new Color(0.45f, 0.4f, 0.35f);
        [SerializeField] private Color chaseColor = new Color(1f, 0.1f, 0.1f);
        [SerializeField] private Color goToLastKnownColor = new Color(1f, 0.5f, 0.1f);
        [SerializeField] private Color searchColor = new Color(0.9f, 0.75f, 0.2f);

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

            if (Time.time - lastHowlTime < FlashDuration) DrawCircle(Position, captureRadius + (Time.time - lastHowlTime) * 10f, chaseColor); //circulo que se agranda cuando aulla
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

        private void OnDrawGizmos()
        {
            //campo de vision: distancia + angulo
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