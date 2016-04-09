using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Grammophone.Indexing.KernelWeightFunctions
{
	/// <summary>
	/// Implies a weight of one for all string lengths.
	/// </summary>
	[Serializable]
	public class SumWeightFunction : WeightFunction
	{
		/// <summary>
		/// Implies every substring occurrence having weight equal to one.
		/// </summary>
		public override double ComputeWeight(int startLength, int endLength)
		{
			return endLength - startLength;
		}
	}
}
