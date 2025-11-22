using System;
using System.IO;
using System.Collections.Generic;
using HarmonyLib;
using ItemStatsSystem;
using UnityEngine;
using Duckov;
using Duckov.Modding;
using SodaCraft.Localizations;
using UnityEngine;
using Duckov.Modding;
using HarmonyLib;
using System.Reflection;
using System.Linq;
using System.Runtime.CompilerServices;
using Duckov.Utilities;

// using ModConfig;

namespace DigByHand
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        // private void Update()
        // {
        //     CharacterMainControl character = CharacterMainControl.Main;
        //
        //     InteractableBase target = character?.interactAction.InteractTarget;
        //     if (target is null) return;
        //     
        //     if (target.requireItemId == 98 || target.requireItemId == 101)
        //     {
        //         if(!target.requireItem){ return; }
        //         
        //         // 禁用物品需求
        //         target.requireItem = false;
        //         target.requireItemId = 0; 
        //         // 修改时间字段
        //         var type = typeof(InteractableBase);
        //
        //         FieldInfo interactTimeField = type.GetField("interactTime",
        //             BindingFlags.NonPublic | BindingFlags.Instance);
        //         FieldInfo unlockTimeField = type.GetField("unlockTime",
        //             BindingFlags.NonPublic | BindingFlags.Instance);
        //         
        //         interactTimeField?.SetValue(target, 3f);
        //         unlockTimeField?.SetValue(target, 0f);
        //     }
        // }
        //
        // private static void Log(string msg)
        // {
        //     Debug.Log($"[徒手挖掘]: {msg}");
        // }


        private static Harmony _harmony;
        private const string MOD_NAME = "徒手挖掘";

        private static string persistentDataPath
        {
            get { return Path.Combine(Application.persistentDataPath, "DigbyHand.txt"); }
        }

        private void Awake()
        {
            _harmony = new Harmony("com.DigByHand.DigByHand");
            _harmony.PatchAll();
            Debug.Log("[徒手挖掘] 模组已加载");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchAll();
            Debug.Log("[徒手挖掘] 模组已卸载");
        }

        private void OnEnable()
        {
            LevelManager.OnAfterLevelInitialized += Clear;
            ModManager.OnModActivated += OnModActivated;
            if (ModConfigAPI.IsAvailable())
            {
                SetupModConfig();
                LoadConfigFromModConfig();
            }
            else
            {
                Debug.LogWarning("[徒手挖掘] ModConfigAPI不可用");
            }
        }

        private void OnDisable()
        {
            LevelManager.OnAfterLevelInitialized -= Clear;
            ModManager.OnModActivated -= OnModActivated;
            ModConfigAPI.SafeRemoveOnOptionsChangedDelegate(new Action<string>(this.OnModConfigOptionsChange));
        }

        protected void Start()
        {
            LevelManager.OnAfterLevelInitialized += Clear;
        }


        private static void Clear()
        {
            Debug.Log("[徒手挖掘] 清空已经挖掘点列表");
            ModGlobalStata.UnlockedInteractables.Clear();
        }


        /*  *******************************配置*******************************  */
        private void OnModActivated(ModInfo info, Duckov.Modding.ModBehaviour behaviour)
        {
            if (info.name == ModConfigAPI.ModConfigName)
            {
                Debug.Log("[徒手挖掘] ModConfig 成功激活!");
                SetupModConfig();
                LoadConfigFromModConfig();
            }
        }

        private void SetupModConfig()
        {
            if (!ModConfigAPI.IsAvailable())
            {
                Debug.LogWarning("[徒手挖掘] ModConfigAPI不可用");
                return;
            }

            Debug.LogWarning("[徒手挖掘] 准备添加配置项");
            ModConfigAPI.SafeAddOnOptionsChangedDelegate(OnModConfigOptionsChange);

            SystemLanguage[] array =
            {
                SystemLanguage.Chinese,
                SystemLanguage.ChineseSimplified,
                SystemLanguage.ChineseTraditional
            };
            bool isChinese = array.Contains(LocalizationManager.CurrentLanguage);


            //

            // ModConfigAPI.SafeAddBoolDropdownList(
            //     MOD_NAME,
            //     "mayHasBleedingDebuff",
            //     isChinese ? "徒手掰铁丝网时有概率伤到手" : "May hurt your hand",
            //     ModGlobalStata.MayHasBleedingDebuff
            // );
            //
            // ModConfigAPI.SafeAddBoolDropdownList(
            //     MOD_NAME,
            //     "consumeWaterAndEnergy",
            //     isChinese ? "徒手操作消耗水和能量" : "Consume water and energy",
            //     ModGlobalStata.ConsumeWaterAndEnergy
            // );

            ModConfigAPI.SafeAddInputWithSlider(
                MOD_NAME,
                "bleedingDebuffProb",
                isChinese ? "掰铁丝网流血概率" : "Probability of bleeding from the net when twisting wire",
                typeof(float),
                ModGlobalStata.BleedingDebuffProb,
                new Vector2(0.0f, 1.0f)
            );

            ModConfigAPI.SafeAddInputWithSlider(
                MOD_NAME,
                "costValue",
                isChinese ? "惩罚力度" : "Cost value",
                typeof(float),
                ModGlobalStata.CostValue,
                new Vector2(0.0f, 20.0f)
            );
            
            var costOption = new SortedDictionary<string, object>
            {
                { isChinese ? "无惩罚" : "None", (int)Cost.None },
                { isChinese ? "水分" : "Water", (int)Cost.Water },
                { isChinese ? "精力" : "Energy", (int)Cost.Energy },
                { isChinese ? "水分和精力" : "WaterAndEnergy", (int)Cost.WaterAndEnergy },
                { isChinese ? "生命值" : "Health", (int)Cost.Health },
                { isChinese ? "全部" : "All", (int)Cost.All }
            };

            ModConfigAPI.SafeAddDropdownList(
                MOD_NAME,
                "willCost",
                isChinese ? "惩罚类型" : "Cost type",
                costOption,
                typeof(int),
                ModGlobalStata.WillCost
            );


            ModConfigAPI.SafeAddBoolDropdownList(
                MOD_NAME,
                "inspection",
                isChinese ? "清理泥土/启用搜索" : "Clear Dirt / Enable Search",
                ModGlobalStata.Inspection
            );

            ModConfigAPI.SafeAddInputWithSlider(
                MOD_NAME,
                "interactTime",
                isChinese ? "交互延时时间" : "Need Interaction Time",
                typeof(float),
                ModGlobalStata.InteractTime,
                new Vector2(0.0f, 3.0f)
            );

            ModConfigAPI.SafeAddBoolDropdownList(
                MOD_NAME,
                "interactTimeEnabled",
                isChinese ? "开启交互延时" : "Need Interaction Time",
                ModGlobalStata.InteractTimeEnabled
            );

            //
        }

        private void OnModConfigOptionsChange(string option)
        {
            if (!option.StartsWith(MOD_NAME + "_"))
            {
                return;
            }

            // 读取配置
            LoadConfigFromModConfig();

            // 保存到本地配置文件
            SaveConfigToModConfig();

            Debug.Log($"[徒手挖掘]: 模组配置已更新 - {option}");
        }


        private void LoadConfigFromModConfig()
        {
            ModGlobalStata.InteractTime =
                ModConfigAPI.SafeLoad<float>(MOD_NAME, "interactTime", ModGlobalStata.InteractTime);
            ModGlobalStata.InteractTimeEnabled = ModConfigAPI.SafeLoad<bool>(MOD_NAME, "interactTimeEnabled",
                ModGlobalStata.InteractTimeEnabled);
            ModGlobalStata.ConsumeWaterAndEnergy = ModConfigAPI.SafeLoad<bool>(MOD_NAME, "consumeWaterAndEnergy",
                ModGlobalStata.ConsumeWaterAndEnergy);
            ModGlobalStata.MayHasBleedingDebuff = ModConfigAPI.SafeLoad<bool>(MOD_NAME, "mayHasBleedingDebuff",
                ModGlobalStata.MayHasBleedingDebuff);
            ModGlobalStata.Inspection = ModConfigAPI.SafeLoad<bool>(MOD_NAME, "inspection",
                ModGlobalStata.Inspection);
            ModGlobalStata.WillCost = (Cost)ModConfigAPI.SafeLoad<int>(MOD_NAME, "willCost",
                (int)ModGlobalStata.WillCost);
            ModGlobalStata.BleedingDebuffProb = ModConfigAPI.SafeLoad<float>(MOD_NAME, "bleedingDebuffProb",
                ModGlobalStata.BleedingDebuffProb);
            ModGlobalStata.CostValue = ModConfigAPI.SafeLoad<float>(MOD_NAME, "costValue",
                ModGlobalStata.CostValue);


            // Debug.Log($"[徒手挖掘] 修改前: {ModGlobalStata.InteractTime}");
            // Debug.Log($"[徒手挖掘] 修改前: {ModGlobalStata.InteractTimeEnabled}");
            // float InteractTime = ModConfigAPI.SafeLoad<float>(MOD_NAME, "InteractTime", ModGlobalStata.InteractTime);
            // bool InteractTimeEnabled = ModConfigAPI.SafeLoad<bool>(MOD_NAME, "InteractTimeEnabled", ModGlobalStata.InteractTimeEnabled);
            // Debug.Log($"[徒手挖掘] 读取的值: {InteractTime}");
            // Debug.Log($"[徒手挖掘] 读取的值: {InteractTimeEnabled}");
            // ModGlobalStata.InteractTime = InteractTime;
            // ModGlobalStata.InteractTimeEnabled = InteractTimeEnabled;
            // Debug.Log($"[徒手挖掘] 修改后: {ModGlobalStata.InteractTime}");
            // Debug.Log($"[徒手挖掘] 修改后: {ModGlobalStata.InteractTimeEnabled}");
        }

        private void SaveConfigToModConfig()
        {
            try
            {
                var config = new ModConfig();
                var json = JsonUtility.ToJson(config, true);
                File.WriteAllText(persistentDataPath, json);
                Debug.Log("[徒手挖掘] 配置已经保存");
            }
            catch (Exception e)
            {
                Debug.LogError($"[徒手挖掘]: 保存配置失败: {e}");
            }
        }
        /*  *******************************配置*******************************  */
    }

    public enum Cost
    {
        None = 0,
        Water = 1,
        Energy = 2,
        WaterAndEnergy = 3,
        Health = 4,
        All = 5
    }

    public static class ModGlobalStata
    {
        public static readonly HashSet<int> UnlockedInteractables = new HashSet<int>();
        public static readonly HashSet<int> SpecialItemIds = new HashSet<int> { 98, 101 };
        public static float InteractTime = 3f; // 挖掘时间
        public static bool InteractTimeEnabled = true; // 是否开启进度条
        public static bool ConsumeWaterAndEnergy = true; //消耗饥渴度
        public static bool MayHasBleedingDebuff = true; // 可能流血

        public static bool Inspection = true; // 需要搜索
        public static Cost WillCost = Cost.WaterAndEnergy; // 代价类型
        public static float CostValue = 5f; // 惩罚力度
        public static float BleedingDebuffProb = 0.3f;
    }

    [Serializable]
    public class ModConfig
    {
        public float interactTime = ModGlobalStata.InteractTime;
        public bool interactTimeEnabled = ModGlobalStata.InteractTimeEnabled;
        public bool consumeWaterAndEnergy = ModGlobalStata.ConsumeWaterAndEnergy;
        public bool mayHasBleedingDebuff = ModGlobalStata.MayHasBleedingDebuff;
        public bool inspection = ModGlobalStata.Inspection;
        public int willCost = (int)ModGlobalStata.WillCost;
        public float costValue = ModGlobalStata.CostValue;
        public float bleedingDebuffProb = ModGlobalStata.BleedingDebuffProb;
    }

    [HarmonyPatch]
    public class InteractableBasePatch
    {
        // private static readonly HashSet<int> unlockedInteractables = new HashSet<int>();
        // private static readonly HashSet<int> specialItemIds = new HashSet<int> { 98, 101 };


        private static int GetInteractableId(InteractableBase interactable)
        {
            if (interactable.overrideItemUsedKey) return interactable.overrideItemUsedSaveKey.GetHashCode();

            var position = interactable.transform.position * 10f;
            var x = (int)Math.Round(position.x);
            var y = (int)Math.Round(position.y);
            var z = (int)Math.Round(position.z);
            return $"Interactable_{x}_{y}_{z}".GetHashCode();
        }

        [HarmonyPatch(typeof(InteractableBase), "Start")]
        [HarmonyPostfix]
        public static void Start_Postfix(InteractableBase __instance)
        {
            try
            {
                if (ModGlobalStata.SpecialItemIds.Contains(__instance.requireItemId))
                {
                    // 存储原始状态并禁用道具需求
                    Traverse.Create(__instance).Field("_originalRequireItem").SetValue(__instance.requireItem);
                    __instance.requireItem = false;
        
                    // 检查解锁状态
                    var interactableId = GetInteractableId(__instance);
                    if (ModGlobalStata.UnlockedInteractables.Contains(interactableId) ||
                        !ModGlobalStata.InteractTimeEnabled)
                    {
                        // 已经解锁过了
                        Traverse.Create(__instance).Field("requireItemUsed").SetValue(true);
                        Traverse.Create(__instance).Field("interactTime").SetValue(0f);
                    }
                    else
                    {
                        Traverse.Create(__instance).Field("requireItemUsed").SetValue(true);
                        Traverse.Create(__instance).Field("interactTime").SetValue(
                            ModGlobalStata.InteractTime
                        );
                        // Traverse.Create(__instance).Field("needInspect")?.SetValue(ModGlobalStata.Inspection);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[徒手挖掘] Error in Start_Postfix: {e}");
            }
        }


        [HarmonyPatch(typeof(InteractableBase), "StartInteract")]
        [HarmonyPrefix]
        public static void StartInteract_Prefix(InteractableBase __instance)
        {
            try
            {
                if (ModGlobalStata.SpecialItemIds.Contains(__instance.requireItemId))
                {
                    // 存储原始状态并禁用道具需求
                    Traverse.Create(__instance).Field("_originalRequireItem").SetValue(__instance.requireItem);
                    __instance.requireItem = false;

                    // 检查解锁状态
                    var interactableId = GetInteractableId(__instance);
                    if (ModGlobalStata.UnlockedInteractables.Contains(interactableId) ||
                        !ModGlobalStata.InteractTimeEnabled)
                    {
                        // 已经解锁过了
                        Traverse.Create(__instance).Field("requireItemUsed").SetValue(true);
                        Traverse.Create(__instance).Field("interactTime").SetValue(0f);
                    }
                    else
                    {
                        Traverse.Create(__instance).Field("requireItemUsed").SetValue(true);
                        Traverse.Create(__instance).Field("interactTime").SetValue(
                            ModGlobalStata.InteractTime
                        );
                        Traverse.Create(__instance).Field("inventoryReference")?.Field("needInspection").SetValue(ModGlobalStata.Inspection);
                        Debug.Log($"[徒手挖掘] 设置了需要搜索{ModGlobalStata.Inspection}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[徒手挖掘] Error in Start_Postfix: {e}");
            }
        }


        // [HarmonyPatch(typeof(InteractableBase), "FinishInteract")]
        // [HarmonyPostfix]
        // public static void FinishInteract_Postfix(InteractableBase __instance, CharacterMainControl _interactCharacter)
        // {
        //     try
        //     {
        //         if (ModGlobalStata.SpecialItemIds.Contains(__instance.requireItemId))
        //         {
        //             var interactableId = GetInteractableId(__instance);
        //             ModGlobalStata.UnlockedInteractables.Add(interactableId);
        //             Traverse.Create(__instance).Field("requireItemUsed").SetValue(true);
        //             Traverse.Create(__instance).Field("interactTime").SetValue(0f);
        //
        //             Debug.Log("[徒手挖掘] (FinishInteract)尝试消耗能量和水");
        //             if (_interactCharacter != null && ModGlobalStata.ConsumeWaterAndEnergy)
        //             {
        //                 Debug.Log("[徒手挖掘] 尝试消耗能量和水");
        //                 _interactCharacter.CurrentWater -= 20;
        //                 _interactCharacter.CurrentEnergy -= 20;
        //                 if (__instance.requireItemId == 101 && ModGlobalStata.MayHasBleedingDebuff)
        //                 {
        //                     _interactCharacter.AddBuff(GameplayDataSettings.Buffs.BleedSBuff, _interactCharacter, 0);
        //                 }
        //             }
        //         }
        //     }
        //     catch (Exception e)
        //     {
        //         Debug.LogError($"[徒手挖掘] Error in FinishInteract_Postfix: {e}");
        //     }
        // }

        [HarmonyPatch(typeof(InteractableBase), "StopInteract")]
        [HarmonyPrefix]
        public static void StopInteract_Prefix(InteractableBase __instance)
        {
            try
            {
                if (ModGlobalStata.SpecialItemIds.Contains(__instance.requireItemId))
                {
                    var interactableId = GetInteractableId(__instance);

                    Debug.Log("[徒手挖掘] (StopInteract)尝试消耗能量和水");
                    CharacterMainControl _interactCharacter =
                        __instance.GetComponent<CharacterMainControl>() ?? CharacterMainControl.Main;
                    if (_interactCharacter != null &&
                        !ModGlobalStata.UnlockedInteractables.Contains(interactableId))
                    {
                        // if (ModGlobalStata.ConsumeWaterAndEnergy)
                        // {
                        //     Debug.Log("[徒手挖掘] 消耗能量和水");
                        //     _interactCharacter.CurrentWater -= 5;
                        //     _interactCharacter.CurrentEnergy -= 5;
                        // }
                        switch (ModGlobalStata.WillCost)
                        {
                            case Cost.All:
                            {
                                _interactCharacter.CurrentEnergy -= ModGlobalStata.CostValue;
                                _interactCharacter.CurrentWater -= ModGlobalStata.CostValue;
                                _interactCharacter.AddHealth(-1 * ModGlobalStata.CostValue);
                                break;
                            }
                            case Cost.Energy:
                            {
                                _interactCharacter.CurrentEnergy -= ModGlobalStata.CostValue;
                                break;
                            }
                            case Cost.Health:
                            {
                                _interactCharacter.AddHealth(-1 * ModGlobalStata.CostValue);
                                break;
                            }
                            case Cost.Water:
                            {
                                _interactCharacter.CurrentWater -= ModGlobalStata.CostValue;
                                break;
                            }
                            case Cost.WaterAndEnergy:
                            {
                                _interactCharacter.CurrentEnergy -= ModGlobalStata.CostValue;
                                _interactCharacter.CurrentWater -= ModGlobalStata.CostValue;
                                break;
                            }
                            default:
                            {
                                break;
                            }
                        }
                        

                        if (__instance.requireItemId == 101 && ModGlobalStata.BleedingDebuffProb > 0)
                        {
                            Debug.Log("[徒手挖掘] 概率触发流血debug");
                            var random = new System.Random();
                            // if (random.NextDouble() < 0.3)
                            if (random.NextDouble() < ModGlobalStata.BleedingDebuffProb)
                            {
                                _interactCharacter.AddBuff(GameplayDataSettings.Buffs.BleedSBuff, _interactCharacter,
                                    0);
                            }
                        }
                    }

                    ModGlobalStata.UnlockedInteractables.Add(interactableId);
                    Traverse.Create(__instance).Field("requireItemUsed").SetValue(true);
                    Traverse.Create(__instance).Field("interactTime").SetValue(0f);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[徒手挖掘] Error in StopInteract_Postfix: {e}");
            }
        }

        [HarmonyPatch(typeof(InteractableBase), "TryGetRequiredItem")]
        [HarmonyPrefix]
        public static bool TryGetRequiredItem_Prefix(InteractableBase __instance, ref ValueTuple<bool, Item> __result)
        {
            try
            {
                if (ModGlobalStata.SpecialItemIds.Contains(__instance.requireItemId))
                {
                    __result = new ValueTuple<bool, Item>(true, null);
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[徒手挖掘] Error in TryGetRequiredItem_Prefix: {e}");
            }

            return true;
        }
    }
}