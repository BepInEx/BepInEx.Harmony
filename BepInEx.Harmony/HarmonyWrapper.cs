using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace BepInEx.Harmony
{
	/// <summary>
	/// A wrapper for Harmony based operations.
	/// </summary>
	public partial class HarmonyWrapper
	{
		/// <summary>
		/// Applies all patches specified in the type.
		/// </summary>
		/// <param name="type">The type to scan.</param>
		/// <param name="harmonyInstance">The HarmonyInstance to use.</param>
		public static HarmonyLib.Harmony PatchAll(Type type, HarmonyLib.Harmony harmonyInstance = null)
		{
			var instance = harmonyInstance ?? new HarmonyLib.Harmony($"harmonywrapper-auto-{Guid.NewGuid()}");

			type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Do(method =>
			{
				var patchAttributeMethods = HarmonyMethodExtensions.GetFromMethod(method);
				if (patchAttributeMethods != null && patchAttributeMethods.Any())
				{
					var attributes = method.GetCustomAttributes(true);

					var combinedInfo = HarmonyMethod.Merge(patchAttributeMethods);

					if (attributes.Any(x => x is ParameterByRefAttribute))
					{
						var byRefAttribute = (ParameterByRefAttribute)attributes.First(x => x is ParameterByRefAttribute);

						foreach (int index in byRefAttribute.ParameterIndices)
						{
							combinedInfo.argumentTypes[index] = combinedInfo.argumentTypes[index].MakeByRefType();
						}
					}

					HarmonyMethod prefix = null;
					HarmonyMethod transpiler = null;
					HarmonyMethod postfix = null;

					if (attributes.Any(x => x is HarmonyPrefix))
						prefix = new HarmonyMethod(method);

					if (attributes.Any(x => x is HarmonyTranspiler))
						transpiler = new HarmonyMethod(method);

					if (attributes.Any(x => x is HarmonyPostfix))
						postfix = new HarmonyMethod(method);


					List<HarmonyMethod> completeMethods = patchAttributeMethods.Where(x => x.declaringType != null && x.methodName != null).ToList();

					if (patchAttributeMethods.All(x => x.declaringType != combinedInfo.declaringType && x.methodName != combinedInfo.methodName))
						completeMethods.Add(combinedInfo);

					List<MethodBase> originalMethods = new List<MethodBase>();

					foreach (var methodToPatch in completeMethods)
					{
						if (!methodToPatch.methodType.HasValue)
							methodToPatch.methodType = MethodType.Normal;

						var originalMethod = GetOriginalMethod(methodToPatch);

						if (originalMethod == null)
							throw new ArgumentException($"Null method for attribute: \n" +
														$"Type={methodToPatch.declaringType.FullName ?? "<null>"}\n" +
														$"Name={methodToPatch.methodName ?? "<null>"}\n" +
														$"MethodType={(methodToPatch.methodType.HasValue ? methodToPatch.methodType.Value.ToString() : "<null>")}\n" +
														$"Args={(methodToPatch.argumentTypes == null ? "<null>" : string.Join(",", methodToPatch.argumentTypes.Select(x => x.FullName).ToArray()))}");

						originalMethods.Add(originalMethod);
					}

					var processor = new PatchProcessor(instance, originalMethods, prefix, postfix, transpiler);
					processor.Patch();
				}
			});

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
			=> PatchAll(new HarmonyLib.Harmony(harmonyInstanceId));

		
		private static MethodBase GetOriginalMethod(HarmonyMethod attribute)
		{
			if (attribute.declaringType == null)
				return null;

			switch (attribute.methodType)
			{
				case MethodType.Normal:
					if (attribute.methodName == null)
						return null;
					return AccessTools.DeclaredMethod(attribute.declaringType, attribute.methodName, attribute.argumentTypes);

				case MethodType.Getter:
					if (attribute.methodName == null)
						return null;
					return AccessTools.DeclaredProperty(attribute.declaringType, attribute.methodName)
									  .GetGetMethod(true);

				case MethodType.Setter:
					if (attribute.methodName == null)
						return null;
					return AccessTools.DeclaredProperty(attribute.declaringType, attribute.methodName)
									  .GetSetMethod(true);

				case MethodType.Constructor:
					return AccessTools.GetDeclaredConstructors(attribute.declaringType)
									  .FirstOrDefault((ConstructorInfo c) =>
									  {
									  	if (c.IsStatic)
									  	{
									  		return false;
									  	}
									  	var parameters = c.GetParameters();
									  	if (attribute.argumentTypes == null && parameters.Length == 0)
										{
											return true;
										}
									  	return parameters
									  		.Select((p) => p.ParameterType)
									  		.SequenceEqual(attribute.argumentTypes);
									  });

				case MethodType.StaticConstructor:
					return AccessTools.GetDeclaredConstructors(attribute.declaringType)
									  .FirstOrDefault(c => c.IsStatic);
			}

			return null;
		}
	}
}
