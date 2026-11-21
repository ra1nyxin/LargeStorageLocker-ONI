using HarmonyLib;

namespace LargeStorageLocker
{
    [HarmonyPatch(typeof(Storage), "MassStored")]
    [HarmonyPriority(Priority.Last)]
    internal static class StorageMassOverflowPatch
    {
        // Below this point, rounding kilograms * 1000 cannot overflow an Int32.
        private const float OverflowCheckMassKg = 2147483f;

        private static void Postfix(Storage __instance, ref float __result)
        {
            if (__result >= 0f || float.IsNaN(__result))
            {
                return;
            }

            float exactMass = __instance.ExactMassStored();
            if (exactMass >= OverflowCheckMassKg && !float.IsInfinity(exactMass))
            {
                // At this magnitude float already has less than milligram precision.
                __result = exactMass;
            }
        }
    }
}
