using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Grammophone.Indexing
{
	/// <summary>
	/// Represents a generalized suffix tree of characters
	/// of generic type <typeparamref name="C"/>.
	/// </summary>
	/// <typeparam name="C">The type of the character of a word.</typeparam>
	/// <typeparam name="D">The type of information allocated per word.</typeparam>
	/// <typeparam name="N">The type of extra data stored per node.</typeparam>
	[Serializable]
	public class SuffixTree<C, D, N> : RadixTree<C, D, N>
	{
		#region Inner class definitions

		/// <summary>
		/// Represents a node, either internal or implicit.
		/// It is defined based on the branch which contains it.
		/// </summary>
		[Serializable]
		public class Node
		{
			#region Public properties

			/// <summary>
			/// The branch where the node lies.
			/// </summary>
			public Branch Branch { get; internal set; }

			/// <summary>
			/// The offset from the start of the branch where the node lies.
			/// </summary>
			public int Offset { get; internal set; }

			#endregion

			#region Construction

			/// <summary>
			/// Create.
			/// </summary>
			/// <param name="branch">
			/// The branch where the node lies.
			/// </param>
			/// <param name="offset">
			/// The offset from the start of the branch where the node lies.
			/// </param>
			public Node(Branch branch, int offset)
			{
				if (branch == null) throw new ArgumentNullException("branch");

				if (offset > branch.Length && branch.Length > 0)
					throw new ArgumentException("offset must be less or equal to length.", "offset");

				this.Branch = branch;
				this.Offset = offset;
			}

			#endregion

			#region Public methods

			/// <summary>
			/// Try to follow node downwards expecting a character <paramref name="chr"/>.
			/// </summary>
			/// <param name="chr">The character to follow downwards.</param>
			/// <returns>Returns the node found, else null.</returns>
			public Node TryAdvance(C chr)
			{
				int position = this.Offset;

				if (position >= this.Branch.Length)
				{
					Branch child = this.Branch.GetChildStartingWith(chr);
					if (child != null)
						return new Node(child, 1);
				}
				else
				{
					if (chr.Equals(this.Branch[position]))
						return new Node(this.Branch, position + 1);
				}

				return null;
			}

			/// <summary>
			/// Follow node implied by suffix link.
			/// </summary>
			/// <returns>Returns the linked node if it exists or null.</returns>
			public Node FollowLink()
			{
				if (this.Offset == 0)
				{
					// Is this the root?
					if (this.Branch.Parent != null)
					{
						// If not, then the suffix link is given by the parent branch.
						Branch linkedBranch = this.Branch.Parent.SuffixLink;
						if (linkedBranch != null)
						{
							return new Node(linkedBranch, linkedBranch.Length);
						}
					}
					return null;
				}
				else
				{
					// If node is an explicit one, go to suffix link directly,
					if (this.Offset == this.Branch.Length)
					{
						var branchSuffixLink = this.Branch.SuffixLink;

						if (branchSuffixLink != null)
							return new Node(branchSuffixLink, branchSuffixLink.Length);
						else
							return null;
					}
					// else, ascend to nearest parent explicit node,
					if (this.Branch.Parent != null)
					{
						// and, if the parent is not root,
						// go to its suffix link, then descend by equivalent characters.
						if (!this.Branch.Parent.IsRoot)
						{
							Branch linkedBranch = this.Branch.Parent.SuffixLink;

							if (linkedBranch == null) return null;

							int distanceFromBridge = this.Offset;

							int descentPosition = 0;

							Branch linkedChildBranch = null;

							while (true)
							{
								linkedChildBranch =
									linkedBranch.GetChildStartingWith(this.Branch[descentPosition]);

								if (linkedChildBranch == null) break;

								if (distanceFromBridge <= linkedChildBranch.Length)
									return new Node(linkedChildBranch, distanceFromBridge);

								descentPosition += linkedChildBranch.Length;
								distanceFromBridge -= linkedChildBranch.Length;
								linkedBranch = linkedChildBranch;
							}

							return null;
						}
						else if (this.Branch.SuffixLink != null)
						{
							// else, search from the root for the branches that 
							// form the path of characters [1..Offset] of this branch.

							Branch linkedBranch = this.Branch.Parent;

							int distanceFromBridge = this.Offset - 1;

							// No descent is needed. Link to the root.
							if (distanceFromBridge == 0)
								return new Node(linkedBranch, linkedBranch.Length);

							int descentPosition = 1;

							Branch linkedChildBranch = null;

							while (true)
							{
								linkedChildBranch =
									linkedBranch.GetChildStartingWith(this.Branch[descentPosition]);

								if (linkedChildBranch == null) break;

								if (distanceFromBridge <= linkedChildBranch.Length)
									return new Node(linkedChildBranch, distanceFromBridge);

								descentPosition += linkedChildBranch.Length;
								distanceFromBridge -= linkedChildBranch.Length;
								linkedBranch = linkedChildBranch;
							}

						}
					}
				}

				return null;
			}

			/// <summary>
			/// Get the closest explicit parent node.
			/// </summary>
			/// <remarks>
			/// Valid as long as the sufix tree is unchanged. Used in matching statistics.
			/// </remarks>
			public Node GetFloor()
			{
				if (this.Offset != 0)
				{
					if (this.Branch.Parent != null)
						return new Node(this.Branch.Parent, this.Branch.Parent.Length);
					else
						return null;
				}
				else
				{
					if (this.Branch.Parent != null)
					{
						if (this.Branch.Parent.Parent != null)
						{
							return new Node(this.Branch.Parent.Parent, this.Branch.Parent.Parent.Length);
						}
					}

					return null;
				}
			}

			/// <summary>
			/// Get the next downwards explitit node, if this is not already explicit.
			/// Else, return the same node.
			/// </summary>
			/// <remarks>
			/// Valid as long as the sufix tree is unchanged. Used in matching statistics.
			/// </remarks>
			public Node GetCeil()
			{
				if (this.Offset > 0)
				{
					if (this.Offset < this.Branch.Length)
						return new Node(this.Branch, this.Branch.Length);
					else
						return this;
				}
				else
				{
					if (this.Branch.Parent != null)
						return new Node(this.Branch.Parent, this.Branch.Parent.Length);
					else
						return this;
				}
			}

			#endregion

			#region Internal methods

			/// <summary>
			/// Add a branch to the current node.
			/// </summary>
			/// <param name="branch">The branch to add.</param>
			/// <returns>
			/// If a new explicit internal node was created, it returns true.
			/// </returns>
			internal bool AddBranch(Branch branch)
			{
				if (this.Offset == 0)
				{
					if (this.Branch.Length == 0) // Is this the root (which is empty)?
					{
						this.Branch.AddChild(branch);
					}
					else if (this.Branch.Parent != null)
					{
						this.Branch.Parent.AddChild(branch);
					}

					return false;
				}
				else
				{
					if (this.Offset < this.Branch.Length)
					{
						Branch upperBanch = this.Branch.Split(this.Offset);
						upperBanch.AddChild(branch);

						this.Branch = upperBanch;
						//this.Offset = upperBanch.Length;

						return true;
					}
					else
					{
						this.Branch.AddChild(branch);

						return true;
					}
				}
			}

			#endregion
		}

		/// <summary>
		/// Matching statistics.
		/// </summary>
		/// <remarks>
		/// Remains valid as long as the suffix tree upon which is built
		/// remains unchanged.
		/// </remarks>
		public class MatchingStatistics : IEnumerable<MatchingStatistics.StatisticsEntry>
		{
			#region Public classes

			/// <summary>
			/// A statistics entry that refers to a prefix of the given word.
			/// </summary>
			public class StatisticsEntry
			{
				#region Public properties

				/// <summary>
				/// Offset of the word's suffix.
				/// </summary>
				public int WordOffset { get; private set; }

				/// <summary>
				/// Length of the longest match of the word's suffix to the
				/// suffix tree.
				/// </summary>
				public int Length { get; private set; }

				/// <summary>
				/// The node (implicit or explicit) of the
				/// suffix tree that describes the longest match.
				/// </summary>
				public Node Node { get; private set; }

				/// <summary>
				/// Closest explicit parent node to the implicit node of the
				/// suffix tree that describes the longest match.
				/// </summary>
				public Node Floor { get; private set; }

				/// <summary>
				/// Next downwards node to the implicit node of the
				/// suffix tree that describes the longest match.
				/// </summary>
				public Node Ceil { get; private set; }

				#endregion

				#region Construction

				/// <summary>
				/// Create.
				/// </summary>
				internal StatisticsEntry(int wordOffset, int length, Node node)
				{
					if (node == null) throw new ArgumentNullException("node");

					this.WordOffset = wordOffset;
					this.Length = length;
					this.Node = node;
					this.Floor = node.GetFloor();
					this.Ceil = node.GetCeil();
				}

				#endregion
			}

			#endregion

			#region Private fields

			private IList<StatisticsEntry> entries;

			#endregion

			#region Public properties

			/// <summary>
			/// The word upon which statistics are built.
			/// </summary>
			public C[] Word { get; private set; }

			/// <summary>
			/// Get the statistics entry that corresponds to the <see cref="Word"/>'s
			/// suffix that starts at a given <paramref name="suffixIndex"/>.
			/// </summary>
			/// <param name="suffixIndex">The start of the <see cref="Word"/>'s suffix.</param>
			/// <returns>Returns the matching statistics entry.</returns>
			public StatisticsEntry this[int suffixIndex]
			{
				get
				{
					if (suffixIndex < 0)
						throw new IndexOutOfRangeException("suffixInex must be non negative.");

					if (suffixIndex >= this.Word.Length)
						throw new IndexOutOfRangeException("suffixIndex must be less than the word's length.");

					return this.entries[suffixIndex];
				}
			}

			#endregion

			#region Construction

			/// <summary>
			/// Create.
			/// </summary>
			internal MatchingStatistics(C[] word, StatisticsEntry[] entries)
			{
				if (word == null) throw new ArgumentNullException("word");
				if (entries == null) throw new ArgumentNullException("entries");

				if (word.Length != entries.Length)
					throw new ArgumentException("word length must be equal to entries count.");

				this.Word = word;
				this.entries = entries;
			}

			#endregion

			#region IEnumerable<StatisticsEntry> Members

			/// <summary>
			/// Enumerate all matching statistics entries.
			/// </summary>
			public IEnumerator<MatchingStatistics.StatisticsEntry> GetEnumerator()
			{
				return entries.GetEnumerator();
			}

			#endregion

			#region IEnumerable Members

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return this.entries.GetEnumerator();
			}

			#endregion
		}

		#endregion

		#region Internal properties

		/// <summary>
		/// Current first suffix link that points at an internal node.
		/// Used in speedup for tree build.
		/// </summary>
		internal Node ActiveNode { get; set; }

		#endregion

		#region Construction

		/// <summary>
		/// Create.
		/// </summary>
		/// <param name="wordItemProcessor">The optional word item processor to use during word addition.</param>
		public SuffixTree(WordItemProcessor<C, D, N> wordItemProcessor = null)
			: base(wordItemProcessor)
		{
		}

		#endregion

		#region Public methods

		/// <summary>
		/// Add a word to the suffix tree.
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
			InitializeWord();

			Node rootNode = this.ActiveNode;

			int height = 0;

			Branch previousAddedBranch = null;

			for (int i = 0; i < word.Length; i++)
			{
				Node previousSplitNode = null;

				Node node = this.ActiveNode;

				C chr = word[i];

				while (true)
				{
					Node nextNode = node.TryAdvance(chr);

					if (nextNode != null)
					{
						this.ActiveNode = nextNode;

						if (i == word.Length - 1)
						{
							if (previousAddedBranch != null) previousAddedBranch.SuffixLink = nextNode.Branch;

							do
							{
								wordItemProcessor.OnWordAdd(word, wordItem, nextNode.Branch);

								nextNode = nextNode.FollowLink();
							}
							while (!nextNode.Branch.IsRoot);
						}

						height++;

						break;
					}
					else
					{
						nextNode = node.FollowLink();

						bool nextNodeIsOnTheSameBranch = nextNode != null && nextNode.Branch == node.Branch;

						Branch addedBranch = new Branch(word, i, i - height);
						wordItemProcessor.OnWordAdd(word, wordItem, addedBranch);

						bool nodeWasSplit = node.AddBranch(addedBranch);

						addedBranch.SuffixLink = this.Root;

						if (previousAddedBranch != null) previousAddedBranch.SuffixLink = addedBranch;

						previousAddedBranch = addedBranch;

						if (nodeWasSplit)
						{
							if (nextNodeIsOnTheSameBranch)
							{
								// If the node was split and nextNode.Branch pointed initially to the same unsplit node.Branch,
								// we must restore the nextNode.Branch to point to the new upper branch after the split.
								nextNode.Branch = node.Branch;
							}

							node.Branch.SuffixLink = this.Root;

							if (previousSplitNode != null)
							{
								previousSplitNode.Branch.SuffixLink = node.Branch;
							}

							previousSplitNode = node;
						}

						node = nextNode;

						if (node == null)
						{
							this.ActiveNode = rootNode;
							break;
						}
						else
						{
							height--;
						}

					}
				}
			}
		}

		/// <summary>
		/// Get the matching statistics of a word against the sufix tree.
		/// </summary>
		/// <param name="word">The word.</param>
		/// <returns>Returns the matching statistics.</returns>
		/// <remarks>
		/// The returned matching statistics remain valid as long as the tree is
		/// not changed.
		/// Time and space complexity: O(<paramref name="word"/>.Length).
		/// </remarks>
		public MatchingStatistics GetMatchingStatistics(C[] word)
		{
			if (word == null) throw new ArgumentNullException("word");

			Node rootNode = new Node(this.Root, this.Root.Length);

			Node node = rootNode;

			int matchLength = 0;

			var entries = new MatchingStatistics.StatisticsEntry[word.Length];

			for (
				int suffixIndex = 0;
				suffixIndex < word.Length;
				suffixIndex++)
			{
				if (suffixIndex + matchLength < word.Length)
				{
					for (
						Node nextNode = node.TryAdvance(word[suffixIndex + matchLength]);
						nextNode != null;
						nextNode = nextNode.TryAdvance(word[suffixIndex + matchLength]))
					{
						matchLength++;
						node = nextNode;

						if (suffixIndex + matchLength == word.Length) break;
					}
				}

				entries[suffixIndex] =
					new MatchingStatistics.StatisticsEntry(suffixIndex, matchLength, node);

				node = node.FollowLink();

				if (node == null)
				{
					node = rootNode;
				}

				if (node.Branch.IsRoot) matchLength = 0;

				if (matchLength > 0) matchLength--;
			}

			return new MatchingStatistics(word, entries);
		}

		#endregion

		#region Private methods

		private void InitializeWord()
		{
			this.ActiveNode = new Node(this.Root, 0);
		}

		#endregion
	}
}
