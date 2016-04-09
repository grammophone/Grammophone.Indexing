using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Grammophone.Indexing
{
	/// <summary>
	/// A radix tree containing words of 
	/// character type <typeparamref name="C"/>.
	/// </summary>
	/// <typeparam name="C">The type of the character.</typeparam>
	/// <typeparam name="D">The type of information allocated per word.</typeparam>
	/// <typeparam name="N">The type of extra data stored per node.</typeparam>
	[Serializable]
	public class WordTree<C, D, N> : RadixTree<C, D, N>
	{
		#region Construction

		/// <summary>
		/// Create.
		/// </summary>
		/// <param name="wordItemProcessor">The optional word item processor to use during word addition.</param>
		public WordTree(WordItemProcessor<C, D, N> wordItemProcessor = null)
			: base(wordItemProcessor)
		{
		}

		#endregion

		#region Public methods

		/// <summary>
		/// Add a word to the tree.
		/// </summary>
		/// <param name="word">The word to add.</param>
		/// <param name="wordItem">The info associated with the word.</param>
		/// <remarks>
		/// In order for the tree to be non-implicit, ie. to have a leaf for each word, the word
		/// must have as its last 'character' a character that doesn't belong to the
		/// 'character set' or 'alphabet'. This is frequently called a sentinel, or a termination
		/// special character, and is usually depicted in various papers as '$'.
		/// Time and space complexity: O(<paramref name="word"/>.Length).
		/// </remarks>
		public override void AddWord(C[] word, D wordItem)
		{
			// Search for the longest available common match
			// betwen the word and the tree contents.
			var searchResult = this.LongestCommonPrefixSearch(word);

			// Did we find any significant match?
			// Equivalently, is the found branch other than the root?

			var branch = searchResult.Branch;

			if (branch != this.Root)
			{
				// Yes, at least a common prefix was found.
				// Was the whole word found?
				if (searchResult.Match.Length == word.Length)
				{
					// Yes, the whole word was matched.

					// Does the match terminate at branch boundary?
					if (searchResult.MatchEndOffset != branch.Length)
					{
						// No, so there is a need to create an explicit node by
						// breaking the branch in two.
						branch = searchResult.Branch.Split(searchResult.MatchEndOffset);
					}

					// Add the information that corresponds to the given word.
					wordItemProcessor.OnWordAdd(word, wordItem, branch);
				}
				else
				{
					// No, just part of the word was matched against the existing
					// contents of the tree.

					// Does the match terminate at branch boundary?
					if (searchResult.MatchEndOffset != branch.Length)
					{
						// No, so there is a need to create an explicit node
						// breaking the branch in two.
						branch = searchResult.Branch.Split(searchResult.MatchEndOffset);
					}

					AddBranch(word, wordItem, branch, branch.StartIndex + searchResult.MatchEndOffset);
				}
			}
			else
			{
				// No, there is no match with existing content. Append a branch at root.
				AddBranch(word, wordItem, this.Root, 0);
			}

			/* INLINE IMPLEMENTATION */
			//// The current branch where we try to match the current character.
			//var branch = this.Root;

			//// The offset in the current branch where we 
			//// compare our current character.
			//int branchOffset = 0;

			//for (int i = 0; i < word.Length; i++)
			//{
			//  // Our current character.
			//  C chr = word[i];

			//  // Did we reach the end of the branch?
			//  if (branchOffset >= branch.Length)
			//  {
			//    // Yes, so try to find whether there is an outgoing branch
			//    // starting with the current character.
			//    var nextBranch = branch.GetChildStartingWith(chr);

			//    // Is there such a branch?
			//    if (nextBranch == null)
			//    {
			//      // No, append a branch for the rest of the word and return.
			//      AddBranch(word, wordItem, branch, i);

			//      return;
			//    }

			//    // Yes, a branch starting with the current character is found,
			//    // so, continue from there.
			//    // Take account of the found character by setting branchOffset
			//    // equal to one.
			//    branch = nextBranch;
			//    branchOffset = 1;
			//  }
			//  else
			//  {
			//    // No, our position is somewhere in the middle of the current branch.
			//    // Does our character match with the one at the position of the branch?
			//    if (!branch.Source[branch.StartIndex + branchOffset].Equals(chr))
			//    {
			//      // If not, split the branch and
			//      // append a new branch for the rest of the word at the split position
			//      // and return.
			//      branch.Split(branchOffset);
			//      AddBranch(word, wordItem, branch, i);

			//      return;
			//    }
			//    else
			//    {
			//      // If yes, advance our search position
			//      // in the current branch for the next character test.
			//      branchOffset++;
			//    }
			//  }
			//}

			//// If we reached this point, there was a total path overlap 
			//// of the word against the preexisting tree.

			//// If the overlap ended in the middle of a branch...
			//if (branchOffset < branch.Length)
			//{
			//  // ...split the branch where the new word ended, so we can
			//  // add later the word item at the split point.
			//  branch.Split(branchOffset);
			//}

			//// Add the word item at the branch end, which is by now 
			//// the word path termination point.
			//branch.AddWordItem(wordItem);
		}

		#endregion

		#region Private methods

		private Branch AddBranch(C[] word, D wordItem, Branch branch, int wordOffset)
		{
			var nextBranch = new Branch(word, wordOffset, 0);
			
			branch.AddChild(nextBranch);

			wordItemProcessor.OnWordAdd(word, wordItem, nextBranch);

			return nextBranch;
		}

		#endregion
	}
}
