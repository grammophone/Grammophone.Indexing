using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Grammophone.Indexing
{
	/// <summary>
	/// Strategy for additional storage action to take place when a word is added to a radix tree branch.
	/// </summary>
	/// <typeparam name="C">The type of the character of a word.</typeparam>
	/// <typeparam name="D">The type of information allocated per word.</typeparam>
	/// <typeparam name="N">The type of extra data stored per node.</typeparam>
	/// <remarks>
	/// The strategy typically takes advantage of the 
	/// field <see cref="RadixTree{C, D, N}.Branch.NodeData"/> of type <typeparamref name="N"/>, 
	/// but it is not required.
	/// </remarks>
	[Serializable]
	public abstract class WordItemProcessor<C, D, N>
	{
		/// <summary>
		/// The additional storage action to take place when a word is added to a branch.
		/// </summary>
		/// <param name="str">The added word.</param>
		/// <param name="wordItem">The information accompanying the word.</param>
		/// <param name="branch">The branch where the word is allocated.</param>
		/// <remarks>
		/// The method typically takes advantage 
		/// of the <see cref="RadixTree{C, D, N}.Branch.NodeData"/> field of the <paramref name="branch"/>, 
		/// but it is not required.
		/// </remarks>
		public abstract void OnWordAdd(C[] str, D wordItem, RadixTree<C, D, N>.Branch branch);
	}
}
