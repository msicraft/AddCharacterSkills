using EntityStates;
using RoR2;
using UnityEngine;
using R2API;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace AddCharacterSkills.Acrid
{

    public class Acrid_EmergencyEscape : BaseSkillState
    {
        private Vector3 moveVector;
        private Vector3 jumpVector;
        private float YVector;
        private float stopwatch;
        private int castCount;
        private float distance;
        private List<HurtBox> hurtboxList;
        private float characterlevel;
        private DamageInfo damageInfo;
        private GameObject castingEffect;
        private float frameCount;

        public override void OnEnter()
        {
            base.OnEnter();

            if (NetworkServer.active)
            {
                characterBody.AddBuff(RoR2Content.Buffs.HiddenInvincibility);
                characterBody.AddBuff(RoR2Content.Buffs.Cloak);
            }

            if (base.characterMotor)
            {
                base.characterMotor.velocity = Vector3.zero;
            }

            CastingSkill_Stun();

        }

        public void CastingSkill_Jump()
        {
            YVector = 5 + (moveSpeedStat * 0.75f);
            if (YVector > 20)
            {
                YVector = 20;
            }
            moveVector = new Vector3(0, YVector, 0);
            jumpVector = characterMotor.rootMotion + moveVector;
            if (castCount < 1)
            {
                Vector3 test = new Vector3(10, 0, 0);
                base.characterMotor.rootMotion = jumpVector;

                castCount++;
            }
        }

        public void CastingSkill_Stun()
        {
            SearchTarget();

            characterlevel = characterBody.level;
            float enemyCount = hurtboxList.Count;
            if (enemyCount > 10)
            {
                if (characterlevel >= 25)
                {
                    enemyCount = 15;
                } else
                {
                    enemyCount = 10;
                }
            }
            if (hurtboxList.Count > 0)
            {
                for (int a=0; a<enemyCount; a++)
                {
                    HurtBox hurtBox = hurtboxList[a];
                    if (hurtBox)
                    {
                        HealthComponent enemyHealthComponent = hurtBox.healthComponent;

                        if (NetworkServer.active)
                        {
                            damageInfo = new DamageInfo();
                            damageInfo.damage = 1;
                            damageInfo.attacker = gameObject;
                            damageInfo.procCoefficient = 1;
                            damageInfo.position = hurtBox.transform.position;
                            damageInfo.crit = false;
                            damageInfo.damageType = DamageType.Stun1s;

                            enemyHealthComponent.TakeDamage(damageInfo);
                            GlobalEventManager.instance.OnHitEnemy(damageInfo, enemyHealthComponent.gameObject);
                            GlobalEventManager.instance.OnHitAll(damageInfo, enemyHealthComponent.gameObject);
                        }
                    }
                }
            }
        }

        public void CreateJumpEffect()
        {
            castingEffect = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("prefabs/effects/boostjumpeffect"), "EmergencyEscapeJumpEffect", true);

            Vector3 position = gameObject.transform.position;
            Vector2 normalized = UnityEngine.Random.insideUnitCircle.normalized;
            Vector3 normal = new Vector3(normalized.x, 0f, normalized.y);
            
            EffectManager.SimpleImpactEffect(castingEffect, position, normal, false);
        }

        private void SearchTarget()
        {
            characterlevel = base.characterBody.level;
            if (characterlevel >= 25)
            {
                distance = 35f;
            }
            else
            {
                distance = 25f;
            }
            Ray aimRay = base.GetAimRay();
            BullseyeSearch bullseyeSearch = new BullseyeSearch();
            bullseyeSearch.searchOrigin = aimRay.origin;
            bullseyeSearch.searchDirection = aimRay.direction;
            bullseyeSearch.maxDistanceFilter = distance;
            bullseyeSearch.teamMaskFilter = TeamMask.GetUnprotectedTeams(base.GetTeam());
            bullseyeSearch.sortMode = BullseyeSearch.SortMode.Distance;
            bullseyeSearch.RefreshCandidates();
            bullseyeSearch.FilterOutGameObject(base.gameObject);
            hurtboxList = bullseyeSearch.GetResults().ToList();
        }

        public override void FixedUpdate()
        {
            frameCount++;
            base.FixedUpdate();
            stopwatch += Time.fixedDeltaTime;
            if (stopwatch <= 0.3f)
            {
                if (frameCount % 2 == 0)
                {
                    CreateJumpEffect();
                }
                CastingSkill_Jump();
            }
            if (stopwatch >= 0.5f)
            {
                outer.SetNextStateToMain();
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Death;
        }

        public override void OnExit()
        {
            if (NetworkServer.active)
            {
                characterBody.RemoveBuff(RoR2Content.Buffs.HiddenInvincibility);
                characterBody.RemoveBuff(RoR2Content.Buffs.Cloak);

                characterBody.AddTimedBuff(RoR2Content.Buffs.HiddenInvincibility, 0.5f);
                characterBody.AddTimedBuff(RoR2Content.Buffs.CloakSpeed, 1.5f);
            }

            base.OnExit();
        }
    }
}
