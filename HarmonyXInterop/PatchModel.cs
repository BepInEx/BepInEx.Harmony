using System.Reflection;
using HarmonyLib;

namespace HarmonyXInterop
{
    public class PatchInfoWrapper
    {
        public PatchMethod[] finalizers;
        public PatchMethod[] postfixes;
        public PatchMethod[] prefixes;
        public PatchMethod[] transpilers;
    }

    public class PatchMethod
    {
        /// <summary>After parameter</summary>
        public string[] after;

        /// <summary>Before parameter</summary>
        public string[] before;

        public MethodInfo method; // need to be called 'method'
        public string owner;

        /// <summary>Priority</summary>
        public int priority = -1;

        public HarmonyMethod ToHarmonyMethod(out string patchOwner)
        {
            patchOwner = owner;
            return new HarmonyMethod
            {
                after = after,
                before = before,
                method = method,
                priority = priority
            };
        }
    }
}