using System;
using System.Reflection;

namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class HarmonyPatch : Attribute
    {
        public HarmonyPatch(Type type, string method) { }
    }

    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class HarmonyPriority : Attribute
    {
        public HarmonyPriority(int priority) { }
    }

    internal static class Priority { public const int Last = 0; }
}

internal sealed class Storage
{
    public float Mass;
    public int Reads;
    public float ExactMassStored() { Reads++; return Mass; }
}

internal static class StorageMassOverflowTests
{
    private static int Main()
    {
        MethodInfo postfix = typeof(LargeStorageLocker.StorageMassOverflowPatch).GetMethod(
            "Postfix", BindingFlags.NonPublic | BindingFlags.Static);
        Check(postfix, 100f, 100f, 100f, 0);
        Check(postfix, 1000000000f, 1000000000f, 1000000000f, 0);
        Check(postfix, 2147483f, -2147483.75f, 2147483f, 1);
        Check(postfix, 2147483.75f, -2147483.75f, 2147483.75f, 1);
        Check(postfix, 1000000000f, -2147483.75f, 1000000000f, 1);
        Check(postfix, 2147482f, -1f, -1f, 1);
        Check(postfix, float.PositiveInfinity, -1f, -1f, 1);
        Check(postfix, float.NaN, -1f, -1f, 1);
        Check(postfix, 100f, float.NaN, float.NaN, 0);
        Console.WriteLine("Storage mass overflow tests passed.");
        return 0;
    }

    private static void Check(MethodInfo postfix, float mass, float original, float expected, int reads)
    {
        Storage storage = new Storage { Mass = mass };
        object[] args = { storage, original };
        postfix.Invoke(null, args);
        float actual = (float)args[1];
        if (!actual.Equals(expected) || storage.Reads != reads)
        {
            throw new Exception(string.Format("mass={0}, original={1}, expected={2}, actual={3}, reads={4}",
                mass, original, expected, actual, storage.Reads));
        }
    }
}
