using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gramma.Indexing
{
	/// <summary>
	/// Strategy for addition of word items for <see cref="KernelSuffixTree{C, D, N}"/>.
	/// </summary>
	/// <typeparam name="C">The type of character of a word.</typeparam>
	/// <typeparam name="D">The word item, implementing <see cref="KernelDataTypes.IWordItem"/>.</typeparam>
	/// <typeparam name="N">The type of extra data stored per node, implementing <see cref="KernelDataTypes.INodeData"/>.</typeparam>
	[Serializable]
	public class KernelWordItemProcessor<C, D, N> : WordItemProcessor<C, D, N>
		where D: KernelDataTypes.IWordItem
		where N: KernelDataTypes.INodeData
	{
		/// <summary>
		/// Uses the <see cref="KernelDataTypes.IWordItem.Weight"/> property of <paramref name="wordItem"/>
		/// to update <see cref="KernelDataTypes.INodeData.DescendantLeavesSum"/> 
		/// property of <see cref="RadixTree{C, D, N}.Branch"/>.
		/// </summary>
		public override void OnWordAdd(C[] str, D wordItem, RadixTree<C, D, N>.Branch branch)
		{
			branch.NodeData.DescendantLeavesSum += wordItem.Weight;
		}
	}
}
