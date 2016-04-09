using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Grammophone.Indexing
{
	/// <summary>
	/// Represents a radix tree (also called patricia tree).
	/// This is the base abstraction upon which <see cref="SuffixTree{C, D, N}"/>
	/// and <see cref="WordTree{C, D, N}"/> are built.
	/// </summary>
	/// <typeparam name="C">The type of the character of a word.</typeparam>
	/// <typeparam name="D">The type of information allocated per word.</typeparam>
	/// <typeparam name="N">The type of extra data stored per node.</typeparam>
	[Serializable]
	public abstract class RadixTree<C, D, N>
	{
		#region Inner class definitions

		/// <summary>
		/// Represents a branch of the radix tree.
		/// </summary>
		[Serializable]
		public class Branch
		{
			#region Private fields

			/// <summary>
			/// The children of this branch keyed by their first 'character'.
			/// </summary>
			private Dictionary<C, Branch> children;

			/// <summary>
			/// An empty list of word items shared by the branches that
			/// don't terminate at word radixes.
			/// </summary>
			private static List<D> emptyWordItems = new List<D>();

			#endregion

			#region Construction

			/// <summary>
			/// Create.
			/// </summary>
			/// <param name="source">
			/// The pointer to the source string.
			/// </param>
			/// <param name="startIndex">
			/// The start index of the branch fragment in the <paramref name="source"/> string.
			/// </param>
			/// <param name="length">
			/// The length of the branch fragment in the <paramref name="source"/> string.
			/// A default value of -1 marks the end of the source.
			/// </param>
			/// <param name="wordStartIndex">
			/// The start of the full word within <paramref name="source"/>.
			/// </param>
			public Branch(C[] source, int startIndex, int wordStartIndex, int length = -1)
			{
				if (source == null) throw new ArgumentNullException("source");

				if (startIndex < 0)
					throw new ArgumentException("startIndex must be non negative.", "startIndex");

				if (length < -1)
					throw new ArgumentException("length must be greater than -2.", "endInex");

				if (startIndex + length > source.Length)
					throw new ArgumentException("startIndex + length must not exceed source's length.", "length");

				if (wordStartIndex < 0)
					throw new ArgumentException("wordStartIndex must not be negative.", "wordStartIndex");

				if (wordStartIndex > startIndex)
					throw new ArgumentException("wordStartIndex bust not be greater that startIndex", "wordStartIndex");

				this.Source = source;
				this.StartIndex = startIndex;
				this.Length = length >= 0 ? length : source.Length - startIndex;

				this.children = new Dictionary<C, Branch>();

				this.WordStartIndex = wordStartIndex;
			}

			#endregion

			#region Public properties

			/// <summary>
			/// The pointer to the source string.
			/// </summary>
			public C[] Source { get; internal set; }

			/// <summary>
			/// The start of the branch fragment index in the <see cref="Source"/> string.
			/// </summary>
			public int StartIndex { get; internal set; }

			/// <summary>
			/// The start of the full word within <see cref="Source"/>.
			/// </summary>
			public int WordStartIndex { get; internal set; }

			/// <summary>
			/// The length of the branch fragment in the <see cref="Source"/> string.
			/// </summary>
			public int Length { get; internal set; }

			/// <summary>
			/// The parent of this branch or null if it is root.
			/// </summary>
			public Branch Parent { get; internal set; }

			/// <summary>
			/// Return the first 'character' of the branch.
			/// </summary>
			public C FirstChar
			{
				get
				{
					if (this.Source == null || this.Length == 0) return default(C);
					return this.Source[this.StartIndex];
				}
			}

			/// <summary>
			/// Get the character at a given offset of the branch.
			/// The offset counting starts at zero and must me less
			/// than <see cref="Length"/>.
			/// </summary>
			/// <param name="charOffset">The offset.</param>
			/// <returns>Returns the corresponding character.</returns>
			public C this[int charOffset]
			{
				get
				{
					if (charOffset >= this.Length)
						throw new ArgumentException("charOffset must be less than length.");

					return this.Source[this.StartIndex + charOffset];
				}
			}

			/// <summary>
			/// The children of this branch, if any.
			/// </summary>
			public IEnumerable<Branch> Children
			{
				get
				{
					return this.children.Values;
				}
			}

			/// <summary>
			/// Returns true if this is the root branch.
			/// </summary>
			/// <remarks>
			/// This is determined from whether the length is zero.
			/// </remarks>
			public bool IsRoot
			{
				get
				{
					return this.Length == 0;
				}
			}

			/// <summary>
			/// Suffix link used for speedup of suffix tree construction.
			/// </summary>
			/// <remarks>
			/// Implies the node at the end of the branch.
			/// </remarks>
			public Branch SuffixLink { get; internal set; }

			/// <summary>
			/// Arbitrary node data that may be assigned to this branch.
			/// </summary>
			/// <remarks>
			/// These are extra data to annotate the nodes implied by branches
			/// for various algorithms use. Typical examples are DFS children preprocessing
			/// for string kernel computations.
			/// WARNING: This must be a public field, not a property, in order for 
			/// subproperties to function properly even when 
			/// type <typeparamref name="N"/> is a struct.
			/// </remarks>
			public N NodeData;

			/// <summary>
			/// This property will soon replace the <see cref="GetChars"/> method.
			/// </summary>
			public C[] Chars
			{
				get
				{
					return GetChars();
				}
			}

			/// <summary>
			/// This property will soon replace the <see cref="GetPathChars"/> method.
			/// </summary>
			public C[] PathChars
			{
				get
				{
					return GetPathChars();
				}
			}

			#endregion

			#region Public methods

			/// <summary>
			/// Determine if there is a branch branch starting
			/// with a given character <paramref name="chr"/>.
			/// </summary>
			/// <param name="chr">The character to test.</param>
			/// <returns>
			/// Returns true if there is a branch branch starting with
			/// <paramref name="chr"/>.
			/// </returns>
			public bool DoesChildStartWith(C chr)
			{
				return this.children.ContainsKey(chr);
			}

			/// <summary>
			/// Attempt to find the branch branch that starts with
			/// a given character <paramref name="chr"/>.
			/// </summary>
			/// <param name="chr">The character.</param>
			/// <returns>
			/// Returns the branch found, is such exists, else null.
			/// </returns>
			public Branch GetChildStartingWith(C chr)
			{
				Branch child = null;

				this.children.TryGetValue(chr, out child);

				return child;
			}

			/// <summary>
			/// Split this branch at a given offset.
			/// The current branch becomes the lower part.
			/// </summary>
			/// <param name="offset">The offset.</param>
			/// <returns>Returns the new higher part of the branch.</returns>
			public Branch Split(int offset)
			{
				if (offset <= 0)
					throw new ArgumentException("offset must be greater than zero.", "offset");

				if (offset >= this.Length)
					throw new ArgumentException("offset must be less than length.", "offset");

				Branch newUpperBranch = new Branch(this.Source, this.StartIndex, this.WordStartIndex, offset);

				Branch parent = this.Parent;

				if (parent != null)
				{
					parent.RemoveChild(this);

					parent.AddChild(newUpperBranch);
				}

				this.StartIndex += offset;
				this.Length -= offset;

				newUpperBranch.AddChild(this);

				return newUpperBranch;
			}

			/// <summary>
			/// Add a new child to the branch.
			/// </summary>
			/// <param name="newChild">The branch to add.</param>
			public void AddChild(Branch newChild)
			{
				if (newChild == null) throw new ArgumentNullException("newChild");

				if (this.DoesChildStartWith(newChild.FirstChar))
					throw new ArgumentException(
						"There is already a child starting with the branch's first character",
						"newChild");

				newChild.Parent = this;
				this.children.Add(newChild.FirstChar, newChild);
			}

			/// <summary>
			/// Attempt to remove a branch starting with character <paramref name="startChar"/>.
			/// </summary>
			/// <param name="startChar">The first character of the branch.</param>
			/// <returns>Returns true if found and removed.</returns>
			public bool RemoveChild(C startChar)
			{
				Branch child = null;

				if (this.children.TryGetValue(startChar, out child))
				{
					child.Parent = null;
					return this.children.Remove(startChar);
				}
				else
				{
					return false;
				}
			}

			/// <summary>
			/// Attempt to remove a branch branch.
			/// </summary>
			/// <param name="branch">The branch branch to remove.</param>
			/// <returns>
			/// Returns true if the given <paramref name="branch"/> was
			/// indeed found branch of this branch and removed.
			/// </returns>
			public bool RemoveChild(Branch branch)
			{
				Branch candidateChild = null;

				if (this.children.TryGetValue(branch.FirstChar, out candidateChild))
				{
					if (branch == candidateChild)
					{
						branch.Parent = null;
						return this.children.Remove(branch.FirstChar);
					}
				}

				return false;
			}

			/// <summary>
			/// Get a copy of the characters in this branch.
			/// </summary>
			public C[] GetChars()
			{
				C[] chars = new C[this.Length];

				for (int i = 0; i < this.Length; i++)
				{
					chars[i] = this.Source[this.StartIndex + i];
				}

				return chars;
			}

			/// <summary>
			/// Get a copy of the characters of the full path up to this branch.
			/// </summary>
			public C[] GetPathChars()
			{
				int length = StartIndex + Length - WordStartIndex;
				C[] word = new C[length];
				Array.Copy(Source, WordStartIndex, word, 0, length);

				return word;
			}

			#endregion
		}

		/// <summary>
		/// A NOP action during word storage.
		/// </summary>
		[Serializable]
		private class NullWordItemProcessor : WordItemProcessor<C, D, N>
		{
			public override void OnWordAdd(C[] str, D wordItem, Branch branch)
			{
			}
		}

		/// <summary>
		/// Represents a result from a search in the tree.
		/// </summary>
		public class SearchResult
		{
			#region Construction

			/// <summary>
			/// Create.
			/// </summary>
			/// <param name="branch">
			/// The branch where the match terminated.
			/// </param>
			/// <param name="matchEndOffset">
			/// The offset within the <paramref name="branch"/> where the match ended.
			/// </param>
			/// <param name="editDistance">
			/// The edit distance of the match compared to the query.
			/// </param>
			public SearchResult(Branch branch, int matchEndOffset, double editDistance = 0.0)
			{
				if (branch == null) throw new ArgumentNullException("branch");
				if (matchEndOffset > branch.Length)
					throw new ArgumentException("matchEndOffset cannot be greater that branche's length.", "matchEndOffset");
				if (matchEndOffset < 0)
					throw new ArgumentException("matchEndOffset must not be negative.", "matchEndOffset");

				this.Branch = branch;
				this.MatchEndOffset = matchEndOffset;

				int matchLength = branch.StartIndex + matchEndOffset - branch.WordStartIndex;

				this.Match = new C[matchLength];

				Array.Copy(this.Branch.Source, branch.WordStartIndex, this.Match, 0, matchLength);

				this.EditDistance = editDistance;
			}

			#endregion

			#region Public properties

			/// <summary>
			/// The match found.
			/// </summary>
			public C[] Match { get; private set; }

			/// <summary>
			/// The branch where the match terminated.
			/// </summary>
			public Branch Branch { get; private set; }

			/// <summary>
			/// The offset within the <see cref="Branch"/> where the match ended.
			/// </summary>
			public int MatchEndOffset { get; private set; }

			/// <summary>
			/// The edit distance of the match compared to the query.
			/// </summary>
			public double EditDistance { get; private set; }

			#endregion
		}

		#endregion

		#region Delegates definitions

		/// <summary>
		/// Signature of a function to be applied to a branch.
		/// </summary>
		/// <param name="branch">The branch to act upon.</param>
		/// <remarks>
		/// Used in <see cref="DfsVisit"/> method.
		/// </remarks>
		public delegate void BranchActionFunction(Branch branch);

		/// <summary>
		/// Signature of a function that returns a value extracted from a branch.
		/// </summary>
		/// <typeparam name="T">The type of value extracted.</typeparam>
		/// <param name="branch">The branch to extract the value from.</param>
		/// <returns>Returns the value extracted.</returns>
		/// <remarks>
		/// Used in <see cref="PostOrderProcess"/> method.
		/// </remarks>
		public delegate T BranchValueFunction<T>(Branch branch);

		/// <summary>
		/// Signature of a function that combines two values.
		/// </summary>
		/// <typeparam name="T">The type type of values to combine.</typeparam>
		/// <param name="v1">The first value.</param>
		/// <param name="v2">The second value.</param>
		/// <returns>Returns the combined value.</returns>
		/// <remarks>
		/// Used in <see cref="PostOrderProcess"/> method.
		/// </remarks>
		public delegate T AccumulatorFunction<T>(T v1, T v2);

		/// <summary>
		/// Signature of a function that processes a branch with an argument of
		/// type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The input type.</typeparam>
		/// <param name="branch">The branch being processed.</param>
		/// <param name="value">The value corresponding to the branch.</param>
		/// <remarks>
		/// Used in <see cref="PostOrderProcess"/> method.
		/// </remarks>
		public delegate void BranchProcessFunction<T>(Branch branch, T value);

		/// <summary>
		/// Signature of a character replacement cost function.
		/// Used in edit distance calculations.
		/// </summary>
		/// <param name="char1">The original character.</param>
		/// <param name="char2">The replacement character.</param>
		/// <returns>
		/// Returns a cost typically between zero for identical characters
		/// and one for totally different characters.
		/// </returns>
		/// <remarks>
		/// The function is expected to be symmetric in respect to char1 and char2,
		/// i.e. interchanging char1 and char2 should return the same value.
		/// </remarks>
		public delegate double DistanceFunction(C char1, C char2);

		#endregion

		#region Private fields

		private static readonly NullWordItemProcessor nullWordItemProcessor = new NullWordItemProcessor();

		#endregion

		#region Protected fields

		/// <summary>
		/// The word item procesor to use during word addition.
		/// </summary>
		protected readonly WordItemProcessor<C, D, N> wordItemProcessor;

		#endregion

		#region Public properties

		/// <summary>
		/// The root of the radix tree.
		/// </summary>
		public Branch Root { get; private set; }

		/*
		/// <summary>
		/// The collection of word entries contained in the tree.
		/// </summary>
		/// <remarks>
		/// Commonly used in serialization scenarios.
		/// </remarks>
		public IEnumerable<WordEntry> WordEntries
		{
			get
			{
				return this.wordEntries;
			}
		}
		*/

		#endregion

		#region Construction

		/// <summary>
		/// Create.
		/// </summary>
		/// <param name="wordItemProcessor">The optional word item processor to use during word addition.</param>
		public RadixTree(WordItemProcessor<C, D, N> wordItemProcessor = null)
		{
			Clear();

			this.wordItemProcessor = wordItemProcessor ?? nullWordItemProcessor;
		}

		#endregion

		#region Public methods

		/// <summary>
		/// Clear the contents of the radix tree.
		/// </summary>
		/// <remarks>
		/// Only the <see cref="Root"/> remains after the call.
		/// </remarks>
		public virtual void Clear()
		{
			this.Root = new Branch(new C[0], 0, 0, 0);
			this.Root.SuffixLink = this.Root;

			/* this.wordEntries.Clear(); */
		}

		/// <summary>
		/// Add a word to the radix tree.
		/// </summary>
		/// <param name="word">The word to add.</param>
		/// <param name="wordItem">The info associated with the word.</param>
		/// <remarks>
		/// In order for the tree to be non-implicit, ie. to have a leaf for each word, the word
		/// must have as its last 'character' a character that doesn't belong to the
		/// 'character set' or 'alphabet'. This is frequently called a sentinel, or a termination
		/// special character, and is usually depicted in various papers as '$'.
		/// Desired time and space complexity for implementations:
		/// O(<paramref name="word"/>.Length).
		/// </remarks>
		public abstract void AddWord(C[] word, D wordItem);

		/*
		/// <summary>
		/// Load the radix tree.
		/// </summary>
		/// <param name="reader">The reader to use for entry loading.</param>
		/// <remarks>
		/// Appends the loaded data to the existing, if any.
		/// </remarks>
		public virtual void Load(IWordEntryReader reader)
		{
			if (reader == null) throw new ArgumentNullException("reader");

			reader.Open();

			WordEntry wordEntry;

			try
			{
				while (reader.Read(out wordEntry))
				{
					this.AddWord(wordEntry.Word, wordEntry.WordItem);
				}
			}
			finally
			{
				reader.Close();
			}
		}

		/// <summary>
		/// Save the radix tree.
		/// </summary>
		/// <param name="writer">The writer to use for entry saving.</param>
		public virtual void Save(IWordEntryWriter writer)
		{
			if (writer == null) throw new ArgumentNullException("writer");

			writer.Open();

			try
			{
				foreach (var wordEntry in this.WordEntries)
				{
					writer.Write(wordEntry);
				}
			}
			finally
			{
				writer.Close();
			}
		}
		*/

		/// <summary>
		/// Visit the given <paramref name="branch"/> and all its descendants by DFS order and
		/// apply the given <paramref name="branchAction"/> function to each visited branch.
		/// </summary>
		/// <param name="branch">The branch to start the traversal.</param>
		/// <param name="branchAction">The function to apply to each branch.</param>
		public void DfsVisit(Branch branch, BranchActionFunction branchAction)
		{
			if (branch == null) throw new ArgumentNullException("branch");
			if (branchAction == null) throw new ArgumentNullException("branchAction");

			this.DfsVisitImpl(branch, branchAction);
		}

		/// <summary>
		/// Visit recursively the given branch and all its descendants by post-order,
		/// get a value from each branch, accumulate all descendant values of each
		/// branch and process each branch according to the accumulated value of
		/// itself and its descendants.
		/// </summary>
		/// <typeparam name="T">The type of value that is accumulated and processed.</typeparam>
		/// <param name="branch">
		/// The branch to start the traversal.
		/// </param>
		/// <param name="branchValueFunction">
		/// The function that returns the value of type <typeparamref name="T"/>
		/// associated with a branch. 
		/// Children must not be taken into account into the function, because
		/// they are taken care of automatically by the PostOrderProcess method.
		/// </param>
		/// <param name="accumulatorFunction">
		/// The function that accumulates values 
		/// of type <typeparamref name="T"/>.
		/// </param>
		/// <param name="branchProcessFunction">
		/// The function that manipulates the 
		/// branch based on the accumulated value of type <typeparamref name="T"/>.
		/// </param>
		/// <returns>
		/// Returns the total accumulated value of type <typeparamref name="T"/>
		/// of the given <paramref name="branch"/>.
		/// </returns>
		public T PostOrderProcess<T>(
			Branch branch,
			BranchValueFunction<T> branchValueFunction,
			AccumulatorFunction<T> accumulatorFunction,
			BranchProcessFunction<T> branchProcessFunction)
		{
			if (branch == null) throw new ArgumentNullException("branch");
			if (branchValueFunction == null) throw new ArgumentNullException("branchValueFunction");
			if (accumulatorFunction == null) throw new ArgumentNullException("accumulatorFunction");
			if (branchProcessFunction == null) throw new ArgumentNullException("branchProcessFunction");

			return this.PostOrderProcessImpl<T>(
				branch,
				branchValueFunction,
				accumulatorFunction,
				branchProcessFunction);
		}

		/// <summary>
		/// Visit recursively the given branch and all its descendants by pre-order,
		/// get a value from each branch, push the value
		/// to the children in order for them to accumulate it with their own,
		/// then process the branch with the accumulated value.
		/// </summary>
		/// <typeparam name="T">The type of value that is accumulated and processed.</typeparam>
		/// <param name="branch">
		/// The branch to start the traversal.
		/// </param>
		/// <param name="branchValueFunction">
		/// The function that returns the value of type <typeparamref name="T"/>
		/// associated with a branch. 
		/// Children must not be taken into account into the function, because
		/// they are taken care of automatically by the PreOrderProcess method.
		/// </param>
		/// <param name="accumulatorFunction">
		/// The function that accumulates values 
		/// of type <typeparamref name="T"/>.
		/// </param>
		/// <param name="branchProcessFunction">
		/// The function that manipulates the 
		/// branch based on the accumulated value of type <typeparamref name="T"/>.
		/// </param>
		/// <param name="startBranch">
		/// If specified, this is the value that the start branch inherits.
		/// </param>
		/// <returns>
		/// Returns the total accumulated value of type <typeparamref name="T"/>
		/// of the given <paramref name="branch"/>.
		/// </returns>
		public T PreOrderProcess<T>(
			Branch branch,
			BranchValueFunction<T> branchValueFunction,
			AccumulatorFunction<T> accumulatorFunction,
			BranchProcessFunction<T> branchProcessFunction,
			T startBranch = default(T))
		{
			if (branch == null) throw new ArgumentNullException("branch");
			if (branchValueFunction == null) throw new ArgumentNullException("branchValueFunction");
			if (accumulatorFunction == null) throw new ArgumentNullException("accumulatorFunction");
			if (branchProcessFunction == null) throw new ArgumentNullException("branchProcessFunction");

			return PreOrderProcessImpl(
				branch,
				branchValueFunction,
				accumulatorFunction,
				branchProcessFunction,
				startBranch);
		}

		/// <summary>
		/// Search for the longest common prefix between a given word and
		/// the tree contents.
		/// </summary>
		/// <param name="word">The word to search for.</param>
		/// <returns>
		/// Returns the match position corresponding to the 
		/// longest common prefix.
		/// </returns>
		public SearchResult LongestCommonPrefixSearch(C[] word)
		{
			return this.LongestCommonPrefixSearch(word, 0, this.Root);
		}

		/// <summary>
		/// Search for the longest common prefix between a given word and
		/// the tree contents below a given branch.
		/// </summary>
		/// <param name="word">The word containing the suffix.</param>
		/// <param name="wordSuffixIndex">The index of the word suffix.</param>
		/// <param name="branch">The branch to start the search from.</param>
		/// <returns>
		/// Returns the match position corresponding to the 
		/// longest common prefix.
		/// </returns>
		public SearchResult LongestCommonPrefixSearch(C[] word, int wordSuffixIndex, Branch branch)
		{
			if (word == null) throw new ArgumentNullException("word");
			if (branch == null) throw new ArgumentNullException("branch");
			if (wordSuffixIndex > word.Length)
				throw new ArgumentException("wordSuffixIndex must be less than word's length.", "wordSuffixIndex");

			// The offset in the current branch where we 
			// compare our current character.
			int branchOffset = 0;

			for (int i = wordSuffixIndex; i < word.Length; i++)
			{
				// Our current character.
				C chr = word[i];

				// Did we reach the end of the branch?
				if (branchOffset >= branch.Length)
				{
					// Yes, so try to find whether there is an outgoing branch
					// starting with the current character.
					var nextBranch = branch.GetChildStartingWith(chr);

					// Is there such a branch?
					if (nextBranch == null)
					{
						// No, so the current character is not found.
						return new SearchResult(branch, branch.Length);
					}

					// Yes, a branch starting with the current character is found,
					// so, continue from there.
					// Take account of the found character by setting branchOffset
					// equal to one.
					branch = nextBranch;
					branchOffset = 1;
				}
				else
				{
					// No, our position is somewhere in the middle of the current branch.
					// Does our character match with the one at the position of the branch?
					if (!branch.Source[branch.StartIndex + branchOffset].Equals(chr))
					{
						return new SearchResult(branch, branchOffset);
					}
					else
					{
						// If yes, advance our search position
						// in the current branch for the next character test.
						branchOffset++;
					}
				}
			}

			// If we reached this point, there was a total path overlap 
			// of the word against the preexisting tree.

			return new SearchResult(branch, branchOffset);
		}

		/// <summary>
		/// Search in the tree for an exact prefix match of a whole given word.
		/// </summary>
		/// <param name="word">The word to search for.</param>
		/// <returns>Returns the match position, if found, else null.</returns>
		public SearchResult ExactSearch(C[] word)
		{
			SearchResult lcpResult = this.LongestCommonPrefixSearch(word);

			// If only a prefix was matched, it was not a complete match, so, return null.
			if (lcpResult.Match.Length < word.Length) return null;

			return lcpResult;
		}

		/// <summary>
		/// Search in the tree for all prefixes matching the whole of a given word.
		/// </summary>
		/// <param name="word">The word to search for.</param>
		/// <returns>
		/// Returns all the matches found.
		/// </returns>
		public IList<SearchResult> ExactPrefixSearch(C[] word)
		{
			if (word == null) throw new ArgumentNullException("word");

			var lcpSearchResult = this.LongestCommonPrefixSearch(word);

			if (lcpSearchResult.Branch == this.Root) return new List<SearchResult>(0);

			var searchResults = new List<SearchResult>();

			this.DfsVisit(
				lcpSearchResult.Branch, 
				delegate(Branch visitedBranch)
				{
					searchResults.Add(new SearchResult(visitedBranch, visitedBranch.Length));
				}
			);

			return searchResults;
		}

		/// <summary>
		/// Search in the tree for full words having at most
		/// a given maximum edit distance from a given word.
		/// </summary>
		/// <param name="word">The word to search for in the tree.</param>
		/// <param name="maxDistance">The maximum allowed edit distance of matches in the tree.</param>
		/// <param name="distanceFunction">
		/// The function computing the replacement cost between two characters.
		/// </param>
		/// <returns>
		/// Returns the collection of search results.
		/// </returns>
		public IList<SearchResult> ApproximateSearch(
			C[] word, 
			double maxDistance, 
			DynamicMatrix.DistanceFunction<C> distanceFunction)
		{
			if (word == null) throw new ArgumentNullException("word");
			if (distanceFunction == null) throw new ArgumentNullException("distanceFunction");

			var results = new List<SearchResult>();

			this.ApproximateSearch(
				word, 
				maxDistance, 
				distanceFunction, 
				this.Root, 
				0, 
				results,
				DynamicMatrix.Column.CreateInitial(word.Length, maxDistance)
			);

			return results;
		}

		#endregion

		#region Private methods

		/// <summary>
		/// Search recursively under a given branch offset in the tree for words having at most
		/// a given maximum edit distance from a given word.
		/// </summary>
		/// <param name="word">The word to search for.</param>
		/// <param name="maxDistance">The maximum allowed edit distance.</param>
		/// <param name="distanceFunction">The character replacement cost function.</param>
		/// <param name="branch">The branch to continue the search from.</param>
		/// <param name="branchOffset">The offset of the current search position within the branch.</param>
		/// <param name="results">The collection of results being built.</param>
		/// <param name="column">
		/// The current column of the dynamic programming matrix used for
		/// edit distance calculation.
		/// </param>
		private void ApproximateSearch(
			C[] word,
			double maxDistance,
			DynamicMatrix.DistanceFunction<C> distanceFunction,
			Branch branch,
			int branchOffset,
			List<SearchResult> results,
			DynamicMatrix.Column column)
		{
			// Did we reach the end of the branch?
			if (branch.Length == branchOffset)
			{
				// Yes, so recurse to children.
				foreach (var childBranch in branch.Children)
				{
					ApproximateSearch(
						word,
						maxDistance,
						distanceFunction,
						childBranch,
						0,
						results,
						column);
				}

				return;
			}

			// This is not the end of the branch.

			// Compute the next column of the edit distance matrix via dynamic programming.
			// Start at the first significant row.

			// This is the next column of the dynamic programming for edit distance.
			// It will be created just-in-time as soon as an edit distance
			// less than maxDistance is computed.
			DynamicMatrix.Column nextColumn = null;

			C branchCharacter = branch.Source[branch.StartIndex + branchOffset];

			nextColumn = DynamicMatrix.Column.CreateNext(
				word,
				maxDistance,
				distanceFunction,
				column,
				branchCharacter
			);

			// If any cell was found with edit distance under maxDistance, in other words,
			// if the nextColumn was initialized to accomodate these values, then 
			// continue searching for approximate matches, otherwise the search stops here because
			// it is guaranteed that no matches will exist. A null nextColumn signifies a theoretical
			// nextColumn with all cells set to infinity. Remember that a nextColumn is initialized
			// only when a non-infinity value is computed, i.e. an edit distance lower than maxDistance.
			if (nextColumn == null) return;

			// Do we have a match?
			// In other words, have we consumed all of the pattern,
			// all of the branch up to a terminal point and still be
			// under the maximum allowed edit distance (maxDistance)?
			// Only add matches that correspond to full words stored in the tree
			// (terminal branches).
			if (branchOffset == branch.Length - 1 && !branch.Children.Any())
			{
				double editDistance = nextColumn[word.Length - 1];

				if (editDistance <= maxDistance)
				{
					var result = new SearchResult(branch, branchOffset + 1, editDistance);

					results.Add(result);
				}
			}

			// So, we had at least one non-infinity cell, that is,
			// an edit distance under maxDistance, because a non-null nextColumn was returned.
			// Thus continue the dynamic programming.
			// I hope that the compiler's optimizer will recognize an easy tail-recursion here.
			this.ApproximateSearch(
				word,
				maxDistance,
				distanceFunction,
				branch,
				branchOffset + 1,
				results,
				nextColumn);

		}

		/// <summary>
		/// Implmentation of <see cref="DfsVisit"/> that is the real
		/// recursive function, omitting any parameter check.
		/// </summary>
		/// <remarks>
		/// Checks are performed only once in <see cref="DfsVisit"/> method.
		/// </remarks>
		private void DfsVisitImpl(Branch branch, BranchActionFunction branchAction)
		{
			foreach (var childBranch in branch.Children)
			{
				this.DfsVisitImpl(childBranch, branchAction);
			}

			branchAction(branch);
		}

		/// <summary>
		/// Implmentation of <see cref="PostOrderProcess"/> that is the real
		/// recursive function, omitting any parameter check.
		/// </summary>
		private T PostOrderProcessImpl<T>(
			Branch branch,
			BranchValueFunction<T> branchValueFunction,
			AccumulatorFunction<T> accumulatorFunction,
			BranchProcessFunction<T> branchProcessFunction)
		{
			T branchValue = branchValueFunction(branch);

			foreach (var childBranch in branch.Children)
			{
				T childBranchValue = PostOrderProcessImpl<T>(
					childBranch, 
					branchValueFunction,
					accumulatorFunction,
					branchProcessFunction);

				branchValue = accumulatorFunction(branchValue, childBranchValue);
			}

			branchProcessFunction(branch, branchValue);

			return branchValue;
		}

		/// <summary>
		/// Implmentation of <see cref="PreOrderProcess"/> that is the real
		/// recursive function, omitting any parameter check.
		/// </summary>
		private T PreOrderProcessImpl<T>(
			Branch branch,
			BranchValueFunction<T> branchValueFunction,
			AccumulatorFunction<T> accumulatorFunction,
			BranchProcessFunction<T> branchProcessFunction,
			T parentValue)
		{
			T accumulatedValue = accumulatorFunction(parentValue, branchValueFunction(branch));

			branchProcessFunction(branch, accumulatedValue);

			foreach (var childBranch in branch.Children)
			{
				this.PreOrderProcessImpl(
					childBranch,
					branchValueFunction,
					accumulatorFunction,
					branchProcessFunction,
					accumulatedValue);
			}

			return accumulatedValue;
		}

		#endregion

	}
}
