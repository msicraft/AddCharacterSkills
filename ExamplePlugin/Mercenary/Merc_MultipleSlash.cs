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
    public class Merc_MultipleSlash : BaseSkillState
    {

        private float stopwatch;
        private float frame_count;
        private bool crit;
        private float damageMultiple = AddCharacterSkills.Merc_MultipleSlash_multiple.Value;
        private GameObject hitObject;
        private DamageInfo damageInfo;

        private float distance;

        private Vector3 castingPosition;

        private List<HurtBox> hurtboxList;
        private float characterlevel;
        private float damageFrequency = 10;

        //OnEnter() runs once at the start of the skill
        public override void OnEnter()
        {
            base.OnEnter();

            castingPosition = gameObject.transform.position;

            this.crit = Util.CheckRoll(this.critStat, base.characterBody.master);

            this.CreateBlinkEffect(Util.GetCorePosition(base.gameObject));
            base.PlayAnimation("FullBody, Override", "EvisPrep", "EvisPrep.playbackRate", 0.5f);

            if (NetworkServer.active)
            {
                base.characterBody.AddBuff(RoR2Content.Buffs.HiddenInvincibility);
            }
            
        }

        private HurtBox SearchTarget()
        {
            characterlevel = base.characterBody.level;
            if (characterlevel >= 25)
            {
                distance = 30f;
            } else
            {
                distance = 20f;
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


        public void Casting_Skill()
        {
            SearchTarget();

            characterlevel = base.characterBody.level;
            float scale_damage = 1 + (characterlevel * 0.04f);
            float enemyCount = hurtboxList.Count;
            if (enemyCount > 5)
            {
                if (characterlevel >= 1 && characterlevel <= 9)
                {
                    enemyCount = 5;
                }
                else if (characterlevel >= 10 && characterlevel <= 19)
                {
                    enemyCount = 7;
                }
                else if (characterlevel >= 20 && characterlevel <= 29)
                {
                    enemyCount = 9;
                }
                else if (characterlevel >= 30)
                {
                    enemyCount = 11;
                }
            }
            float final_damage = ((damageStat * damageMultiple) * 0.3f) * scale_damage;
            hitObject = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("prefabs/effects/impacteffects/impactmercevis"), "MultipleSlashHitEffect", true);

            if (hurtboxList.Count > 0)
            {
                for (int a = 0; a < enemyCount; a++)
                {
                    HurtBox hurtBox = hurtboxList[a];
                    if (hurtBox)
                    {
                        int Randomnumber = Random.Range(1, 101);

                        HealthComponent healthComponent = hurtBox.healthComponent;

                        HealthComponent playerHealthComponent = base.healthComponent;

                        Vector3 position = hurtBox.transform.position;
                        Vector2 normalized = UnityEngine.Random.insideUnitCircle.normalized;
                        Vector3 normal = new Vector3(normalized.x, 0f, normalized.y);
                        EffectManager.SimpleImpactEffect(hitObject, position, normal, false);

                        if (NetworkServer.active)
                        {
                            damageInfo = new DamageInfo();
                            damageInfo.damage = final_damage;
                            damageInfo.attacker = gameObject;
                            damageInfo.procCoefficient = 1;
                            damageInfo.position = hurtBox.transform.position;
                            damageInfo.crit = crit;

                            if (Randomnumber < 10)
                            {
                                damageInfo.damageType |= DamageType.BonusToLowHealth | DamageType.BleedOnHit;
                            }
                            else
                            {
                                damageInfo.damageType = DamageType.BonusToLowHealth;
                            }

                            float amount = (damageInfo.damage * 0.1f);

                            healthComponent.TakeDamage(damageInfo);
                            GlobalEventManager.instance.OnHitEnemy(damageInfo, healthComponent.gameObject);
                            GlobalEventManager.instance.OnHitAll(damageInfo, healthComponent.gameObject);

                            playerHealthComponent.AddBarrier(amount);
                        }
                    }
                }
            }
        }

        private void SetPosition(Vector3 newPosition)
        {
            if (base.characterMotor)
            {
                base.characterMotor.Motor.SetPositionAndRotation(newPosition, Quaternion.identity, true);
            }
        }

        private void CreateBlinkEffect(Vector3 origin)
        {
            EffectData effectData = new EffectData();
            effectData.rotation = Util.QuaternionSafeLookRotation(Vector3.up);
            effectData.origin = origin;
            EffectManager.SpawnEffect(Evis.blinkPrefab, effectData, false);
        }


        //FixedUpdate() runs almost every frame of the skill
        //Here, we end the skill once it exceeds its intended duration
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            stopwatch += Time.fixedDeltaTime;
            frame_count += Time.fixedDeltaTime;
            float num = 1f / damageFrequency / (this.attackSpeedStat * 0.75f);
            if (frame_count >= num)
            {
                frame_count -= num;
                Casting_Skill();
            } else if (stopwatch >= 1f)
            {
                outer.SetNextStateToMain();
            }
            if (base.characterMotor)
            {
                base.characterMotor.velocity = Vector3.zero;
            }
            if (stopwatch >= 2f)
            {
                outer.SetNextStateToMain();
            }
        }


        //GetMinimumInterruptPriority() returns the InterruptPriority required to interrupt this skill
        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Death;
        }


        //This method runs once at the end
        public override void OnExit()
        {
            this.CreateBlinkEffect(Util.GetCorePosition(base.gameObject));

            if (NetworkServer.active)
            {
                base.characterBody.RemoveBuff(RoR2Content.Buffs.HiddenInvincibility);

                base.characterBody.AddTimedBuff(RoR2Content.Buffs.HiddenInvincibility, 1f);

            }

            this.SetPosition(castingPosition);

            this.PlayAnimation("FullBody, Override", "EvisLoopExit");

            base.OnExit();
        }

    }
}
