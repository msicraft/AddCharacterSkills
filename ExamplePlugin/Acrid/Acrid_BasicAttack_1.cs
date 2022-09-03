using EntityStates;
using RoR2;
using UnityEngine;
using R2API;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Networking;
using RoR2.Projectile;

namespace AddCharacterSkills.Acrid
{
    public class Acrid_BasicAttack_1 : BaseSkillState
    {
        private bool crit;

        public override void OnEnter()
        {
            base.OnEnter();

            this.crit = Util.CheckRoll(this.critStat, base.characterBody.master);
        }

        public void SetBulletAttack()
        {
            Ray ray = GetAimRay();

            BulletAttack bulletAttack = new BulletAttack();
            bulletAttack.owner = base.gameObject;
            bulletAttack.weapon = base.gameObject;
            bulletAttack.origin = ray.origin;
            bulletAttack.aimVector = ray.direction;
            bulletAttack.minSpread = 0f;
            bulletAttack.maxSpread = characterBody.spreadBloomAngle;
            bulletAttack.damage = damageStat * 1f;
            bulletAttack.isCrit = crit;
            bulletAttack.radius = 0.1f;
            bulletAttack.smartCollision = true;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }

        public override void OnExit()
        {
            base.OnExit();
        }
    }
}
