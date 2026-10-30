using UnityEngine;

namespace IA.Agents
{

    public partial class BoidAgent
    {
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

        private void DrawDebugLines()
        {
            if (!showDebugLines) return;

            Vector3 origin = transform.position + Vector3.up * 0.8f;

            if (detectedHunter != null) Debug.DrawLine(origin, detectedHunter.transform.position + Vector3.up, Color.red);
            if (targetInterest != null) Debug.DrawLine(origin, targetInterest.transform.position, Color.green);

            for (int i = 0; i < neighbors.Count; i++)
                Debug.DrawLine(origin, neighbors[i].transform.position + Vector3.up * 0.8f, new Color(0.6f, 0.75f, 1f));
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !IsAlive) return;
            Gizmos.color = new Color(1f, 0.35f, 0.35f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, separationRadius);
            Gizmos.color = new Color(1f, 0.9f, 0.1f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, perceptionRadius);
            Gizmos.color = new Color(1f, 0.2f, 1f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, viewRadius);
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
