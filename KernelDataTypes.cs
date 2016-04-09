using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Grammophone.Indexing
{
	/// <summary>
	/// Contracts and data types used in KernelSuffixTree classes.
	/// </summary>
	public static class KernelDataTypes
	{
		/// <summary>
		/// The node data of the KernelSuffixTree, which is type parameter N, must at least implement
		/// this interface.
		/// </summary>
		public interface INodeData
		{
			/// <summary>
			/// The weighted sum of the leaves item values that are descendants of the node.
			/// </summary>
			double DescendantLeavesSum { get; set; }

			/// <summary>
			/// The accumulated weight of all the substrings starting from length zero up to 
			/// the end of the branch.
			/// </summary>
			double Weight { get; set; }
		}

		/// <summary>
		/// An implementation of the <see cref="INodeData"/> interface in the form of a struct
		/// to conserve heap and space overhead.
		/// </summary>
		[Serializable]
		public struct NodeData : INodeData
		{
			/// <summary>
			/// The weighted sum of the leaves values that are descendants of the node.
			/// </summary>
			public double DescendantLeavesSum { get; set; }

			/// <summary>
			/// The accumulated weight of all the substrings starting from length zero up to 
			/// the end of the branch.
			/// </summary>
			public double Weight { get; set; }
		}

		/// <summary>
		/// The items that tag the words of a KernelSuffixTree, which are of type parameter D, 
		/// must at least implement this interface in order to provide a word weight.
		/// </summary>
		public interface IWordItem
		{
			/// <summary>
			/// The weight associated with a word.
			/// </summary>
			double Weight { get; set; }
		}

		/// <summary>
		/// An implementation of the <see cref="IWordItem"/> interface in the form of a struct
		/// to conserve heap and space overhead. This is implicitly convertible
		/// from and to double since the only property is <see cref="WordItem.Weight"/>
		/// of type double.
		/// </summary>
		[Serializable]
		public struct WordItem : IWordItem
		{
			/// <summary>
			/// The weight associated with a word.
			/// </summary>
			public double Weight { get; set; }

			/// <summary>
			/// Implicit conversion from double into a <see cref="WordItem"/>
			/// having property <see cref="Weight"/> equal to the supplied value.
			/// </summary>
			public static implicit operator WordItem(double weight)
			{
				return new WordItem() { Weight = weight };
			}

			/// <summary>
			/// Implicit conversion from <see cref="WordItem"/> to double
			/// of value equal to the <see cref="Weight"/> property.
			/// </summary>
			public static implicit operator double(WordItem wordItem)
			{
				return wordItem.Weight;
			}
		}
	}
}
