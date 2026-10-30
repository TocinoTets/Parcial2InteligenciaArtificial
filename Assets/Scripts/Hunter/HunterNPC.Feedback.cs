using UnityEngine;

namespace IA.Agents
{
    //color segun el estado, lineas de debug y gizmos de los rangos
    public partial class HunterNPC
    {
        public Color StateColor(HunterStateId id)
        {
            switch (id)
            {
                case HunterStateId.Attack: return attackColor;
                case HunterStateId.Gather: return gatherColor;
                case HunterStateId.Flee: return fleeColor;
                case HunterStateId.Exit: return exitColor;
                case HunterStateId.Dead: return deadColor;
                default: return searchColor;
            }
        }

        private void UpdateVisual()
        {
            if (bodyRenderer != null)
            {
                Material material = bodyRenderer.sharedMaterial;
                if (material != null && materialProperties != null)
                {
                    int propertyId = material.HasProperty(BaseColorId) ? BaseColorId : ColorId;
                    materialProperties.SetColor(propertyId, StateColor(fsm.CurrentId));
                    bodyRenderer.SetPropertyBlock(materialProperties);
                }
            }

            float pulse = meleePulse > 0f ? Mathf.Sin(Mathf.Clamp01(meleePulse / 0.3f) * Mathf.PI) : 0f; //se agranda un poco cuando pega
            transform.localScale = baseScale * (1f + 0.25f * pulse);
        }

        private void DrawDebugLines()
        {
            if (!showDebugLines) return;

            Vector3 origin = transform.position + Vector3.up;

            if (closestVisibleBoid != null)
                Debug.DrawLine(origin, closestVisibleBoid.transform.position + Vector3.up, attackColor);

            if (detectedDeadBoid != null)
                Debug.DrawLine(origin, detectedDeadBoid.transform.position + Vector3.up, gatherColor);

            if (visibleWolf != null)
                Debug.DrawLine(origin, visibleWolf.Position + Vector3.up, fleeColor);

            if (ChosenSafeZone != null)
                Debug.DrawLine(origin, ChosenSafeZone.Position + Vector3.up * 3f, NavGraph.ZoneColor(Zone.Safe));

            path.DrawDebug(transform.position, StateColor(fsm.CurrentId));

            if (lastMeleeTarget != null && Time.time - lastMeleeTime < 0.35f)
                Debug.DrawLine(origin, lastMeleeTarget.transform.position + Vector3.up, Color.white);
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            DrawRanges();

            for (int i = 0; i < requiredGathers; i++) //una esfera por recoleccion, llena si ya la junto
            {
                Vector3 pos = transform.position + Vector3.up * 2.5f + transform.right * (i - (requiredGathers - 1) * 0.5f) * 0.6f;
                Gizmos.color = i < gathers ? gatherColor : new Color(1f, 1f, 1f, 0.3f);
                if (i < gathers) Gizmos.DrawSphere(pos, 0.2f);
                else Gizmos.DrawWireSphere(pos, 0.2f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            DrawRanges();
        }

        private void DrawRanges() //vision, ataque a distancia y cuerpo a cuerpo
        {
            Gizmos.color = new Color(searchColor.r, searchColor.g, searchColor.b, 0.3f);
            Gizmos.DrawWireSphere(transform.position, visionRadius);

            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, rangeAttackRadius);

            Gizmos.color = new Color(attackColor.r, attackColor.g, attackColor.b, 0.8f);
            Gizmos.DrawWireSphere(transform.position, meleeAttackRadius);
        }
    }
}