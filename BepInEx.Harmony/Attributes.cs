using System;

namespace BepInEx.Harmony
{
	/// <summary>
	/// Specifies the indices of parameters that are ByRef.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	public class ParameterByRefAttribute : Attribute
	{
		/// <summary>
		/// The indices of parameters that are ByRef.
		/// </summary>
		public int[] ParameterIndices { get; }

		/// <param name="parameterIndices">The indices of parameters that are ByRef.</param>
		public ParameterByRefAttribute(params int[] parameterIndices)
		{
			ParameterIndices = parameterIndices;
		}
	}
}