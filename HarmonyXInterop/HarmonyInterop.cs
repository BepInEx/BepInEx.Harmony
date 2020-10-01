using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using HarmonyLib.Public.Patching;

namespace HarmonyXInterop
{
    public class PatchInfoWrapper
    {
        public PatchMethod[] prefixes;
        public PatchMethod[] postfixes;
        public PatchMethod[] transpilers;
        public PatchMethod[] finalizers;
    }
    
    public class PatchMethod
    {
        public string owner;
        
        public MethodInfo method; // need to be called 'method'

        /// <summary>Priority</summary>
        public int priority = -1;

        /// <summary>Before parameter</summary>
        public string[] before;

        /// <summary>After parameter</summary>
        public string[] after;

        public HarmonyMethod ToHarmonyMethod(out string patchOwner)
        {
            patchOwner = owner;
            return new HarmonyMethod
            {
                after = after,
                before = before,
                method = method,
                priority = priority,
            };
        }
    }
    
    public static class HarmonyInterop
    {
        private static Action<MethodBase, PatchInfo> UpdateWrapper =
            AccessTools.MethodDelegate<Action<MethodBase, PatchInfo>>(
                AccessTools.Method(typeof(HarmonyManipulator).Assembly.GetType("HarmonyLib.PatchFunctions"),
                    "UpdateWrapper"));
        
        public static void ApplyPatch(MethodBase target, PatchInfoWrapper info)
        {
            var pInfo = target.ToPatchInfo();
            lock (pInfo)
            {
                pInfo.prefixes = Sync(info.prefixes, pInfo.prefixes);
                pInfo.postfixes = Sync(info.postfixes, pInfo.postfixes);
                pInfo.transpilers = Sync(info.transpilers, pInfo.transpilers);
                pInfo.finalizers = Sync(info.finalizers, pInfo.finalizers);
            }
            UpdateWrapper(target, pInfo);
        }

        private static Patch[] Sync(PatchMethod[] add, Patch[] current)
        {
            if (add.Length == 0)
                return current;
            var toRemove = new HashSet<MethodInfo>(add.Select(a => a.method));
            current = current.Where(p => !toRemove.Contains(p.PatchMethod)).ToArray();
            var initialIndex = current.Length;
            return current.Concat(add.Where(method => method != null).Select((method, i) => new Patch(method.ToHarmonyMethod(out var owner), i + initialIndex, owner))).ToArray();
        }
    }
}