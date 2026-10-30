using UnityEngine;

namespace IA.Agents
{
    //interaccion con los objetos de interes y reaccion a los eventos de BoidHealth
    public partial class BoidAgent
    {
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

            bool destroyed = targetInterest.ApplyDamage(interactionDamage);

            if (destroyed)
            {
                targetInterest = null;
            }
        }

        //el cazador sigue llamando a estos dos, pero la logica esta en BoidHealth
        public void TakeDamage(float amount, string source) => life.TakeDamage(amount);

        public bool Harvest() => life.Harvest();

        private void OnDied() //se queda quieto donde murio
        {
            state = BoidState.Dead;
            velocity = Vector3.zero;
            targetInterest = null;
            UpdateVisual();
        }

        private void OnHarvested()
        {
            state = BoidState.Respawning;
            velocity = Vector3.zero;
        }

        private void OnRespawned() //aparece en un lugar random del Coto
        {
            float half = Mathf.Max(2f, arenaHalfExtent - 2f);
            transform.position = AgentMath.RandomPointInSquare(half, groundY);

            velocity = Vector3.zero;
            state = BoidState.Flocking;
            UpdateVisual();
        }
    }
}