using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gramma.Indexing
{
	/// <summary>
	/// Strategy for addition of word items into the <see cref="RadixTree{C, D, N}.Branch.NodeData"/>
	/// of a radix tree branch through <see cref="IWordItemStorage{D}"/> interface.
	/// </summary>
	/// <typeparam name="C">The type of the character of a word.</typeparam>
	/// <typeparam name="D">The type of information allocated per word.</typeparam>
	/// <typeparam name="N">
	/// The type of extra data stored per node, 
	/// expected to implement <see cref="IWordItemStorage{D}"/>.
	/// </typeparam>
	[Serializable]
	public class StorageWordItemProcessor<C, D, N> : WordItemProcessor<C, D, N>
		where N: IWordItemStorage<D>
	{
		/// <summary>
		/// Adds the <paramref name="wordItem"/> to the <see cref="RadixTree{C, D, N}.Branch.NodeData"/>
		/// field of the <paramref name="branch"/>.
		/// </summary>
		/// <remarks>
		/// The addition is accomplished through the <see cref="IWordItemStorage{D}"/> interface.
		/// </remarks>
		public override void OnWordAdd(C[] str, D wordItem, RadixTree<C, D, N>.Branch branch)
		{
			branch.NodeData.AddWordItem(wordItem);
		}
	}
}
