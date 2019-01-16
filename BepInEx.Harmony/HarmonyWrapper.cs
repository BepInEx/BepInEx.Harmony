using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony;

namespace BepInEx.Harmony
{
    public class HarmonyWrapper
    {
		public static HarmonyInstance DefaultInstance { get; } = HarmonyInstance.Create("bepinex.default");

		public static void PatchAll(Type type, HarmonyInstance harmonyInstance = null)
		{
			var instance = harmonyInstance ?? DefaultInstance;

			type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Do(method =>
			{
				var parentMethodInfos = method.GetHarmonyMethods();
				if (parentMethodInfos != null && parentMethodInfos.Any())
				{
					var attributes = method.GetCustomAttributes(true);

					var info = HarmonyMethod.Merge(parentMethodInfos);

					HarmonyMethod prefix = null;
					HarmonyMethod transpiler = null;
					HarmonyMethod postfix = null;

					if (attributes.Any(x => x is HarmonyPrefix))
						prefix = new HarmonyMethod(method);

					if (attributes.Any(x => x is HarmonyTranspiler))
						transpiler = new HarmonyMethod(method);

					if (attributes.Any(x => x is HarmonyPostfix))
						postfix = new HarmonyMethod(method);


					if (!info.methodType.HasValue)
						info.methodType = MethodType.Normal;


					var processor = new PatchProcessor(instance, new List<MethodBase> { GetOriginalMethod(info) }, prefix, postfix, transpiler);
					processor.Patch();
				}
			});
		}

		public static void PatchAll(Assembly assembly, HarmonyInstance harmonyInstance = null)
		{
			foreach (var type in assembly.GetTypes())
				PatchAll(type, harmonyInstance);
		}

		public static void PatchAll(HarmonyInstance harmonyInstance = null)
		{
			PatchAll(Assembly.GetCallingAssembly(), harmonyInstance);
		}

		private static MethodBase GetOriginalMethod(HarmonyMethod attribute)
		{
			if (attribute.declaringType == null) return null;

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
					return AccessTools.DeclaredConstructor(attribute.declaringType, attribute.argumentTypes);

				case MethodType.StaticConstructor:
					return AccessTools.GetDeclaredConstructors(attribute.declaringType)
									  .FirstOrDefault(c => c.IsStatic);
			}

			return null;
		}
	}
}