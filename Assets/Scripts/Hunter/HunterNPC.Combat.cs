using UnityEngine;

namespace IA.Agents
{
    //ataques del cazador, la FSM decide cuando atacar
    public partial class HunterNPC
    {
        public void ResetAttackTimer() //reinicia el TBA
        {
            attackTimer = 0f;
        }

        public void PerformMeleeAttack(BoidAgent target)
        {
            if (target == null) return;

            meleePulse = 0.3f;
            lastMeleeTarget = target;
            lastMeleeTime = Time.time;

            target.TakeDamage(meleeDamage, "Cazador: cuerpo a cuerpo");
        }

        public void PerformRangedAttack(BoidAgent target)
        {
            if (target == null) return;

            if (projectilePrefab == null) //si no hay prefab le hace el daño directo
            {
                target.TakeDamage(rangedDamage, "Cazador: a distancia");
                return;
            }

            Vector3 origin = transform.position + Vector3.up * 0.6f + transform.forward * 0.9f;
            Vector3 direction = AgentMath.Flat(target.transform.position - origin);
            if (direction.sqrMagnitude < 1e-4f) direction = transform.forward;
            direction.Normalize();

            GameObject projectile = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(direction));
            HunterProjectile component = projectile.GetComponent<HunterProjectile>();
            if (component != null) component.Initialize(target, projectileSpeed, rangedDamage); //el daño se aplica cuando impacta
        }
    }
}