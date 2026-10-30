using System;
using UnityEngine;

namespace IA.Agents
{
    //vida, muerte, recoleccion y respawn del boid
    //avisa con eventos y el BoidAgent reacciona (cambia de estado, color y posicion)
    public class BoidHealth : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 50f;
        [SerializeField] private float respawnDelay = 6f; //tiempo hasta reaparecer despues de ser recolectado

        private Renderer bodyRenderer;
        private Collider bodyCollider;
        private float respawnTimer;

        public float Health { get; private set; }
        public float MaxHealth => maxHealth;
        public bool IsDead { get; private set; }
        public bool IsHarvested { get; private set; }

        public event Action Died; //vida en 0, queda quieto en el lugar
        public event Action Harvested; //el cazador lo recolecto, desaparece
        public event Action Respawned; //vuelve a aparecer con la vida llena

        private void Awake()
        {
            bodyRenderer = GetComponentInChildren<Renderer>();
            bodyCollider = GetComponent<Collider>();
            Health = maxHealth;
        }

        private void Update()
        {
            if (!IsHarvested) return;

            respawnTimer += Time.deltaTime;
            if (respawnTimer >= respawnDelay) Respawn();
        }

        public void TakeDamage(float amount)
        {
            if (IsDead) return;

            Health = Mathf.Max(0f, Health - Mathf.Max(0f, amount));
            if (Health > 0f) return;

            IsDead = true;
            Died?.Invoke();
        }

        public bool Harvest()
        {
            if (!IsDead || IsHarvested) return false; //solo se puede recolectar si esta muerto

            IsHarvested = true;
            respawnTimer = 0f;
            SetVisible(false);
            Harvested?.Invoke();
            return true;
        }

        private void Respawn()
        {
            Health = maxHealth;
            IsDead = false;
            IsHarvested = false;
            SetVisible(true);
            Respawned?.Invoke(); //el BoidAgent lo mueve a un lugar random del Coto
        }

        private void SetVisible(bool visible)
        {
            if (bodyRenderer != null) bodyRenderer.enabled = visible;
            if (bodyCollider != null) bodyCollider.enabled = visible;
        }
    }
}