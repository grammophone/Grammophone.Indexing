using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Grammophone.Indexing
{
	/// <summary>
	/// Represents a generalized suffix tree of characters
	/// of generic type <typeparamref name="C"/> capable of computing
	/// the kernel between its contents and a given word.
	/// </summary>
	/// <typeparam name="C">The type of the character of a word.</typeparam>
	/// <typeparam name="D">
	/// The type of information allocated per word. Must at least provide a Weight field
	/// by implementing <see cref="KernelDataTypes.IWordItem"/>.
	/// </typeparam>
	/// <typeparam name="N">
	/// The type of extra data stored per node.
	/// Must at least implement <see cref="KernelDataTypes.INodeData"/> in order to allow
	/// kernel computations.
	/// </typeparam>
	[Serializable]
	public class KernelSuffixTree<C, D, N> : SuffixTree<C, D, N>, IDeserializationCallback
		where D : KernelDataTypes.IWordItem
		where N : KernelDataTypes.INodeData
	{
		#region Private fields

		private KernelWeightFunctions.WeightFunction weightFunction;

		private static readonly KernelWordItemProcessor<C, D, N> defaultWordItemProcessor = 
			new KernelWordItemProcessor<C, D, N>();

		[NonSerialized]
		private object preprocessLock;

		#endregion

		#region Construction

		/// <summary>
		/// Create.
		/// </summary>
		/// <param name="weightFunction">
		/// The weight function used for kernel computation.
		/// </param>
		/// <param name="wordItemProcessor">
		/// 
		/// </param>
		/// <remarks>
		/// Stock weight functions are provided in the <see cref="Grammophone.Indexing.KernelWeightFunctions"/> namespace.
		/// </remarks>
		public KernelSuffixTree(
			KernelWeightFunctions.WeightFunction weightFunction, 
			KernelWordItemProcessor<C, D, N> wordItemProcessor = null)
			: base(wordItemProcessor ?? defaultWordItemProcessor)
		{
			if (weightFunction == null) throw new ArgumentNullException("weightFunction");

			this.preprocessLock = new object();
			this.weightFunction = weightFunction;
		}

		#endregion

		#region Public properties

		/// <summary>
		/// This is false after any tree change and before <see cref="Preprocess"/> or
		/// <see cref="ComputeKernel"/> is called.
		/// </summary>
		public bool IsPreprocessed { get; private set; }

		#endregion

		#region Public methods

		/// <summary>
		/// Add a word to the tree.
		/// </summary>
		/// <param name="word">The word to add.</param>
		/// <param name="wordItem">
		/// The info associated with the word. 
		/// Contains the weight associated with the word.
		/// </param>
		/// <remarks>
		/// In order for the tree to be non-implicit, ie. to have a leaf for each word, the word
		/// must have as its last 'character' a character that doesn't belong to the
		/// 'character set' or 'alphabet'. This is frequently called a sentinel, or a termination
		/// special character, and is usually depicted in various papers as '$'.
		/// Time and space complexity: O(<paramref name="word"/>.Length).
		/// </remarks>
		public override void AddWord(C[] word, D wordItem)
		{
			if (word == null) throw new ArgumentNullException("word");

			this.IsPreprocessed = false;

			base.AddWord(word, wordItem);
		}

		/// <summary>
		/// Preprocess the tree for kernel computations.
		/// This is automatically called by <see cref="ComputeKernel"/>,
		/// if not already done manually.
		/// </summary>
		public void Preprocess()
		{
			lock (this.preprocessLock)
			{
				if (this.IsPreprocessed) return;

				this.PostOrderProcess(
					this.Root,
					branch => !branch.Children.Any() ? branch.NodeData.DescendantLeavesSum : 0.0,
					(value1, value2) => value1 + value2,
					delegate(RadixTree<C, D, N>.Branch branch, double value)
					{
						int startLength = branch.StartIndex - branch.WordStartIndex + 1;
						int endLength = startLength + branch.Length;

						branch.NodeData.DescendantLeavesSum = value;
						branch.NodeData.Weight = value * weightFunction.ComputeWeight(startLength, endLength);
					}
				);

				this.PreOrderProcess(
					this.Root,
					branch => branch.NodeData.Weight,
					(value1, value2) => value1 + value2,
					delegate(RadixTree<C, D, N>.Branch branch, double value)
					{
						branch.NodeData.Weight = value;
					}
				);

				this.IsPreprocessed = true;
			}
		}

		/// <summary>
		/// Compute the kernel between the given word and the tree's contents.
		/// </summary>
		/// <param name="word">The given word.</param>
		/// <returns>Returns the kernel value.</returns>
		/// <remarks>
		/// This calls automatically <see cref="Preprocess"/> if not already done so.
		/// </remarks>
		public double ComputeKernel(C[] word)
		{
			if (word == null) throw new ArgumentNullException("word");

			this.Preprocess();

			var matchingStatistics = this.GetMatchingStatistics(word);

			double accumulator = 0.0;

			foreach (var entry in matchingStatistics)
			{
				if (entry.Length == 0) continue;

				/* Implementation which adds from Floor. */

				int endLength = entry.Length + 1;

				int startLength = endLength - entry.Node.Offset;

				accumulator +=
					entry.Floor.Branch.NodeData.Weight + entry.Ceil.Branch.NodeData.DescendantLeavesSum * weightFunction.ComputeWeight(startLength, endLength);
			}

			return accumulator;
		}

		/// <summary>
		/// Clear the contents of the tree.
		/// </summary>
		/// <remarks>
		/// Only the <see cref="RadixTree{C, D, N}.Root"/> remains after the call.
		/// </remarks>
		public override void Clear()
		{
			this.IsPreprocessed = false;

			base.Clear();
		}

		#endregion


		#region IDeserializationCallback Members

		void IDeserializationCallback.OnDeserialization(object sender)
		{
			this.preprocessLock = new object();
		}

		#endregion
	}

	/// <summary>
	/// Represents a generalized suffix tree of characters
	/// of generic type <typeparamref name="C"/> capable of computing
	/// the kernel between its contents and a given word.
	/// </summary>
	/// <typeparam name="C">The type of the character.</typeparam>
	[Serializable]
	public class KernelSuffixTree<C> : KernelSuffixTree<C, KernelDataTypes.WordItem, KernelDataTypes.NodeData>
	{
		/// <summary>
		/// Create.
		/// </summary>
		/// <param name="weightFunction">
		/// The weight function used for kernel computation.
		/// </param>
		/// <remarks>
		/// Stock weight functions are provided in the <see cref="Grammophone.Indexing.KernelWeightFunctions"/> namespace.
		/// </remarks>
		public KernelSuffixTree(KernelWeightFunctions.WeightFunction weightFunction)
			: base(weightFunction)
		{
		}
	}
}
