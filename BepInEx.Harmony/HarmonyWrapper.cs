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
		[Obsolete("Use HarmonyLib.Harmony.CreateAndPatchAll or HarmonyLib.Harmony.PatchAll")]
		public static HarmonyLib.Harmony PatchAll(Type type, HarmonyLib.Harmony harmonyInstance = null)
		{
			if (harmonyInstance == null)
				return HarmonyLib.Harmony.CreateAndPatchAll(type);
			harmonyInstance.PatchAll(type);
			return harmonyInstance;
		}

		/// <summary>
		/// Applies all patches specified in the type.
		/// </summary>
		/// <param name="type">The type to scan.</param>
		/// <param name="harmonyInstanceId">The ID for the Harmony instance to create, which will be used.</param>
		[Obsolete("Use HarmonyLib.Harmony.CreateAndPatchAll")]
		public static HarmonyLib.Harmony PatchAll(Type type, string harmonyInstanceId)
			=> HarmonyLib.Harmony.CreateAndPatchAll(type, harmonyInstanceId);


		/// <summary>
		/// Applies all patches specified in the assembly.
		/// </summary>
		/// <param name="assembly">The assembly to scan.</param>
		/// <param name="harmonyInstance">The HarmonyInstance to use.</param>
		[Obsolete("Use HarmonyLib.Harmony.CreateAndPatchAll or HarmonyLib.Harmony.PatchAll")]
		public static HarmonyLib.Harmony PatchAll(Assembly assembly, HarmonyLib.Harmony harmonyInstance = null)
		{
			if(harmonyInstance == null)
				return HarmonyLib.Harmony.CreateAndPatchAll(assembly);
			harmonyInstance.PatchAll(assembly);
			return harmonyInstance;
		}


		/// <summary>
		/// Applies all patches specified in the assembly.
		/// </summary>
		/// <param name="assembly">The assembly to scan.</param>
		/// <param name="harmonyInstanceId">The ID for the Harmony instance to create, which will be used.</param>
		[Obsolete("Use HarmonyLib.Harmony.CreateAndPatchAll")]
		public static HarmonyLib.Harmony PatchAll(Assembly assembly, string harmonyInstanceId)
			=> HarmonyLib.Harmony.CreateAndPatchAll(assembly, harmonyInstanceId);


		/// <summary>
		/// Applies all patches specified in the calling assembly.
		/// </summary>
		/// <param name="harmonyInstance">The Harmony instance to use.</param>
		[Obsolete("Use HarmonyLib.Harmony.PatchAll with no arguments")]
		public static HarmonyLib.Harmony PatchAll(HarmonyLib.Harmony harmonyInstance = null)
			=> PatchAll(Assembly.GetCallingAssembly(), harmonyInstance);


		/// <summary>
		/// Applies all patches specified in the calling assembly.
		/// </summary>
		/// <param name="harmonyInstanceId">The ID for the Harmony instance to create, which will be used.</param>
		[Obsolete("Use HarmonyLib.Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), ...)")]
		public static HarmonyLib.Harmony PatchAll(string harmonyInstanceId)
			=> PatchAll(Assembly.GetCallingAssembly(), harmonyInstanceId);

		/// <summary>
		/// Returns an instruction to call the specified delegate.
		/// </summary>
		/// <typeparam name="T">The delegate type to emit.</typeparam>
		/// <param name="action">The delegate to emit.</param>
		/// <returns>The instruction to </returns>
		[Obsolete("Use HarmonyLib.Transpilers.EmitDelegate instead")]
		public static CodeInstruction EmitDelegate<T>(T action) where T : Delegate
		{
			return Transpilers.EmitDelegate(action);
		}
	}
}