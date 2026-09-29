using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace LargeStorageLocker
{
    /// <summary>
    /// Recovery guards for the two vanilla consumers of the "FetchChore" scene partitioner layer.
    /// </summary>
    /// <remarks>
    /// A FetchChore adds itself to the fetch layer bucket from its constructor and removes itself in
    /// Cleanup(). Tearing an entry down frees its handle, nulls the entry's obj and returns the entry
    /// object to a shared pool, but a query validates an entry only on its bounds and query id, never
    /// on its layer or its obj. A consumer can therefore be handed a null or already recycled entry.
    /// Both consumers assume they only ever receive a FetchChore, and one of them reinterprets the
    /// entry without checking at all, so a stale entry turns into either the engine's own assert or a
    /// hard crash.
    ///
    /// Vanilla already declares such an entry unusable - it asserts on exactly those three conditions
    /// ("wasn't a FetchChore", "with a null target", "that isNull"). These guards only perform the
    /// recovery the engine omits: drop that one entry and keep scanning. No game rule, balance number,
    /// element or storage value is touched.
    ///
    /// A 1,000,000 t bin moves mass in bursts three orders of magnitude larger than vanilla, which
    /// creates and destroys far more Pickupable/FetchChore pairs, so a window that is normally almost
    /// impossible to hit is reached routinely. The guards do not depend on knowing why an entry went
    /// stale; they only refuse to act on one that is.
    ///
    /// Every guard resolves its target by reflection, validates the exact signature it is about to
    /// patch, and fails open with a logged error. A game update that moves or reshapes a target
    /// therefore leaves that one guard uninstalled instead of breaking the mod, the save, or - worse -
    /// silently changing a code path this file was never meant to touch.
    /// </remarks>
    internal static class FetchLayerGuards
    {
        // Flip any of these to false to drop a single guard without touching the others.
        private const bool GuardSweeperPath = true;
        private const bool GuardMultiFetchPath = true;
        private const bool SuppressFetchLayerAsserts = true;

        // A stale entry can keep appearing for a long time, so log the first few and then every Nth.
        private const int LogFirstN = 5;
        private const int LogEveryNth = 500;

        // Shared by all three "FindNextChore found an entry ..." asserts.
        private const string FetchLayerAssertMarker = "FindNextChore found an entry";

        // Metadata name of the enum both consumers return. Util.IterationInstruction is a nested type,
        // so its name carries a '+'. It is compared as a string because this file has no reason to
        // resolve the type itself: the guards never construct or return one.
        private const string IterationInstructionTypeName = "Util+IterationInstruction";

        // Prefix method names, kept here because C# 5 has no nameof(). Verified at install time so a
        // rename fails loudly instead of patching the wrong thing.
        private const string SweeperPrefixName = "OnSweeperEntry";
        private const string MultiFetchPrefixName = "OnMultiFetchEntry";
        private const string AssertPrefixName = "OnFetchLayerAssert";

        private static bool _installed;
        private static int _skippedSweeper;
        private static int _skippedMultiFetch;
        private static int _suppressedAsserts;
        private static int _prefixFailures;

        internal static void Install(Harmony harmony)
        {
            if (_installed)
            {
                return;
            }

            _installed = true;

            if (harmony == null)
            {
                Debug.LogError("[LargeStorageLocker] Fetch layer guards were not installed: no Harmony instance was supplied.");
                return;
            }

            if (GuardSweeperPath)
            {
                InstallSweeperGuard(harmony);
            }

            if (GuardMultiFetchPath)
            {
                InstallMultiFetchGuard(harmony);
            }

            if (SuppressFetchLayerAsserts)
            {
                InstallAssertBackstop(harmony);
            }
        }

        // ---- Guard 1: the chore evaluator that Auto-Sweepers reach through their solid transfer arm.

        private static void InstallSweeperGuard(Harmony harmony)
        {
            try
            {
                MethodInfo prefix = FindPrefix(SweeperPrefixName);
                MethodBase target = ResolveChoreEvaluator();

                if (prefix == null || target == null)
                {
                    Debug.LogError("[LargeStorageLocker] The solid transfer arm guard was not installed ("
                        + (prefix == null ? "prefix method missing" : "chore evaluator not resolved")
                        + "). A stale fetch layer entry on that path can still assert.");
                    return;
                }

                harmony.Patch(target, prefix: new HarmonyMethod(prefix));
                Debug.Log("[LargeStorageLocker] Fetch layer guard installed on " + Describe(target) + " (solid transfer arm chore scan).");
            }
            catch (Exception ex)
            {
                Debug.LogError("[LargeStorageLocker] The solid transfer arm guard failed to install: " + ex);
            }
        }

        /// <summary>
        /// Resolves the evaluator ChoreConsumer passes to the chore providers. It is a compiler
        /// generated lambda, so its name is not stable and is never matched against.
        /// </summary>
        private static MethodBase ResolveChoreEvaluator()
        {
            // Preferred: the delegate the game itself stored. That is by definition the method the
            // engine will call, so it needs no guess about compiler generated names. Reading the
            // field requires the static constructor to have run; that constructor only allocates the
            // closure and the delegate, so forcing it here is safe and has no other effect.
            FieldInfo field = typeof(ChoreConsumer).GetField("FindNextChoreEvaluateEntryHelper",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (field != null)
            {
                try
                {
                    RuntimeHelpers.RunClassConstructor(typeof(ChoreConsumer).TypeHandle);
                    Delegate evaluator = field.GetValue(null) as Delegate;
                    if (evaluator != null)
                    {
                        MethodInfo resolved = evaluator.Method;
                        if (IsChoreEvaluator(resolved))
                        {
                            return resolved;
                        }

                        Debug.LogError("[LargeStorageLocker] ChoreConsumer's chore evaluator no longer has the expected shape; the solid transfer arm guard is being skipped rather than patching an unverified method.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[LargeStorageLocker] Could not read ChoreConsumer's chore evaluator directly, falling back to a signature scan: " + ex.Message);
                }
            }

            // Fallback: the evaluator is a method shaped (object, ChoreConsumer) -> IterationInstruction
            // declared on ChoreConsumer or one of its nested closure types.
            MethodInfo found = ScanChoreEvaluator(typeof(ChoreConsumer));
            if (found != null)
            {
                return found;
            }

            Type[] nestedTypes = typeof(ChoreConsumer).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic);
            foreach (Type nested in nestedTypes)
            {
                found = ScanChoreEvaluator(nested);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static MethodInfo ScanChoreEvaluator(Type declaringType)
        {
            MethodInfo found = null;
            MethodInfo[] methods = declaringType.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            foreach (MethodInfo candidate in methods)
            {
                if (!IsChoreEvaluator(candidate))
                {
                    continue;
                }

                if (found != null)
                {
                    // More than one candidate means the shape is no longer unique. Refuse to guess.
                    return null;
                }

                found = candidate;
            }

            return found;
        }

        private static bool IsChoreEvaluator(MethodInfo method)
        {
            if (method == null || !ReturnsIterationInstruction(method))
            {
                return false;
            }

            ParameterInfo[] parameters = method.GetParameters();
            return parameters.Length == 2
                && parameters[0].ParameterType == typeof(object)
                && parameters[1].ParameterType == typeof(ChoreConsumer);
        }

        private static bool OnSweeperEntry(object __0)
        {
            try
            {
                // Exactly the conditions vanilla asserts on before it will use this entry: it has to be
                // a live FetchChore, and that chore has to have a target. Everything else is dropped.
                FetchChore chore = __0 as FetchChore;
                if (chore != null && chore.target != null)
                {
                    return true;
                }

                ReportSkip(ref _skippedSweeper, "scanning for a solid transfer arm",
                    chore == null ? "entry was not a live FetchChore" : "chore had no target", __0);
                return false;
            }
            catch (Exception ex)
            {
                return FailOpen("solid transfer arm chore scan", ex);
            }
        }

        // ---- Guard 2: gathering nearby fetch chores for a sweeping area chore.

        private static void InstallMultiFetchGuard(Harmony harmony)
        {
            try
            {
                MethodInfo prefix = FindPrefix(MultiFetchPrefixName);
                MethodInfo target = FindVisitor(typeof(FetchAreaChore), "gatherNearbyFetchChoresVisitor");

                if (prefix == null || target == null)
                {
                    Debug.LogError("[LargeStorageLocker] The nearby chore gathering guard was not installed ("
                        + (prefix == null ? "prefix method missing" : "visitor not found with the expected signature")
                        + "). A stale fetch layer entry on that path can still crash the game.");
                    return;
                }

                harmony.Patch(target, prefix: new HarmonyMethod(prefix));
                Debug.Log("[LargeStorageLocker] Fetch layer guard installed on " + Describe(target) + " (nearby chore gathering).");
            }
            catch (Exception ex)
            {
                Debug.LogError("[LargeStorageLocker] The nearby chore gathering guard failed to install: " + ex);
            }
        }

        /// <summary>
        /// Looks a visitor up by name. There is deliberately no signature-only fallback: this class
        /// holds several visitors, and patching one that belongs to a different layer would silently
        /// change a code path this guard has no business touching.
        /// </summary>
        private static MethodInfo FindVisitor(Type declaringType, string name)
        {
            MethodInfo found = null;
            MethodInfo[] methods = declaringType.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            foreach (MethodInfo candidate in methods)
            {
                if (candidate.Name != name || !IsFetchEntryVisitor(candidate))
                {
                    continue;
                }

                if (found != null)
                {
                    return null;
                }

                found = candidate;
            }

            return found;
        }

        private static bool IsFetchEntryVisitor(MethodInfo method)
        {
            if (method == null || !ReturnsIterationInstruction(method))
            {
                return false;
            }

            ParameterInfo[] parameters = method.GetParameters();
            return parameters.Length == 2
                && parameters[0].ParameterType == typeof(object)
                && parameters[1].ParameterType.IsByRef;
        }

        private static bool OnMultiFetchEntry(object __0)
        {
            try
            {
                // This visitor only ever needs a FetchChore. It never touches the chore's target, so
                // neither does the guard - a live chore is passed through completely unchanged.
                FetchChore chore = __0 as FetchChore;
                if (chore != null)
                {
                    return true;
                }

                ReportSkip(ref _skippedMultiFetch, "gathering nearby fetch chores",
                    "entry was not a live FetchChore", __0);
                return false;
            }
            catch (Exception ex)
            {
                return FailOpen("nearby chore gathering", ex);
            }
        }

        // ---- Guard 3: the backstop, in case a stale entry reaches an assert through a path the two
        // guards above do not cover.

        private static void InstallAssertBackstop(Harmony harmony)
        {
            try
            {
                MethodInfo prefix = FindPrefix(AssertPrefixName);
                MethodInfo target = FindAssertWithMessage();

                if (prefix == null || target == null)
                {
                    Debug.LogError("[LargeStorageLocker] The assert backstop was not installed ("
                        + (prefix == null ? "prefix method missing" : "DebugUtil.Assert(bool, string) not found")
                        + "). The two primary guards are unaffected.");
                    return;
                }

                harmony.Patch(target, prefix: new HarmonyMethod(prefix));
                Debug.Log("[LargeStorageLocker] Backstop installed on " + Describe(target) + " (suppresses the stale fetch layer entry assert only).");
            }
            catch (Exception ex)
            {
                Debug.LogError("[LargeStorageLocker] The assert backstop failed to install: " + ex);
            }
        }

        private static MethodInfo FindAssertWithMessage()
        {
            MethodInfo found = null;
            MethodInfo[] methods = typeof(DebugUtil).GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);

            foreach (MethodInfo candidate in methods)
            {
                if (candidate.Name != "Assert")
                {
                    continue;
                }

                ParameterInfo[] parameters = candidate.GetParameters();
                if (parameters.Length != 2 || parameters[0].ParameterType != typeof(bool) || parameters[1].ParameterType != typeof(string))
                {
                    continue;
                }

                if (found != null)
                {
                    return null;
                }

                found = candidate;
            }

            return found;
        }

        private static bool OnFetchLayerAssert(bool __0, string __1)
        {
            try
            {
                // Only a failing assert whose text is one of the fetch layer entry messages is
                // suppressed. Every other assert in the game runs untouched.
                if (__0 || __1 == null || __1.IndexOf(FetchLayerAssertMarker, StringComparison.Ordinal) < 0)
                {
                    return true;
                }

                ReportSuppressedAssert(__1);
                return false;
            }
            catch (Exception ex)
            {
                return FailOpen("engine assert", ex);
            }
        }

        // ---- Shared helpers.

        private static bool ReturnsIterationInstruction(MethodInfo method)
        {
            Type returnType = method.ReturnType;
            return returnType != null && returnType.FullName == IterationInstructionTypeName;
        }

        private static MethodInfo FindPrefix(string name)
        {
            MethodInfo prefix = typeof(FetchLayerGuards).GetMethod(name,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (prefix == null)
            {
                Debug.LogError("[LargeStorageLocker] Prefix method " + name + " is missing from " + typeof(FetchLayerGuards).FullName + ".");
            }

            return prefix;
        }

        private static void ReportSkip(ref int counter, string site, string reason, object entry)
        {
            counter++;
            if (!ShouldLog(counter))
            {
                return;
            }

            Debug.LogWarning(string.Format(
                "[LargeStorageLocker] Ignored a stale entry in the fetch chore layer while {0} ({1}, obj={2}, occurrence {3}). The scan continues with the next entry.",
                site, reason, DescribeObject(entry), counter));
        }

        private static void ReportSuppressedAssert(string message)
        {
            _suppressedAsserts++;
            if (!ShouldLog(_suppressedAsserts))
            {
                return;
            }

            Debug.LogWarning(string.Format(
                "[LargeStorageLocker] Suppressed the engine assert \"{0}\" (occurrence {1}). A stale fetch chore layer entry is not a state the chore system can act on, so the scan continues with the next entry.",
                message, _suppressedAsserts));
        }

        private static bool FailOpen(string site, Exception ex)
        {
            _prefixFailures++;
            if (ShouldLog(_prefixFailures))
            {
                Debug.LogError("[LargeStorageLocker] The fetch layer guard for " + site
                    + " hit an unexpected error and let the original code run unchanged: " + ex);
            }

            return true;
        }

        private static bool ShouldLog(int count)
        {
            return count <= LogFirstN || (count % LogEveryNth) == 0;
        }

        private static string DescribeObject(object obj)
        {
            if (obj == null)
            {
                return "<null>";
            }

            // Only the managed type is read. Touching anything else on a possibly destroyed or
            // already recycled object is exactly what these guards exist to avoid.
            Type type = obj.GetType();
            return type.FullName != null ? type.FullName : type.Name;
        }

        private static string Describe(MethodBase method)
        {
            if (method == null)
            {
                return "<null>";
            }

            Type declaringType = method.DeclaringType;
            return (declaringType != null ? declaringType.FullName : "?") + "::" + method.Name;
        }
    }
}
