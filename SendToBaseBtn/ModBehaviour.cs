using System;
using System.IO;
using System.Reflection;
using Duckov.UI;
using HarmonyLib;
using ItemStatsSystem;
using ItemStatsSystem.Data;
using SodaCraft.Localizations;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;

namespace SendToBaseBtn
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        public const string BtnUITextKey = "UI_SendToBase";
        private Harmony? _harmony;
        private bool _isInit;

        public static int CanSendTimes = 5;
        public static int AlreadySendTimes = 0;

        protected override void OnAfterSetup()
        {
            // Debug.Log("SendToBaseBtn模组：OnAfterSetup方法被调用");

            LoadConfig();
            if (!_isInit)
            {
                // Debug.Log("SendToBaseBtn模组：执行修补");
                _harmony = new Harmony("Lexcellent.SendToBaseBtn");
                _harmony.PatchAll(Assembly.GetExecutingAssembly());
                _isInit = true;
                // Debug.Log("SendToBaseBtn模组：修补完成");
            }

            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        protected override void OnBeforeDeactivate()
        {
            // Debug.Log("SendToBaseBtn模组：OnBeforeDeactivate方法被调用");
            if (_isInit)
                // Debug.Log("SendToBaseBtn模组：执行取消修补");
                if (_harmony != null)
                    _harmony.UnpatchAll();
            // Debug.Log("SendToBaseBtn模组：执行取消修补完毕");
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"加载场景：{scene.name}，模式：{mode.ToString()}");
            if (scene.name == "Base_SceneV2")
            {
                AlreadySendTimes = 0;
            }
        }

        void OnSceneUnloaded(Scene scene)
        {
            Debug.Log($"卸载场景：{scene.name}");

            if (scene.name == "Base_SceneV2")
            {
                AlreadySendTimes = 0;
            }
        }

        private void LoadConfig()
        {
            try
            {
                string configPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "info.ini");
                if (File.Exists(configPath))
                {
                    string[] lines = File.ReadAllLines(configPath);
                    foreach (string line in lines)
                    {
                        if (line.StartsWith("SendTimes="))
                        {
                            string value = line.Substring("SendTimes=".Length).Trim();
                            if (int.TryParse(value, out int times))
                            {
                                CanSendTimes = times;
                            }
                        }
                    }
                }
                else
                {
                    Debug.Log("SendToBaseBtn模组：未找到info.ini文件，使用默认值");
                }
            }
            catch (Exception e)
            {
                Debug.Log($"SendToBaseBtn模组：读取配置文件时出错：{e.Message}，使用默认值");
            }
        }
    }

    [HarmonyPatch(typeof(ItemOperationMenu), "MShow")]
    public static class ItemOperationMenuSetupPatch
    {
        private static Button? _sendToBaseButton;

        [HarmonyPostfix]
        public static void Postfix(ItemOperationMenu __instance)
        {
            // 创建"发送到基地"按钮
            if (_sendToBaseButton == null)
            {
                if (LevelManager.Instance.IsBaseLevel)
                {
                    LocalizationManager.SetOverrideText(ModBehaviour.BtnUITextKey, "发送到基地");
                }
                else
                {
                    LocalizationManager.SetOverrideText(ModBehaviour.BtnUITextKey,
                        $"发送到基地({ModBehaviour.CanSendTimes - ModBehaviour.AlreadySendTimes}/{ModBehaviour.CanSendTimes})");
                }

                // 使用反射获取私有的 btn_Wishlist 字段（标记按钮）
                var wishlistField = AccessTools.Field(typeof(ItemOperationMenu), "btn_Wishlist");
                var btnWishlist = (Button)wishlistField.GetValue(__instance);

                // 克隆标记按钮
                var buttonObj = GameObject.Instantiate(btnWishlist.gameObject, btnWishlist.transform.parent);
                _sendToBaseButton = buttonObj.GetComponent<Button>();
                _sendToBaseButton.name = "SendToBaseBtn";
                // 获取并修改文本
                var textComponent = _sendToBaseButton.GetComponentInChildren<TextLocalizor>();
                if (textComponent != null)
                    textComponent.Key = ModBehaviour.BtnUITextKey;
                else
                    Debug.Log("未获取到文本组件");

                // 修改按钮颜色
                var imageComponent = _sendToBaseButton.transform.Find("BG").GetComponent<ProceduralImage>();
                if (imageComponent != null)
                {
                    if (ModBehaviour.AlreadySendTimes >= ModBehaviour.CanSendTimes)
                    {
                        imageComponent.color = new Color(0.8f, 0.18f, 0.11f); // 背景
                    }
                    else
                    {
                        imageComponent.color = new Color(0.2f, 0.8f, 0.12f); // 背景
                    }
                }

                // 添加点击事件
                _sendToBaseButton.onClick.AddListener(() => { SendToBase(__instance); });
            }
            // 修改按钮颜色
            var imageComp = _sendToBaseButton.transform.Find("BG").GetComponent<ProceduralImage>();
            if (ModBehaviour.AlreadySendTimes >= ModBehaviour.CanSendTimes)
            {
                imageComp.color = new Color(0.8f, 0.18f, 0.11f); // 背景
            }
            else
            {
                imageComp.color = new Color(0.2f, 0.8f, 0.12f); // 背景
            }
            // 显示发送到基地按钮
            _sendToBaseButton?.gameObject.SetActive(true);
        }

        private static void SendToBase(ItemOperationMenu menu)
        {
            // Debug.Log("SendToBaseBtn模组：执行发送到基地");
            try
            {
                if (ModBehaviour.AlreadySendTimes >= ModBehaviour.CanSendTimes)
                {
                    NotificationText.Push("已发送次数已达上限");
                    return;
                }

                // 使用反射获取私有的 TargetItem 属性
                var targetItemProperty = AccessTools.Property(typeof(ItemOperationMenu), "TargetItem");
                var targetItem = (Item)targetItemProperty.GetValue(menu);
                if (targetItem == null) return;

                PlayerStorage.IncomingItemBuffer.Add(ItemTreeData.FromItem(targetItem));
                targetItem.Detach();
                targetItem.DestroyTree();
                menu.Close();

                NotificationText.Push($"{targetItem.DisplayName} 已发送到[马蜂自提点]");
                ModBehaviour.AlreadySendTimes += 1;
                if (LevelManager.Instance.IsBaseLevel)
                {
                    LocalizationManager.SetOverrideText(ModBehaviour.BtnUITextKey, "发送到基地");
                }
                else
                {
                    LocalizationManager.SetOverrideText(ModBehaviour.BtnUITextKey,
                        $"发送到基地({ModBehaviour.CanSendTimes - ModBehaviour.AlreadySendTimes}/{ModBehaviour.CanSendTimes})");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("SendToBaseBtn模组：发送到基地失败：" + e.Message);
                NotificationText.Push($"发送到基地失败,{e.Message}");
            }
        }
    }
}