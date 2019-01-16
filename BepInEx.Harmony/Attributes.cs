using System;

namespace BepInEx.Harmony
{
	[AttributeUsage(AttributeTargets.Method)]
	public class ParameterByRefAttribute : Attribute
	{
		public int[] ParameterIndices { get; }

		public ParameterByRefAttribute(params int[] parameterIndices)
		{
			ParameterIndices = parameterIndices;
		}
	}
}