using UnityEngine;

namespace IA.Agents
{
    /// <summary>
    /// Feedback visual del boid: material según estado, registro de eventos, etiqueta
    /// IMGUI de depuración y gizmos de los radios de percepción.
    /// </summary>
    public partial class BoidAgent
    {
        // ---------------- feedback ----------------
        private void UpdateVisual()
        {
            if (bodyRenderer == null) return;

            Material material = MaterialForState(state);
            if (material != null) bodyRenderer.sharedMaterial = material;
        }

        private Material MaterialForState(BoidState current)
        {
            switch (current)
            {
                case BoidState.Evading: return evadingMaterial;
                case BoidState.Arriving:
                case BoidState.Interacting: return interactingMaterial;
                case BoidState.Dead: return deadMaterial;
                default: return flockingMaterial;
            }
        }

        private void LogEvent(string message)
        {
            LastEvent = message;
            recentEvents.Add($"[{Time.time:0.0}s] {message}");
            while (recentEvents.Count > 3) recentEvents.RemoveAt(0);
        }

        private void DrawDebugLines()
        {
            if (!showDebugLines) return;

            Vector3 origin = transform.position + Vector3.up * 0.8f;

            if (detectedHunter != null) Debug.DrawLine(origin, detectedHunter.transform.position + Vector3.up, Color.red);
            if (targetInterest != null) Debug.DrawLine(origin, targetInterest.transform.position, Color.green);

            for (int i = 0; i < neighbors.Count; i++)
                Debug.DrawLine(origin, neighbors[i].transform.position + Vector3.up * 0.8f, new Color(0.6f, 0.75f, 1f));
        }

        private void OnGUI()
        {
            if (!showDebugInfo) return;
            label.Draw(transform.position + Vector3.up * 1.9f, BuildLabelText(), TextColorForState(state));
        }

        private string BuildLabelText()
        {
            textBuilder.Length = 0;
            textBuilder.Append("Boid ").Append(agentId).Append("  [").Append(state).Append("]\n");
            textBuilder.Append("vida ").Append(health.ToString("0")).Append('/').Append(maxHealth.ToString("0"));
            textBuilder.Append(" | vecinos ").Append(neighbors.Count);
            textBuilder.Append(" | cadáveres ").Append(deadNeighbors.Count).Append('\n');

            textBuilder.Append("amenaza: ");
            if (detectedHunter != null)
            {
                float distance = AgentMath.FlatDistance(transform.position, detectedHunter.transform.position);
                textBuilder.Append("Cazador (").Append(distance.ToString("0.0")).Append(" m) → Evade");
            }
            else
            {
                textBuilder.Append("ninguna");
            }

            textBuilder.Append("\nobjetivo: ").Append(targetInterest != null ? targetInterest.name + $" ({targetInterest.CurrentHealth:0} vida)" : "ninguno");
            textBuilder.Append("\núlt.: ").Append(LastEvent).Append('\n');

            for (int i = recentEvents.Count - 1; i >= 0; i--) textBuilder.Append(recentEvents[i]).Append('\n');

            return textBuilder.ToString();
        }

        private Color TextColorForState(BoidState current)
        {
            switch (current)
            {
                case BoidState.Evading: return new Color(1f, 0.65f, 0.25f);
                case BoidState.Arriving:
                case BoidState.Interacting: return new Color(0.45f, 1f, 0.55f);
                case BoidState.Dead: return new Color(0.75f, 0.75f, 0.75f);
                case BoidState.Respawning: return new Color(0.6f, 0.6f, 0.95f);
                default: return new Color(0.55f, 0.8f, 1f);
            }
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !showDebugInfo) return;
            Gizmos.color = new Color(1f, 0.35f, 0.35f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, separationRadius);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, separationRadius);

            Gizmos.color = new Color(1f, 0.9f, 0.1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, perceptionRadius);

            Gizmos.color = new Color(1f, 0.2f, 1f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, viewRadius);

            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, interestSightRadius);

            Gizmos.color = new Color(1f, 1f, 1f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, minSeparationDistance);
        }
    }
}
