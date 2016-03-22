using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gramma.Indexing.KernelWeightFunctions
{
	/// <summary>
	/// Base class for computing weights in 
	/// string kernels using <see cref="KernelSuffixTree{C, D, N}"/>.
	/// </summary>
	[Serializable]
	public abstract class WeightFunction
	{
		/// <summary>
		/// Weight function implementation, which must provide a telescopic sum
		/// of string weights based on their lengths. Must be O(1) to preserve
		/// the O(m+n) complexity of the kernel computation. See remarks.
		/// </summary>
		/// <param name="startLength">The string start length, INCLUSIVE.</param>
		/// <param name="endLength">The string end length, EXCLUSIVE.</param>
		/// <returns>
		/// Returns the sum of the weights.
		/// </returns>
		/// <remarks>
		/// Must employ a telescopic mathematic technique or any other O(1) means
		/// in order to preserve the O(m) complexity of tree preprocessing, where
		/// m is the sum of the word lengths in the tree. The O(m) preprocessing time
		/// guarantees the O(m+n) kernel computation time for the first kernel computation
		/// (which includes the O(m) tree bulding and O(m) preprocessing)
		/// and O(n) for subsequent computations (as building and preprocessing 
		/// are already done), where n is the length of the
		/// word against which the contents of the tree are kernelized.
		/// </remarks>
		public abstract double ComputeWeight(int startLength, int endLength);

		/// <summary>
		/// Returns a weight function of λ^|s| for string lengths |s|.
		/// </summary>
		public static WeightFunction ExpSum(double λ)
		{
			if (Math.Abs(λ - 1.0) < 1e-6) return new SumWeightFunction();

			return new ExpSumWeightFunction(λ);
		}
	}
}
