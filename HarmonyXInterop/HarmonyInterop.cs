using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using HarmonyLib.Public.Patching;
using Mono.Cecil;

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
        private static readonly SortedDictionary<Version, string> Assemblies = new SortedDictionary<Version, string>();
        private static readonly Dictionary<Version, Assembly> AssemblyCache = new Dictionary<Version, Assembly>();

        public static void Initialize()
        {
            var curDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            foreach (var file in Directory.GetFiles(curDir, "0Harmony*.dll", SearchOption.AllDirectories))
            {
                using var ass = AssemblyDefinition.ReadAssembly(file);
                Assemblies.Add(ass.Name.Version, file);
            }

            AppDomain.CurrentDomain.AssemblyResolve += ResolveHarmonyDependency;
        }

        private static bool TryParseAssemblyName(string fullName, out AssemblyName assemblyName)
        {
            try
            {
                assemblyName = new AssemblyName(fullName);
                return true;
            }
            catch (Exception e)
            {
                File.AppendAllText("tryparseerr.log", $"Failed to parse {fullName}: {e}");
                assemblyName = null;
                return false;
            }
        }

        // In some cases interop lib name is not 0Harmony (e.g. build purposes)
        // In this case we normalize assembly names so that they are always resolved
        // properly
        private static byte[] FixupHarmonyAssemlbyName(string path)
        {
            using var ms = new MemoryStream();
            using var ad = AssemblyDefinition.ReadAssembly(path);
            ad.Name.Name = "0Harmony";
            ad.MainModule.Name = "0Harmony.dll";
            ad.Write(ms);
            return ms.ToArray();
        }
        
        private static Assembly ResolveHarmonyDependency(object sender, ResolveEventArgs args)
        {
            if (!TryParseAssemblyName(args.Name, out var name))
                return null;

            if (!name.Name.StartsWith("0Harmony"))
                return null;

            var assToLoad = Assemblies.LastOrDefault(kv => kv.Key <= name.Version);
            if (assToLoad.Value == null)
                return null;

            if (AssemblyCache.TryGetValue(assToLoad.Key, out var assembly))
                return assembly;

            try
            {
                assembly = Assembly.Load(FixupHarmonyAssemlbyName(assToLoad.Value));
                AssemblyCache[assToLoad.Key] = assembly;
                return assembly;
            }
            catch (Exception)
            {
                return null;
            }
        }

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
            return current.Concat(add.Where(method => method != null).Select((method, i) =>
                new Patch(method.ToHarmonyMethod(out var owner), i + initialIndex, owner))).ToArray();
        }
    }
}