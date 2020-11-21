using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib.Internal.Patching;
using HarmonyXInterop;
using MonoMod.RuntimeDetour;

namespace HarmonyLib.Internal
{
    internal class MethodPatcher
    {
        private MethodBase mb;
        private PatchInfoWrapper previousState = new PatchInfoWrapper
        {
            prefixes = new PatchMethod[0],
            postfixes = new PatchMethod[0],
            transpilers = new PatchMethod[0],
            finalizers = new PatchMethod[0]
        };
        
        public void Apply()
        {
            PatchMethod[] ToPatchMethod(Patch[] patches)
            {
                return patches.Select(p => new PatchMethod
                {
                    after = p.after,
                    before = p.before,
                    method = p.PatchMethod,
                    priority = p.priority,
                    owner = p.owner,
                }).ToArray();
            }
            
            var info = mb.ToPatchInfo();
            var state = new PatchInfoWrapper
            {
                prefixes = ToPatchMethod(info.prefixes),
                postfixes = ToPatchMethod(info.postfixes),
                transpilers = ToPatchMethod(info.transpilers),
                finalizers = ToPatchMethod(info.finalizers),
            };

            static void Diff(PatchMethod[] last, PatchMethod[] curr, out PatchMethod[] add, out PatchMethod[] remove)
            {
                add = curr.Except(last, PatchMethodComparer.Instance).ToArray();
                remove = last.Except(curr, PatchMethodComparer.Instance).ToArray();
            }
            
            var add = new PatchInfoWrapper();
            var remove = new PatchInfoWrapper();
            
            Diff(previousState.prefixes, state.prefixes, out add.prefixes, out remove.prefixes);
            Diff(previousState.postfixes, state.postfixes, out add.postfixes, out remove.postfixes);
            Diff(previousState.transpilers, state.transpilers, out add.transpilers, out remove.transpilers);
            Diff(previousState.finalizers, state.finalizers, out add.finalizers, out remove.finalizers);

            previousState = state;
            
            HarmonyInterop.ApplyPatch(mb, add, remove);
        }
        
        public static MethodPatcher Create(MethodBase target)
        {
            return new MethodPatcher {mb = target};
        }
    }
    
    internal static class GlobalPatchState
    {
        private static readonly Dictionary<MethodBase, PatchInfo> PatchInfos = new Dictionary<MethodBase, PatchInfo>();
        private static readonly Dictionary<MethodBase, MethodPatcher> MethodPatchers = new Dictionary<MethodBase, MethodPatcher>();

        public static MethodPatcher GetMethodPatcher(this MethodBase methodBase)
        {
            lock (MethodPatchers)
            {
                if (MethodPatchers.TryGetValue(methodBase, out var methodPatcher))
                    return methodPatcher;
                return MethodPatchers[methodBase] = MethodPatcher.Create(methodBase);
            }
        }

        public static PatchInfo GetPatchInfo(this MethodBase methodBase)
        {
            lock (PatchInfos)
            {
                return PatchInfos.GetValueSafe(methodBase);
            }
        }

        public static PatchInfo ToPatchInfo(this MethodBase methodBase)
        {
            lock (PatchInfos)
            {
                if (PatchInfos.TryGetValue(methodBase, out var info))
                    return info;

                return PatchInfos[methodBase] = new PatchInfo();
            }
        }

        public static IEnumerable<MethodBase> GetPatchedMethods()
        {
            lock (PatchInfos)
            {
                return PatchInfos.Keys.ToList();
            }
        }
    }
}