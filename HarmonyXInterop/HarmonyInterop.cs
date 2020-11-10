using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using HarmonyLib.Public.Patching;
using HarmonyLib.Tools;
using Mono.Cecil;

namespace HarmonyXInterop
{
    public static class HarmonyInterop
    {
        private static readonly SortedDictionary<Version, string> Assemblies = new SortedDictionary<Version, string>();

        private static readonly Func<MethodBase, PatchInfo, MethodInfo> UpdateWrapper =
            AccessTools.MethodDelegate<Func<MethodBase, PatchInfo, MethodInfo>>(
                AccessTools.Method(typeof(HarmonyManipulator).Assembly.GetType("HarmonyLib.PatchFunctions"),
                    "UpdateWrapper"));

        private static readonly Action<Logger.LogChannel, Func<string>> HarmonyLog =
            AccessTools.MethodDelegate<Action<Logger.LogChannel, Func<string>>>(AccessTools.Method(typeof(Logger),
                "Log"));

        private static readonly Action<Logger.LogChannel, string> HarmonyLogText =
            AccessTools.MethodDelegate<Action<Logger.LogChannel, string>>(AccessTools.Method(typeof(Logger),
                "LogText"));
        
        private static readonly Dictionary<string, long> shimCache = new Dictionary<string, long>();
        private static BinaryWriter cacheWriter;

        public static void Log(int channel, Func<string> message)
        {
            HarmonyLog((Logger.LogChannel) channel, message);
        }
        
        public static void LogText(int channel, string message)
        {
            HarmonyLogText((Logger.LogChannel) channel, message);
        }
        
        public static void Initialize(string cachePath)
        {
            Directory.CreateDirectory(cachePath);
            var cacheFile = Path.Combine(cachePath, "harmony_interop_cache.dat");
            var curDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            foreach (var file in Directory.GetFiles(curDir, "0Harmony*.dll", SearchOption.AllDirectories))
            {
                using var ass = AssemblyDefinition.ReadAssembly(file);
                // Don't add normal Harmony, resolve it normally
                if (ass.Name.Name != "0harmony")
                    Assemblies.Add(ass.Name.Version, ass.Name.Name);
            }

            if (File.Exists(cacheFile))
            {
                try
                {
                    using var br = new BinaryReader(File.OpenRead(cacheFile));
                    while (true)
                    {
                        var file = br.ReadString();
                        var writeTime = br.ReadInt64();
                        shimCache[file] = writeTime;
                    }
                }
                catch (Exception)
                {
                    // Skip
                }
            }

            cacheWriter = new BinaryWriter(File.Create(cacheFile));
            foreach (var kv in shimCache)
            {
                cacheWriter.Write(kv.Key);
                cacheWriter.Write(kv.Value);
            }
            cacheWriter.Flush();
        }

        public static void TryShim(string path, Action<string> logMessage = null, ReaderParameters readerParameters = null)
        {
            var lastWriteTime = File.GetLastWriteTimeUtc(path).Ticks;
            if (shimCache.TryGetValue(path, out var cachedWriteTime) && cachedWriteTime == lastWriteTime)
                return;
            try
            {
                // Read via MemoryStream to prevent sharing violation
                // This is only a problem on the first run; the cache prevents this from happening often
                using var ms = new MemoryStream(File.ReadAllBytes(path));
                using var ad = AssemblyDefinition.ReadAssembly(ms, readerParameters ?? new ReaderParameters());
                var harmonyRef = ad.MainModule.AssemblyReferences.FirstOrDefault(a => a.Name == "0Harmony");
                if (harmonyRef != null)
                {
                    var assToLoad = Assemblies.LastOrDefault(kv => kv.Key <= harmonyRef.Version);
                    if (assToLoad.Value != null)
                    {
                        logMessage?.Invoke($"Shimming {path} to use older version of Harmony ({assToLoad.Value}). Please update the plugin if possible.");
                        harmonyRef.Name = assToLoad.Value;
                        // Write via intermediate MemoryStream to prevent DLL corruption
                        using var outputMs = new MemoryStream();
                        ad.Write(outputMs);
                        File.WriteAllBytes(path, outputMs.ToArray());
                        lastWriteTime = File.GetLastWriteTimeUtc(path).Ticks;
                    }
                }

                shimCache[path] = lastWriteTime;
                cacheWriter.Write(path);
                cacheWriter.Write(lastWriteTime);
                cacheWriter.Flush();
            }
            catch (Exception e)
            {
                logMessage?.Invoke($"Failed to shim {path}: {e}");
            }
        }

        public static void ApplyPatch(MethodBase target, PatchInfoWrapper info)
        {
            var pInfo = target.ToPatchInfo();
            lock (pInfo)
            {
                pInfo.prefixes = Sync(info.prefixes, pInfo.prefixes);
                pInfo.postfixes = Sync(info.postfixes, pInfo.postfixes);
                pInfo.transpilers = Sync(info.transpilers.Select(p => new PatchMethod
                {
                    after = p.after,
                    before = p.before,
                    method = TranspilerInterop.WrapInterop(p.method),
                    owner = p.owner,
                    priority = p.priority
                }).ToArray(), pInfo.transpilers);
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