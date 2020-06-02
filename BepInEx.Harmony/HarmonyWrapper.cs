using System;
using System.Reflection;
using HarmonyLib;

namespace BepInEx.Harmony
{
	/// <summary>
	/// A wrapper for Harmony based operations.
	/// </summary>
	public class HarmonyWrapper
	{
		/// <summary>
		/// Applies all patches specified in the type.
		/// </summary>
		/// <param name="type">The type to scan.</param>
		/// <param name="harmonyInstance">The HarmonyInstance to use.</param>
		public static HarmonyLib.Harmony PatchAll(Type type, HarmonyLib.Harmony harmonyInstance = null)
		{
			var instance = harmonyInstance ?? new HarmonyLib.Harmony($"harmonywrapper-auto-{Guid.NewGuid()}");
			instance.PatchAll(type);
				return instance;
		}

		/// <summary>
		/// Applies all patches specified in the type.
		/// </summary>
		/// <param name="type">The type to scan.</param>
		/// <param name="harmonyInstanceId">The ID for the Harmony instance to create, which will be used.</param>
		public static HarmonyLib.Harmony PatchAll(Type type, string harmonyInstanceId)
			=> PatchAll(type, new HarmonyLib.Harmony(harmonyInstanceId));


		/// <summary>
		/// Applies all patches specified in the assembly.
		/// </summary>
		/// <param name="assembly">The assembly to scan.</param>
		/// <param name="harmonyInstance">The HarmonyInstance to use.</param>
		public static HarmonyLib.Harmony PatchAll(Assembly assembly, HarmonyLib.Harmony harmonyInstance = null)
		{
			var instance = harmonyInstance ?? new HarmonyLib.Harmony($"harmonywrapper-auto-{Guid.NewGuid()}");

			foreach (var type in assembly.GetTypes())
				PatchAll(type, instance);

			return instance;
		}


		/// <summary>
		/// Applies all patches specified in the assembly.
		/// </summary>
		/// <param name="assembly">The assembly to scan.</param>
		/// <param name="harmonyInstanceId">The ID for the Harmony instance to create, which will be used.</param>
		public static HarmonyLib.Harmony PatchAll(Assembly assembly, string harmonyInstanceId)
			=> PatchAll(assembly, new HarmonyLib.Harmony(harmonyInstanceId));


		/// <summary>
		/// Applies all patches specified in the calling assembly.
		/// </summary>
		/// <param name="harmonyInstance">The Harmony instance to use.</param>
		public static HarmonyLib.Harmony PatchAll(HarmonyLib.Harmony harmonyInstance = null)
			=> PatchAll(Assembly.GetCallingAssembly(), harmonyInstance);


		/// <summary>
		/// Applies all patches specified in the calling assembly.
		/// </summary>
		/// <param name="harmonyInstanceId">The ID for the Harmony instance to create, which will be used.</param>
		public static HarmonyLib.Harmony PatchAll(string harmonyInstanceId)
			=> PatchAll(Assembly.GetCallingAssembly(), harmonyInstanceId);

		/// <summary>
		/// Returns an instruction to call the specified delegate.
		/// </summary>
		/// <typeparam name="T">The delegate type to emit.</typeparam>
		/// <param name="action">The delegate to emit.</param>
		/// <returns>The instruction to </returns>
		public static CodeInstruction EmitDelegate<T>(T action) where T : Delegate
		{
			return Transpilers.EmitDelegate(action);
		}
	}
}