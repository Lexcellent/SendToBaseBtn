using System;
using System.Reflection;
using UnityEngine;
using HarmonyLib;
using System.IO;
using Duckov.Economy;
using Duckov.UI;
using Duckov.Utilities;
using ItemStatsSystem;
using ItemStatsSystem.Data;
using SodaCraft.Localizations;
using TMPro;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;

namespace SendToBaseBtn
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        private bool _isInit = false;
        private Harmony? _harmony = null;
        
        public const string BtnUITextKey = "SendToBase";

        protected override void OnAfterSetup()
        {
            // Debug.Log("SendToBaseBtn模组：OnAfterSetup方法被调用");
            if (!_isInit)
            {
                // Debug.Log("SendToBaseBtn模组：执行修补");
                _harmony = new Harmony("Lexcellent.SendToBaseBtn");
                _harmony.PatchAll(Assembly.GetExecutingAssembly());
                _isInit = true;
                // Debug.Log("SendToBaseBtn模组：修补完成");
            }
        }

        protected override void OnBeforeDeactivate()
        {
            // Debug.Log("SendToBaseBtn模组：OnBeforeDeactivate方法被调用");
            if (_isInit)
            {
                // Debug.Log("SendToBaseBtn模组：执行取消修补");
                if (_harmony != null)
                {
                    _harmony.UnpatchAll();
                }

                // Debug.Log("SendToBaseBtn模组：执行取消修补完毕");
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
                    LocalizationManager.SetOverrideText(ModBehaviour.BtnUITextKey, "发送到基地");
                    // 使用反射获取私有的 btn_Wishlist 字段（标记按钮）
                    var wishlistField = AccessTools.Field(typeof(ItemOperationMenu), "btn_Wishlist");
                    Button btnWishlist = (Button)wishlistField.GetValue(__instance);

                    // 克隆标记按钮
                    GameObject buttonObj = GameObject.Instantiate(btnWishlist.gameObject, btnWishlist.transform.parent);
                    _sendToBaseButton = buttonObj.GetComponent<Button>();
                    _sendToBaseButton.name = "SendToBaseBtn";
                    // var transform = _sendToBaseButton.transform.parent.Find("Text (TMP)");
                    // if (transform == null)
                    // {
                    //     Debug.Log("未找到字体父类");
                    //     return;
                    // }

                    // 获取并修改文本
                    var textComponent = _sendToBaseButton.GetComponentInChildren<TextLocalizor>();
                    if (textComponent != null)
                    {
                        textComponent.Key = ModBehaviour.BtnUITextKey;
                    }
                    else
                    {
                        Debug.Log("未获取到文本组件");
                    }

                    // 修改按钮颜色
                    var imageComponent = _sendToBaseButton.transform.Find("BG").GetComponent<ProceduralImage>();
                    if (imageComponent != null)
                    {
                        imageComponent.color = new Color(0.2f, 0.8f, 0.12f); // 背景
                    }

                    // // 设置按钮大小和位置
                    // var rectTransform = _sendToBaseButton.GetComponent<RectTransform>();
                    // rectTransform.sizeDelta = new Vector2(200, 60);
                    // rectTransform.localPosition = new Vector3(0, -120, 0);

                    // 添加点击事件
                    _sendToBaseButton.onClick.AddListener(() => { SendToBase(__instance); });
                }

            // // 使用反射获取私有的 Dumpable 属性
            // var dumpableProperty = AccessTools.Property(typeof(ItemOperationMenu), "Dumpable");
            // bool isDumpable = (bool)dumpableProperty.GetValue(__instance);

            // 只有当物品可以丢弃时才显示发送到基地按钮
            _sendToBaseButton?.gameObject.SetActive(true);
        }

        private static void SendToBase(ItemOperationMenu menu)
        {
            // Debug.Log("SendToBaseBtn模组：执行发送到基地");
            try
            {
                // 使用反射获取私有的 TargetItem 属性
                var targetItemProperty = AccessTools.Property(typeof(ItemOperationMenu), "TargetItem");
                var targetItem = (Item)targetItemProperty.GetValue(menu);
                if (targetItem == null) return;
            
                PlayerStorage.IncomingItemBuffer.Add(ItemTreeData.FromItem(targetItem));
                targetItem.Detach();
                targetItem.DestroyTree();
                menu.Close();
            
                NotificationText.Push($"已发送到[马蜂自提点]");
            }catch(Exception e)
            {
                Debug.LogError("SendToBaseBtn模组：发送到基地失败：" + e.Message);
                NotificationText.Push($"发送到基地失败,{e.Message}");
            }
            
        }
    }
}