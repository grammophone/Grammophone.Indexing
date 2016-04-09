using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Grammophone.Indexing
{
	/// <summary>
	/// Defitinion for storage of word information.
	/// </summary>
	/// <typeparam name="D">The type of a word item.</typeparam>
	public interface IWordItemStorage<D>
	{
		/// <summary>
		/// Add a word information to the storage.
		/// </summary>
		/// <param name="wordItem">The word information to add.</param>
		void AddWordItem(D wordItem);

		/// <summary>
		/// The word items stored.
		/// </summary>
		IEnumerable<D> WordItems { get; }
	}
}
