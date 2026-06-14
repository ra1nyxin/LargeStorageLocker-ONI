using System;
using HarmonyLib;
using KMod;
using UnityEngine;

namespace LargeStorageLocker
{
    public sealed class Mod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            Debug.Log("[LargeStorageLocker] Loaded.");
        }
    }

    [HarmonyPatch(typeof(StorageLockerConfig), "ConfigureBuildingTemplate")]
    [HarmonyPriority(Priority.Last)]
    internal static class StorageLockerConfigPatch
    {
        // 1,000,000 metric tons. The vanilla field is a float in kilograms.
        private const float TargetCapacityKg = 1000000000f;

        private static void Postfix(GameObject go)
        {
            try
            {
                if (go == null)
                {
                    return;
                }

                Storage storage = go.GetComponent<Storage>();
                if (storage == null)
                {
                    Debug.LogWarning("[LargeStorageLocker] Storage component was not found; capacity was not changed.");
                    return;
                }

                if (float.IsNaN(TargetCapacityKg) || float.IsInfinity(TargetCapacityKg) || TargetCapacityKg <= 0f)
                {
                    Debug.LogWarning("[LargeStorageLocker] Invalid target capacity; capacity was not changed.");
                    return;
                }

                if (storage.capacityKg < TargetCapacityKg)
                {
                    storage.capacityKg = TargetCapacityKg;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[LargeStorageLocker] Failed to patch Storage Bin capacity: " + ex);
            }
        }
    }
}
