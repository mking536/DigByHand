using UnityEngine;
using System.Reflection;

namespace DigByHand
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        private void Update()
        {
            CharacterMainControl character = CharacterMainControl.Main;

            InteractableBase target = character?.interactAction.InteractTarget;
            if (target is null) return;
            
            if (target.requireItemId == 98 || target.requireItemId == 101)
            {
                if(!target.requireItem){ return; }
                
                // 禁用物品需求
                target.requireItem = false;
                target.requireItemId = 0; 
                // 修改时间字段
                var type = typeof(InteractableBase);

                FieldInfo interactTimeField = type.GetField("interactTime",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                FieldInfo unlockTimeField = type.GetField("unlockTime",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                
                interactTimeField?.SetValue(target, 3f);
                unlockTimeField?.SetValue(target, 0f);
            }
        }

        private static void Log(string msg)
        {
            Debug.Log($"[徒手挖掘]: {msg}");
        }
    }
}