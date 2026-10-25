using UnityEngine;

namespace IA.Agents
{
    /// <summary>
    /// Feedback visual del Cazador: color según estado de la FSM, pulso de golpe,
    /// etiqueta IMGUI de depuración, líneas de depuración y gizmos de rangos.
    /// </summary>
    public partial class HunterNPC
    {
        // ---------------- feedback ----------------
        public void LogAction(string action)
        {
            LastAction = action;
            recentActions.Add(action);
            while (recentActions.Count > 5) recentActions.RemoveAt(0);

            Debug.Log($"[Cazador][{(fsm != null ? fsm.CurrentId.ToString() : "Init")}] {action}", this);
        }

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

            // Pulso al concretar un ataque cuerpo a cuerpo.
            float pulse = meleePulse > 0f ? Mathf.Sin(Mathf.Clamp01(meleePulse / 0.3f) * Mathf.PI) : 0f;
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

        private void OnGUI()
        {
            if (!showDebugInfo) return;
            label.Draw(transform.position + Vector3.up * 2.6f, BuildLabelText(), StateColor(fsm.CurrentId), 270f, 150f, 12);
        }

        private string BuildLabelText()
        {
            textBuilder.Length = 0;
            textBuilder.Append("CAZADOR  [").Append(fsm.CurrentId).Append("]  ").Append(fsm.TimeInState.ToString("0.0")).Append("s\n");
            textBuilder.Append("RECOLECCIONES ").Append(gathers).Append('/').Append(requiredGathers);
            textBuilder.Append(" | zona ").Append(CurrentZone).Append('\n');
            if (visibleWolf != null) textBuilder.Append("¡LOBO DETECTADO: ").Append(visibleWolf.name).Append("!\n");
            if (ChosenSafeZone != null) textBuilder.Append("huye a Zona segura ").Append(ChosenSafeZone.SafeZoneId).Append('\n');
            if (fsm.CurrentId == HunterStateId.Flee && ActionProgress > 0f)
                textBuilder.Append("espera en zona segura: ").Append((ActionProgress * 100f).ToString("0")).Append("%\n");
            if (fsm.CurrentId == HunterStateId.Dead)
                textBuilder.Append("reaparece en ").Append((respawnTime * (1f - ActionProgress)).ToString("0.0")).Append("s\n");
            textBuilder.Append("TBA ").Append(tba.ToString("0.0")).Append("s → ");
            textBuilder.Append(IsAttackReady ? "LISTO" : $"faltan {(tba - attackTimer):0.0}s").Append('\n');
            textBuilder.Append("Range ").Append(rangeAttackRadius.ToString("0.0"));
            textBuilder.Append(" | Melee ").Append(meleeAttackRadius.ToString("0.0"));
            textBuilder.Append(" | visión ").Append(visionRadius.ToString("0.0")).Append('\n');

            textBuilder.Append("objetivo: ");
            if (closestVisibleBoid != null)
            {
                float distance = AgentMath.FlatDistance(transform.position, closestVisibleBoid.transform.position);
                textBuilder.Append(closestVisibleBoid.name).Append(" (").Append(distance.ToString("0.0")).Append(" m, vida ")
                           .Append(closestVisibleBoid.Health.ToString("0")).Append(')');
            }
            else textBuilder.Append("ninguno");

            textBuilder.Append("\ndetectados: ").Append(detectedBoids.Count);
            textBuilder.Append(" | eliminado: ").Append(detectedDeadBoid != null ? detectedDeadBoid.name : "-");
            textBuilder.Append("\nacción: ").Append(LastAction);

            if (fsm.CurrentId == HunterStateId.Gather && ActionProgress > 0f)
                textBuilder.Append("\nrecolección: ").Append((ActionProgress * 100f).ToString("0")).Append('%');

            textBuilder.Append('\n');
            for (int i = recentActions.Count - 1; i >= 0; i--) textBuilder.Append(recentActions[i]).Append('\n');

            return textBuilder.ToString();
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying) DrawRanges();
        }

        private void OnDrawGizmosSelected()
        {
            DrawRanges();
        }

        private void DrawRanges()
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
