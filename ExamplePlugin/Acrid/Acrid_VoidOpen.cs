using EntityStates;
using RoR2;
using UnityEngine;
using R2API;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace AddCharacterSkills.Acrid
{
    public class Acrid_VoidOpen : BaseSkillState
    {
        private List<HurtBox> hurtboxList;
        private GameObject hitObject;
        private DamageInfo damageInfo;
        private float damageMultiple = AddCharacterSkills.Acrid_VoidOpen_Multiple.Value;
        private int frame_count;

        private float distance;
        private float characterlevel;
        private bool crit;

        public override void OnEnter()
        {
            base.OnEnter();

            this.crit = Util.CheckRoll(this.critStat, base.characterBody.master);

            if (NetworkServer.active)
            {
                base.characterBody.AddTimedBuff(RoR2Content.Buffs.Cloak, 1.5f);
            }
        }

        private void SearchTarget()
        {
            characterlevel = base.characterBody.level;

            Ray aimRay = base.GetAimRay();

            if (characterlevel >= 25)
            {
                distance = 35f;
            } else
            {
                distance = 25f;
            }

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

        public void CastingSkill()
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
                else if (characterlevel >= 20 && characterlevel <= 24)
                {
                    enemyCount = 8;
                }
                else if (characterlevel >= 25)
                {
                    enemyCount = 12;
                }
            }
            
            float final_damage = (damageStat * damageMultiple) * scale_damage;
            hitObject = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("prefabs/effects/nullifierdeathexplosion"), "VoidEffect", true);

            if (hurtboxList.Count > 0)
            {
                for (int a=0; a<enemyCount; a++)
                {
                    HurtBox hurtBox = hurtboxList[a];
                    if (hurtBox)
                    {
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
                            damageInfo.damageType = (DamageType.BonusToLowHealth | DamageType.BypassArmor);
                            damageInfo.crit = crit;

                            float healamount = (final_damage * 0.1f);
                            ProcChainMask procChainMask = default(ProcChainMask);

                            healthComponent.body.AddTimedBuff(RoR2Content.Buffs.HealingDisabled, 5f);

                            healthComponent.TakeDamage(damageInfo);
                            GlobalEventManager.instance.OnHitEnemy(damageInfo, healthComponent.gameObject);
                            GlobalEventManager.instance.OnHitAll(damageInfo, healthComponent.gameObject);

                            playerHealthComponent.Heal(healamount, procChainMask, false);
                        }

                    }
                }
            } 
        }

        public override void FixedUpdate()
        {
            frame_count++;
            base.FixedUpdate();
            if (frame_count % 2 == 0)
            {
                CastingSkill();
            }
            if (frame_count >=3)
            {
                outer.SetNextStateToMain();
                return;
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Death;
        }

        public override void OnExit()
        {
            base.OnExit();
        }

    }

}
