using UnityEngine;

namespace IA.Agents
{
    /// <summary>
    /// Ciclo de vida del boid: interacción con objetos de interés, daño, muerte in situ,
    /// recolección por parte del Cazador y reaparición aleatoria.
    /// </summary>
    public partial class BoidAgent
    {
        // ---------------- interacción con objetos de interés ----------------
        private void TickInteraction(float dt)
        {
            if (state != BoidState.Interacting || targetInterest == null)
            {
                interactionTimer = 0f;
                return;
            }

            interactionTimer += dt;
            if (interactionTimer < interactionInterval) return;

            interactionTimer = 0f;

            string targetName = targetInterest.name;
            bool destroyed = targetInterest.ApplyDamage(interactionDamage);
            LogEvent($"interactúa con {targetName} (-{interactionDamage:0} de vida)");

            if (destroyed)
            {
                LogEvent($"{targetName} destruido");
                targetInterest = null;
            }
        }

        // ---------------- vida / muerte / recolección ----------------
        public void TakeDamage(float amount, string source)
        {
            if (!IsAlive) return;

            health = Mathf.Max(0f, health - Mathf.Max(0f, amount));
            LogEvent($"recibe {amount:0} de daño ({source}) → vida {health:0}");

            if (health <= 0f) Die();
        }

        private void Die()
        {
            state = BoidState.Dead;
            velocity = Vector3.zero;
            targetInterest = null;
            LogEvent("vida 0 → queda inactivo en el lugar donde fue eliminado");
            UpdateVisual();
        }

        /// <summary>Recolección por parte del Cazador: desaparece del escenario.</summary>
        public bool Harvest()
        {
            if (state != BoidState.Dead) return false;

            state = BoidState.Respawning;
            respawnTimer = 0f;
            velocity = Vector3.zero;
            SetVisible(false);
            LogEvent($"recolectado → desaparece y reaparece en {respawnDelay:0.#}s");
            return true;
        }

        private void TickInactive(float dt)
        {
            if (state != BoidState.Respawning) return; // Dead: agente inactivo, sin desplazamiento

            respawnTimer += dt;
            if (respawnTimer >= respawnDelay) Respawn();
        }

        private void Respawn()
        {
            float half = Mathf.Max(2f, arenaHalfExtent - 2f);
            transform.position = AgentMath.RandomPointInSquare(half, groundY);

            health = maxHealth;
            velocity = Vector3.zero;
            state = BoidState.Flocking;
            SetVisible(true);
            UpdateVisual();
            LogEvent("reaparece en una ubicación aleatoria del escenario");
        }

        private void SetVisible(bool visible)
        {
            if (bodyRenderer != null) bodyRenderer.enabled = visible;
            if (bodyCollider != null) bodyCollider.enabled = visible;
        }
    }
}
