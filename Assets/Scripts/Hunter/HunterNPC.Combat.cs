using UnityEngine;

namespace IA.Agents
{
    /// <summary>
    /// Ataques del Cazador. Los estados de la FSM deciden cuándo llamarlos y aplican el
    /// TBA; aquí sólo se ejecuta el golpe cuerpo a cuerpo o el proyectil a distancia.
    /// </summary>
    public partial class HunterNPC
    {
        // ---------------- ataques ----------------
        public void ResetAttackTimer()
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
            LogAction($"Attack: MELEE a {target.name} (-{meleeDamage:0} vida)");
        }

        public void PerformRangedAttack(BoidAgent target)
        {
            if (target == null) return;

            if (projectilePrefab == null)
            {
                target.TakeDamage(rangedDamage, "Cazador: a distancia");
                LogAction($"Attack: A DISTANCIA a {target.name} (-{rangedDamage:0} vida, sin prefab de proyectil)");
                return;
            }

            Vector3 origin = transform.position + Vector3.up * 0.6f + transform.forward * 0.9f;
            Vector3 direction = AgentMath.Flat(target.transform.position - origin);
            if (direction.sqrMagnitude < 1e-4f) direction = transform.forward;
            direction.Normalize();

            GameObject projectile = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(direction));
            HunterProjectile component = projectile.GetComponent<HunterProjectile>();
            if (component != null) component.Initialize(target, projectileSpeed, rangedDamage);

            LogAction($"Attack: A DISTANCIA a {target.name} (proyectil, -{rangedDamage:0} vida al impactar)");
        }
    }
}
