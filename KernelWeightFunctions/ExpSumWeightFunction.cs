using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gramma.Indexing.KernelWeightFunctions
{
	/// <summary>
	/// Implies a weight of λ^|s| for string lengths |s|.
	/// </summary>
	[Serializable]
	public class ExpSumWeightFunction : WeightFunction
	{
		/// <summary>
		/// Create.
		/// </summary>
		/// <param name="λ">The base in λ^|s| expression.</param>
		public ExpSumWeightFunction(double λ)
		{
			this.λ = λ;
		}

		/// <summary>
		/// The base in λ^|s| expression.
		/// </summary>
		public double λ { get; private set; }

		/// <summary>
		/// Implies a weight of λ^|s| for string lengths |s|.
		/// </summary>
		public override double ComputeWeight(int startLength, int endLength)
		{
			return (Math.Pow(λ, startLength) - Math.Pow(λ, endLength)) / (1.0 - λ);
		}
	}
}
