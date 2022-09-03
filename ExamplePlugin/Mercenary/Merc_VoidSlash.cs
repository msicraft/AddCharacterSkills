using EntityStates;
using RoR2;
using UnityEngine;
using EntityStates.Merc;
using R2API;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace AddCharacterSkills.Mercenary
{
    public class Merc_VoidSlash : BaseSkillState
    {
        private Vector3 castingPosition;
        private float frame_count;
        private bool crit;
        private float stopwatch;
        private float minTime;

        private GameObject castingEffect;
        private float distance;
        private List<HurtBox> hurtboxList;
        private float characterlevel;
        private GameObject hitObject;
        private float damageMultiple = AddCharacterSkills.Merc_VoidStrike_multiple.Value;
        private DamageInfo damageInfo;

        private float castCount;

        public override void OnEnter()
        {
            base.OnEnter();

            castingPosition = gameObject.transform.position;

            this.crit = Util.CheckRoll(this.critStat, base.characterBody.master);

            this.CreateBlinkEffect(Util.GetCorePosition(base.gameObject));
            base.PlayAnimation("FullBody, Override", "EvisPrep", "EvisPrep.playbackRate", 1.5f);

            if (NetworkServer.active)
            {
                base.characterBody.AddBuff(RoR2Content.Buffs.HiddenInvincibility);
            }
        }

        public void SetCastingEffect()
        {
            Vector3 position = gameObject.transform.position;
            Vector2 normalized = UnityEngine.Random.insideUnitCircle.normalized;
            Vector3 normal = new Vector3(normalized.x, 0f, normalized.y);
            castingEffect = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("prefabs/effects/impvoidspikeexplosion"), "VoidStrikeCastingEffect", true);

            EffectManager.SimpleImpactEffect(castingEffect, position, normal, false);
        }

        private void CreateBlinkEffect(Vector3 origin)
        {
            EffectData effectData = new EffectData();
            effectData.rotation = Util.QuaternionSafeLookRotation(Vector3.up);
            effectData.origin = origin;
            EffectManager.SpawnEffect(Evis.blinkPrefab, effectData, false);
        }

        private void SetPosition(Vector3 newPosition)
        {
            if (base.characterMotor)
            {
                base.characterMotor.Motor.SetPositionAndRotation(newPosition, Quaternion.identity, true);
            }
        }

        private HurtBox SearchTarget()
        {
            characterlevel = base.characterBody.level;
            if (characterlevel >= 30)
            {
                distance = 40f;
            }
            else
            {
                distance = 30f;
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
            return bullseyeSearch.GetResults().FirstOrDefault<HurtBox>();
        }

        public void CastingSkill()
        {
            SearchTarget();

            characterlevel = base.characterBody.level;
            float scale_damage = 1 + (characterlevel * 0.04f);
            float enemyCount = hurtboxList.Count;
            if (enemyCount > 6)
            {
                if (characterlevel >= 1 && characterlevel <= 9)
                {
                    enemyCount = 6;
                } else if (characterlevel >= 10 && characterlevel <= 19)
                {
                    enemyCount = 8;
                } else if (characterlevel >= 20 && characterlevel <= 29)
                {
                    enemyCount = 10;
                } else if (characterlevel >= 30)
                {
                    enemyCount = 15;
                }
            }
            float final_damage = ((damageStat * damageMultiple) * scale_damage);
            hitObject = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("prefabs/effects/impbossblink"), "VoidStrikeEffect", true);

            if (hurtboxList.Count > 0)
            {
                for (int a=0; a<enemyCount; a++)
                {
                    HurtBox hurtBox = hurtboxList[a];
                    if (hurtBox)
                    {
                        HealthComponent enemyHealthComponent = hurtBox.healthComponent;
                        HealthComponent playerHealthComponent = base.healthComponent;
                        float enemyHealth = enemyHealthComponent.fullHealth;

                        Vector3 position = hurtBox.transform.position;
                        Vector2 normalized = UnityEngine.Random.insideUnitCircle.normalized;
                        Vector3 normal = new Vector3(normalized.x, 0f, normalized.y);
                        EffectManager.SimpleImpactEffect(hitObject, position, normal, false);

                        if (NetworkServer.active)
                        {
                            float lastDamage = final_damage + (enemyHealth * 0.01f);
                            float healamount = (lastDamage * 0.05f);
                            ProcChainMask procChainMask = default(ProcChainMask);

                            damageInfo = new DamageInfo();
                            damageInfo.damage = lastDamage;
                            damageInfo.attacker = gameObject;
                            damageInfo.procCoefficient = 1;
                            damageInfo.position = hurtBox.transform.position;
                            damageInfo.crit = crit;
                            damageInfo.damageType = (DamageType.BonusToLowHealth | DamageType.ApplyMercExpose | DamageType.BypassArmor);


                            enemyHealthComponent.body.AddTimedBuff(RoR2Content.Buffs.HealingDisabled, 5f);

                            enemyHealthComponent.TakeDamage(damageInfo);
                            GlobalEventManager.instance.OnHitEnemy(damageInfo, enemyHealthComponent.gameObject);
                            GlobalEventManager.instance.OnHitAll(damageInfo, enemyHealthComponent.gameObject);

                            playerHealthComponent.Heal(healamount, procChainMask, false);
                        }

                        castCount++;

                    }
                }
            }
        }

        public override void FixedUpdate()
        {
            frame_count++;
            base.FixedUpdate();
            stopwatch += Time.fixedDeltaTime;
            minTime = 2f / (attackSpeedStat * 0.5f);
            if (minTime < 1)
            {
                minTime = 1;
            }
            if (minTime > 2)
            {
                minTime = 2;
            }
            if (stopwatch <= minTime && frame_count % 4 == 0)
            {
                SetCastingEffect();
            }
            if (stopwatch >= minTime)
            {
                if (castCount < 1)
                {
                    CastingSkill();
                } else if (castCount > 1)
                {
                    outer.SetNextStateToMain();
                }
            }
            if (stopwatch >= 2.1f)
            {
                outer.SetNextStateToMain();
            }
            if (base.characterMotor)
            {
                base.characterMotor.velocity = Vector3.zero;
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Death;
        }

        public override void OnExit()
        {
            this.CreateBlinkEffect(Util.GetCorePosition(base.gameObject));
            this.SetPosition(castingPosition);
            this.PlayAnimation("FullBody, Override", "EvisLoopExit");

            if (NetworkServer.active)
            {
                base.characterBody.RemoveBuff(RoR2Content.Buffs.HiddenInvincibility);

                base.characterBody.AddTimedBuff(RoR2Content.Buffs.HiddenInvincibility, 1f);
            }
            

            base.OnExit();
        }
    }
}
