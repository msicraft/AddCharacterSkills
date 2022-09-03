using BepInEx;
using R2API;
using R2API.Networking;
using R2API.Utils;
using AddCharacterSkills.Mercenary;
using AddCharacterSkills.Acrid;
using BepInEx.Configuration;
using System;
using UnityEngine;
using RoR2.Skills;
using RoR2;
using EntityStates;
using BepInEx.Logging;

namespace AddCharacterSkills
{
    //This is an example plugin that can be put in BepInEx/plugins/ExamplePlugin/ExamplePlugin.dll to test out.
    //It's a small plugin that adds a relatively simple item to the game, and gives you that item whenever you press F2.

    //This attribute specifies that we have a dependency on R2API, as we're using it to add our item to the game.
    //You don't need this if you're not using R2API in your plugin, it's just to tell BepInEx to initialize R2API before this plugin so it's safe to use R2API.
    [BepInDependency(R2API.R2API.PluginGUID)]

    //This attribute is required, and lists metadata for your plugin.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    //We will be using 2 modules from R2API: ItemAPI to add our item and LanguageAPI to add our language tokens.
    [R2APISubmoduleDependency(nameof(LanguageAPI), nameof(PrefabAPI), nameof(DamageAPI) ,nameof(R2API), nameof(NetworkingAPI))]

    //This is the main declaration of our plugin class. BepInEx searches for all classes inheriting from BaseUnityPlugin to initialize on startup.
    //BaseUnityPlugin itself inherits from MonoBehaviour, so you can use this as a reference for what you can declare and use in your plugin class: https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
    public class AddCharacterSkills : BaseUnityPlugin
    {

        //The Plugin GUID should be a unique ID for this plugin, which is human readable (as it is used in places like the config).
        //If we see this PluginGUID as it is on thunderstore, we will deprecate this mod. Change the PluginAuthor and the PluginName !
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "msicraft";
        public const string PluginName = "AddCharacterSkills";
        public const string PluginVersion = "1.0.0";

        private ManualLogSource getLog;

        //The Awake() method is run at the very start when the game is initialized.
        public void Awake()
        {
            NetworkingAPI.RegisterMessageType<NetworkInterface>();

            getLog = Logger;

            Merc_Setup();
            Acrid_Setup();

            Logger.LogInfo("AddCharacterSkills Enable");
        }

        //The Update() method is run on every frame of the game.
        private void Update()
        {
        }

        public void Hooks()
        {

        }

        //https://github.com/xiaoxiao921/GithubActionCacheTest/blob/b7114ba076e4f5e127a0a8f68f0ce9a00c653be8/assetPathsDump.html
        //-------------------------
        //Mercenary Section 
        private GameObject MercObject;
        public static ConfigEntry<bool> Merc_MultipleSlash_check { get; set; }
        public static ConfigEntry<float> Merc_MultipleSlash_multiple { get; set; }
        public static ConfigEntry<float> Merc_MultipleSlash_cooldown { get; set; }
        public static ConfigEntry<bool> Merc_VoidStrike_check { get; set; }
        public static ConfigEntry<float> Merc_VoidStrike_multiple { get; set; }
        public static ConfigEntry<float> Merc_VoidStrike_cooldown { get; set; }

        public void Merc_Setup()
        {
            set_Merc_Config();

            bool check_Merc_PowerSlash = Merc_MultipleSlash_check.Value;
            bool check_Merc_VoidStrike = Merc_VoidStrike_check.Value;

            if (check_Merc_PowerSlash)
            {
                Set_MercMultipleSlash();
                Logger.LogInfo("Mercenary Special MultipleSlash Enable");
            }
            else { Logger.LogInfo("Mercenary Special MultipleSlash Disable");}
            if (check_Merc_VoidStrike)
            {
                Set_Merc_VoidStrike();
                Logger.LogInfo("Mercenary Special Void Strike Enable");
            } else{ Logger.LogInfo("Mercenary Special Void Strike Disable");}
        }
        public void set_Merc_Config()
        {
            Merc_MultipleSlash_check = Config.Bind<bool>("Merc.MultipleSlash", "MultipleSlash Enable", true, "Skill Enable Setting");
            Merc_MultipleSlash_multiple = Config.Bind<float>("Merc.MultipleSlash", "Multiple", 1.1f,"Skill Damage multiple");
            Merc_MultipleSlash_cooldown = Config.Bind<float>("Merc.MultipleSlash", "Cooldown", 10, "Skill Cooldown");

            Merc_VoidStrike_check = Config.Bind<bool>("Merc.Void High-Speed Slash", "Void High-Speed Slash Enable", true, "Skill Enable Setting");
            Merc_VoidStrike_multiple = Config.Bind<float>("Merc.Void High-Speed Slash", "Multiple", 5.5f, "Skill Damage multiple");
            Merc_VoidStrike_cooldown = Config.Bind<float>("Merc.Void High-Speed Slash", "Cooldown", 15, "Skill Cooldown");

        }

        public void Set_MercMultipleSlash()
        {
            MercObject = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("prefabs/characterbodies/mercbody"), "MercBody", true);

            float multiple = Merc_MultipleSlash_multiple.Value;

            //Resources.Load<GameObject>("prefabs/characterbodies/mercbody")
            //Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Merc/MercBody.prefab").WaitForCompletion();

            //We use LanguageAPI to add strings to the game, in the form of tokens
            LanguageAPI.Add("MERCENARY_SPECIAL_POWERSLASH_NAME", "Multiple Slash");
            LanguageAPI.Add("MERCENARY_SPECIAL_POWERSLASH_DESCRIPTION", $"<b><style=cIsHealth>[Slayer]</b><color=#FFFFFF>Cleaves nearby enemies very quickly several times."+ "\n" +
                "Damage, range, and number of targets increase according to level. (<b>Max at level 30 or higher</b>)</color>" + "\n" +
                "<style=cIsDamage>Attack Damage: Damage x " + multiple + " x 0.3 x LevelScale\n" +
                "Number of Target: 5 + LevelScale<color=#FFFFFF>");

            //Now we must create a SkillDef
            SkillDef Merc_MultipleSlash = ScriptableObject.CreateInstance<SkillDef>();

            //Check step 2 for the code of the CustomSkillsTutorial.MyEntityStates.SimpleBulletAttack class
            Merc_MultipleSlash.activationState = new SerializableEntityStateType(typeof(Merc_MultipleSlash));
            Merc_MultipleSlash.activationStateMachineName = "Body";
            Merc_MultipleSlash.baseMaxStock = 1;
            Merc_MultipleSlash.baseRechargeInterval = Merc_MultipleSlash_cooldown.Value;
            Merc_MultipleSlash.beginSkillCooldownOnSkillEnd = false;
            Merc_MultipleSlash.canceledFromSprinting = false;
            Merc_MultipleSlash.cancelSprintingOnActivation = false;
            Merc_MultipleSlash.fullRestockOnAssign = true;
            Merc_MultipleSlash.interruptPriority = InterruptPriority.Skill;
            Merc_MultipleSlash.isCombatSkill = true;
            Merc_MultipleSlash.mustKeyPress = false;
            Merc_MultipleSlash.rechargeStock = 1;
            Merc_MultipleSlash.requiredStock = 1;
            Merc_MultipleSlash.stockToConsume = 1;
            //For the skill icon, you will have to load a Sprite from your own AssetBundle
            Merc_MultipleSlash.icon = null;
            Merc_MultipleSlash.skillDescriptionToken = "MERCENARY_SPECIAL_POWERSLASH_DESCRIPTION";
            Merc_MultipleSlash.skillName = "MERCENARY_SPECIAL_POWERSLASH_NAME";
            Merc_MultipleSlash.skillNameToken = "MERCENARY_SPECIAL_POWERSLASH_NAME";

            ContentAddition.AddSkillDef(Merc_MultipleSlash);

            SkillLocator skillLocator = MercObject.GetComponent<SkillLocator>();
            SkillFamily skillFamily = skillLocator.special.skillFamily;

            Array.Resize(ref skillFamily.variants, skillFamily.variants.Length + 1);
            skillFamily.variants[skillFamily.variants.Length - 1] = new SkillFamily.Variant
            {
                skillDef = Merc_MultipleSlash,
                unlockableName = "",
                viewableNode = new ViewablesCatalog.Node(Merc_MultipleSlash.skillNameToken, false, null)
            };

            ContentAddition.AddEntityState<Merc_MultipleSlash>(out _);
        }

        public void Set_Merc_VoidStrike()
        {
            float multiple = Merc_VoidStrike_multiple.Value;
            MercObject = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("prefabs/characterbodies/mercbody"), "MercBody", true);

            LanguageAPI.Add("MERCENARY_SPECIAL_VOIDSTRIKE_NAME", "Void High-Speed Slash");
            LanguageAPI.Add("MERCENARY_SPECIAL_VOIDSTRIKE_DESCRIPTION", $"<b><style=cIsHealth>[Slayer]</b><color=#FFFFFF>Use the power of the void to slash nearby enemies, dealing massive unavoidable damage." + "\n" +
                $"Heal 5% of damage per target" + "\n" +
                "Skill upgrade at level 30 or higher(Increased range and number of targets) </color>" + "\n" + 
                "<style=cIsDamage>Attack Damage: Damage x " + multiple + " x LevelScale<color=#FFFFFF>\n");

            SkillDef Merc_VoidSlash = ScriptableObject.CreateInstance<SkillDef>();
            Merc_VoidSlash.activationState = new SerializableEntityStateType(typeof(Merc_VoidSlash));
            Merc_VoidSlash.activationStateMachineName = "Body";
            Merc_VoidSlash.baseMaxStock = 1;
            Merc_VoidSlash.baseRechargeInterval = Merc_VoidStrike_cooldown.Value;
            Merc_VoidSlash.beginSkillCooldownOnSkillEnd = false;
            Merc_VoidSlash.canceledFromSprinting = false;
            Merc_VoidSlash.cancelSprintingOnActivation = false;
            Merc_VoidSlash.fullRestockOnAssign = true;
            Merc_VoidSlash.interruptPriority = InterruptPriority.Skill;
            Merc_VoidSlash.isCombatSkill = true;
            Merc_VoidSlash.mustKeyPress = false;
            Merc_VoidSlash.rechargeStock = 1;
            Merc_VoidSlash.requiredStock = 1;
            Merc_VoidSlash.stockToConsume = 1;
            //For the skill icon, you will have to load a Sprite from your own AssetBundle
            Merc_VoidSlash.icon = null;
            Merc_VoidSlash.skillDescriptionToken = "MERCENARY_SPECIAL_VOIDSTRIKE_DESCRIPTION";
            Merc_VoidSlash.skillName = "MERCENARY_SPECIAL_VOIDSTRIKE_NAME";
            Merc_VoidSlash.skillNameToken = "MERCENARY_SPECIAL_VOIDSTRIKE_NAME";

            ContentAddition.AddSkillDef(Merc_VoidSlash);

            SkillLocator skillLocator = MercObject.GetComponent<SkillLocator>();
            SkillFamily skillFamily = skillLocator.special.skillFamily;

            Array.Resize(ref skillFamily.variants, skillFamily.variants.Length + 1);
            skillFamily.variants[skillFamily.variants.Length - 1] = new SkillFamily.Variant
            {
                skillDef = Merc_VoidSlash,
                unlockableName = "",
                viewableNode = new ViewablesCatalog.Node(Merc_VoidSlash.skillNameToken, false, null)
            };

            ContentAddition.AddEntityState<Merc_VoidSlash>(out _);

        }
        //---------------------

        //Acrid Section
        private GameObject AcridObject;

        public static ConfigEntry<bool> Acrid_VoidOpen_Check { get; set; }
        public static ConfigEntry<float> Acrid_VoidOpen_Multiple { get; set; }
        public static ConfigEntry<float> Acrid_VoidOpen_Cooldown { get; set; }
        public static ConfigEntry<bool> Acrid_EmergencyEscape_Check { get; set; }
        public static ConfigEntry<float> Acrid_EmergencyEscape_Cooldown { get; set; }
        public static ConfigEntry<bool> Acrid_BasicAttack_1_Check { get; set; }

        public void Acrid_Setup()
        {
            Set_AcridConfig();

            bool check_acrid_VoidOpen = Acrid_VoidOpen_Check.Value;
            bool check_acrid_EmergencyEscape = Acrid_EmergencyEscape_Check.Value;
            bool check_acrid_BasicAttack1 = Acrid_BasicAttack_1_Check.Value;

            if (check_acrid_VoidOpen)
            {
                Set_Acrid_VoidOpen();
                Logger.LogInfo("Acrid Special VoidOpen Enable");
            } else{ Logger.LogInfo("Acrid Special VoidOpen Disable");}
            if (check_acrid_EmergencyEscape)
            {
                Set_Acrid_EmergencyEscape();
                Logger.LogInfo("Acrid Utility EmergencyEscape Enable");
            } else { Logger.LogInfo("Acrid Utility EmergencyEscape Disable"); }
            if (check_acrid_BasicAttack1)
            {
                Set_Acrid_basicAttack_1();
                Logger.LogInfo("Acrid Primary Basic Attack 1 Enable");
            } else { Logger.LogInfo("Acrid Primary Basic Attack 1 Disable"); }
        }

        public void Set_AcridConfig()
        {
            Acrid_VoidOpen_Check = Config.Bind<bool>("Acrid.Void Open - Strike", "Void Open - Strike Enable", true, "Skill Enable Setting");
            Acrid_VoidOpen_Multiple = Config.Bind<float>("Acrid.Void Open - Strike", "Multiple", 5.5f, "Skill Damage multiple");
            Acrid_VoidOpen_Cooldown = Config.Bind<float>("Acrid.Void Open - Strike", "Cooldown", 15, "Skill Cooldown");

            Acrid_EmergencyEscape_Check = Config.Bind<bool>("Acrid.EmergencyEscape", "EmergencyEscape Enable", true, "Skill Enable Setting");
            Acrid_EmergencyEscape_Cooldown = Config.Bind<float>("Acrid.EmergencyEscape", "Cooldown", 9, "Skill Cooldown");

            Acrid_BasicAttack_1_Check = Config.Bind<bool>("Acrid.BasicAttack-1", "Basic Attack 1 Enable", true, "Skill Enable Setting");
        }

        public void Set_Acrid_VoidOpen()
        {
            AcridObject = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("prefabs/characterbodies/crocobody"), "AcridBody", true);

            float multiple = Acrid_VoidOpen_Multiple.Value;

            LanguageAPI.Add("ACRID_SPECIAL_VOIDOPEN_NAME", "Void Open - Strike");
            LanguageAPI.Add("ACRID_SPECIAL_VOIDOPEN_DESCRIPTION", $"<b><style=cIsHealth>[Slayer]</b><color=#FFFFFF>Use the power of the void to deal heavy damage to nearby enemies." + "\n" +
                $"Heal 10% of damage per target" + "\n" +
                "Damage, range, and number of target increase according to level. (<b>Max at level 25 or higher</b>)</color>" + "\n" +
                "<style=cIsDamage>Attack Damage: Damage x " + multiple + " x LevelScale\n" +
                "Number of Target: 5 + LevelScale<color=#FFFFFF>");

            SkillDef Acrid_VoidOpen = ScriptableObject.CreateInstance<SkillDef>();

            Acrid_VoidOpen.activationState = new SerializableEntityStateType(typeof(Acrid_VoidOpen));
            Acrid_VoidOpen.activationStateMachineName = "Weapon";
            Acrid_VoidOpen.baseMaxStock = 1;
            Acrid_VoidOpen.baseRechargeInterval = Acrid_VoidOpen_Cooldown.Value;
            Acrid_VoidOpen.beginSkillCooldownOnSkillEnd = true;
            Acrid_VoidOpen.canceledFromSprinting = false;
            Acrid_VoidOpen.cancelSprintingOnActivation = false;
            Acrid_VoidOpen.fullRestockOnAssign = true;
            Acrid_VoidOpen.interruptPriority = InterruptPriority.Skill;
            Acrid_VoidOpen.isCombatSkill = true;
            Acrid_VoidOpen.mustKeyPress = false;
            Acrid_VoidOpen.rechargeStock = 1;
            Acrid_VoidOpen.requiredStock = 1;
            Acrid_VoidOpen.stockToConsume = 1;
            //For the skill icon, you will have to load a Sprite from your own AssetBundle
            Acrid_VoidOpen.icon = null;
            Acrid_VoidOpen.skillDescriptionToken = "ACRID_SPECIAL_VOIDOPEN_DESCRIPTION";
            Acrid_VoidOpen.skillName = "ACRID_SPECIAL_VOIDOPEN_NAME";
            Acrid_VoidOpen.skillNameToken = "ACRID_SPECIAL_VOIDOPEN_NAME";

            ContentAddition.AddSkillDef(Acrid_VoidOpen);

            SkillLocator skillLocator = AcridObject.GetComponent<SkillLocator>();
            SkillFamily skillFamily = skillLocator.special.skillFamily;

            Array.Resize(ref skillFamily.variants, skillFamily.variants.Length + 1);
            skillFamily.variants[skillFamily.variants.Length - 1] = new SkillFamily.Variant
            {
                skillDef = Acrid_VoidOpen,
                unlockableName = "",
                viewableNode = new ViewablesCatalog.Node(Acrid_VoidOpen.skillNameToken, false, null)
            };

            ContentAddition.AddEntityState<Acrid_VoidOpen>(out _);
        }

        public void Set_Acrid_EmergencyEscape()
        {
            AcridObject = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("prefabs/characterbodies/crocobody"), "AcridBody", true);

            float cooldown = Acrid_EmergencyEscape_Cooldown.Value;

            LanguageAPI.Add("ACRID_UTILITY_EMERGENCYESCAPE_NAME", "Emergency Escape");
            LanguageAPI.Add("ACRID_UTILITY_EMERGENCYESCAPE_DESCRIPTION", $"Stun nearby enemies and leap upwards.");

            SkillDef Acrid_EmergencyEscape = ScriptableObject.CreateInstance<SkillDef>();

            Acrid_EmergencyEscape.activationState = new SerializableEntityStateType(typeof(Acrid_EmergencyEscape));
            Acrid_EmergencyEscape.activationStateMachineName = "Body";
            Acrid_EmergencyEscape.baseMaxStock = 2;
            Acrid_EmergencyEscape.baseRechargeInterval = cooldown;
            Acrid_EmergencyEscape.beginSkillCooldownOnSkillEnd = true;
            Acrid_EmergencyEscape.canceledFromSprinting = false;
            Acrid_EmergencyEscape.cancelSprintingOnActivation = false;
            Acrid_EmergencyEscape.fullRestockOnAssign = true;
            Acrid_EmergencyEscape.interruptPriority = InterruptPriority.Skill;
            Acrid_EmergencyEscape.isCombatSkill = true;
            Acrid_EmergencyEscape.mustKeyPress = false;
            Acrid_EmergencyEscape.rechargeStock = 1;
            Acrid_EmergencyEscape.requiredStock = 1;
            Acrid_EmergencyEscape.stockToConsume = 1;
            //For the skill icon, you will have to load a Sprite from your own AssetBundle
            Acrid_EmergencyEscape.icon = null;
            Acrid_EmergencyEscape.skillDescriptionToken = "ACRID_UTILITY_EMERGENCYESCAPE_DESCRIPTION";
            Acrid_EmergencyEscape.skillName = "ACRID_UTILITY_EMERGENCYESCAPE_NAME";
            Acrid_EmergencyEscape.skillNameToken = "ACRID_UTILITY_EMERGENCYESCAPE_NAME";

            ContentAddition.AddSkillDef(Acrid_EmergencyEscape);

            SkillLocator skillLocator = AcridObject.GetComponent<SkillLocator>();
            SkillFamily skillFamily = skillLocator.utility.skillFamily;

            Array.Resize(ref skillFamily.variants, skillFamily.variants.Length + 1);
            skillFamily.variants[skillFamily.variants.Length - 1] = new SkillFamily.Variant
            {
                skillDef = Acrid_EmergencyEscape,
                unlockableName = "",
                viewableNode = new ViewablesCatalog.Node(Acrid_EmergencyEscape.skillNameToken, false, null)
            };

            ContentAddition.AddEntityState<Acrid_EmergencyEscape>(out _);
        }

        public void Set_Acrid_basicAttack_1()
        {
            AcridObject = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("prefabs/characterbodies/crocobody"), "AcridBody", true);

            LanguageAPI.Add("ACRID_PRIMARY_BASICATTACK_1_NAME", "Basic Attack-1");
            LanguageAPI.Add("ACRID_PRIMARY_BASICATTACK_1_DESCRIPTION", $"Basic Attack 1");

            SkillDef Acrid_BasicAttack_1 = ScriptableObject.CreateInstance<SkillDef>();

            Acrid_BasicAttack_1.activationState = new SerializableEntityStateType(typeof(Acrid_BasicAttack_1));
            Acrid_BasicAttack_1.activationStateMachineName = "Weapon";
            Acrid_BasicAttack_1.baseMaxStock = 5;
            Acrid_BasicAttack_1.baseRechargeInterval = 5;
            Acrid_BasicAttack_1.beginSkillCooldownOnSkillEnd = true;
            Acrid_BasicAttack_1.canceledFromSprinting = false;
            Acrid_BasicAttack_1.cancelSprintingOnActivation = false;
            Acrid_BasicAttack_1.fullRestockOnAssign = true;
            Acrid_BasicAttack_1.interruptPriority = InterruptPriority.Any;
            Acrid_BasicAttack_1.isCombatSkill = true;
            Acrid_BasicAttack_1.mustKeyPress = false;
            Acrid_BasicAttack_1.rechargeStock = 1;
            Acrid_BasicAttack_1.requiredStock = 1;
            Acrid_BasicAttack_1.stockToConsume = 1;
            //For the skill icon, you will have to load a Sprite from your own AssetBundle
            Acrid_BasicAttack_1.icon = null;
            Acrid_BasicAttack_1.skillDescriptionToken = "ACRID_PRIMARY_BASICATTACK_1_DESCRIPTION";
            Acrid_BasicAttack_1.skillName = "ACRID_PRIMARY_BASICATTACK_1_NAME";
            Acrid_BasicAttack_1.skillNameToken = "ACRID_PRIMARY_BASICATTACK_1_NAME";

            ContentAddition.AddSkillDef(Acrid_BasicAttack_1);

            SkillLocator skillLocator = AcridObject.GetComponent<SkillLocator>();
            SkillFamily skillFamily = skillLocator.primary.skillFamily;

            Array.Resize(ref skillFamily.variants, skillFamily.variants.Length + 1);
            skillFamily.variants[skillFamily.variants.Length - 1] = new SkillFamily.Variant
            {
                skillDef = Acrid_BasicAttack_1,
                unlockableName = "",
                viewableNode = new ViewablesCatalog.Node(Acrid_BasicAttack_1.skillNameToken, false, null)
            };

            ContentAddition.AddEntityState<Acrid_BasicAttack_1>(out _);
        }

    }


}
