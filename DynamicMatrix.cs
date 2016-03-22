using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gramma.Indexing
{
	/// <summary>
	/// Dynamic programming matrix used for edit distance computation.
	/// </summary>
	public class DynamicMatrix
	{
		#region Delegates definition

		/// <summary>
		/// Signature of a character replacement cost function.
		/// Used in edit distance calculations.
		/// </summary>
		/// <typeparam name="C">The type of characters in the string.</typeparam>
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
		public delegate double DistanceFunction<C>(C char1, C char2);

		#endregion

		#region Auxillary classes

		/// <summary>
		/// A column of values used in edit distance calculations. Despite that it behaves like an
		/// infinite dimension vector, which can be indexed from minus infinity to plus 
		/// infinity, it only holds the significant values added to it. These start
		/// at a given <see cref="Column.StartIndex"/> and extend for <see cref="Column.Length"/>
		/// positions, according to the explicitly added values. 
		/// The rest of the values are implied to have value of positive infinity.
		/// </summary>
		public class Column
		{
			#region Delegates definition

			/// <summary>
			/// Called during execution of CreateNext method
			/// when a cell having value less than maxDistance is computed.
			/// </summary>
			/// <param name="rowIndex">The row where the cell under maxDistance was found.</param>
			/// <param name="editDistance">The computed edit distance of the cell.</param>
			public delegate void MatchCallback(int rowIndex, double editDistance);

			#endregion

			#region Private fields

			/// <summary>
			/// Holds the added items.
			/// </summary>
			private List<double> items;

			#endregion

			#region Public properties

			/// <summary>
			/// This is the index where the added items start.
			/// </summary>
			/// <remarks>
			/// The added items represent the significant items which were explicitly added
			/// in the column.
			/// </remarks>
			public int StartIndex { get; private set; }

			/// <summary>
			/// The count of the items added in the column.
			/// </summary>
			/// <remarks>
			/// This is the count of the significant items which were explicitly added
			/// in the column.
			/// </remarks>
			public int Length
			{
				get
				{
					return items.Count;
				}
			}

			/// <summary>
			/// Get a value of the column.
			/// </summary>
			/// <param name="index">The idex of the value.</param>
			/// <returns>
			/// If index is between <see cref="StartIndex"/> and 
			/// <see cref="StartIndex"/> + <see cref="Length"/> - 1, it returns the 
			/// corresponding value, else it returns positive infinity.
			/// </returns>
			public double this[int index]
			{
				get
				{
					if (index < this.StartIndex || index >= this.StartIndex + this.Length) 
						return Double.PositiveInfinity;

					return this.items[index - this.StartIndex];
				}
			}

			#endregion

			#region Construction

			/// <summary>
			/// Create an empty column.
			/// </summary>
			/// <param name="startIndex">The starting index of the column.</param>
			/// <param name="capacity">The initial capacity of the column.</param>
			/// <remarks>
			/// The capacity is a guess for the expected length, but the column is empty.
			/// </remarks>
			public Column(int startIndex, int capacity = 5)
			{
				if (capacity < 0) 
					throw new ArgumentException("capacity must not be negative.", "capacity");

				this.StartIndex = startIndex;
				this.items = new List<double>(capacity);
			}

			#endregion

			#region Public methods

			/// <summary>
			/// Add a value to the column.
			/// </summary>
			/// <param name="value">the value to add.</param>
			/// <remarks>
			/// This increases the column's <see cref="Length"/> by one.
			/// </remarks>
			public void Add(double value)
			{
				this.items.Add(value);
			}

			/// <summary>
			/// Create an initial left column starting at index -1
			/// and having values 
			/// 0, 1, ..., min(<paramref name="maxDistance"/>, <paramref name="patternLength"/>).
			/// </summary>
			/// <param name="patternLength">The length of the pattern.</param>
			/// <param name="maxDistance">The maximum generalized edit distance. Can be infinity.</param>
			/// <returns>Returns the column.</returns>
			public static Column CreateInitial(int patternLength, double maxDistance)
			{
				if (patternLength < 0) throw new ArgumentException("patternLength must be non-negative.", "patternLength");
				if (maxDistance < 0.0) throw new ArgumentException("maxDistance must be non-negative.", "maxDistance");

				int bound;

				if (maxDistance < Double.PositiveInfinity)
					bound = Math.Min((int)maxDistance, patternLength);
				else
					bound = patternLength;

				var column = new Column(-1, bound + 1);

				for (int i = 0; i <= bound; i++)
				{
					column.Add(i);
				}

				return column;
			}

			/// <summary>
			/// Create an initial left column starting at index -1
			/// and having values 
			/// 0, 1, ..., min(<paramref name="maxDistance"/>, <paramref name="patternLength"/>, <paramref name="diagonalMargin"/>).
			/// </summary>
			/// <param name="patternLength">The length of the pattern.</param>
			/// <param name="maxDistance">The maximum generalized edit distance.</param>
			/// <param name="diagonalMargin">The margin of the diagonal of the computed elements of the matrix.</param>
			/// <returns>Returns the column.</returns>
			public static Column CreateInitial(int patternLength, double maxDistance, int diagonalMargin)
			{
				if (patternLength < 0) throw new ArgumentException("patternLength must be non-negative.", "patternLength");
				if (maxDistance < 0.0) throw new ArgumentException("maxDistance must be non-negative.", "maxDistance");
				if (diagonalMargin < 0) throw new ArgumentException("diagonalMargin must be non-negative.", "diagonalMargin");

				int bound;

				if (maxDistance < Double.PositiveInfinity)
					bound = Math.Min((int)maxDistance, patternLength);
				else
					bound = patternLength;

				bound = Math.Min(bound, diagonalMargin);

				return CreateInitial(patternLength, bound);
			}

			/// <summary>
			/// Create an initial left column starting at index -1
			/// and having values 
			/// 0, 1, ..., min(<paramref name="patternLength"/>, <paramref name="diagonalMargin"/>).
			/// </summary>
			/// <param name="patternLength">The length of the pattern.</param>
			/// <param name="diagonalMargin">The margin of the diagonal of the computed elements of the matrix.</param>
			/// <returns>Returns the column.</returns>
			public static Column CreateInitial(int patternLength, int diagonalMargin)
			{
				if (patternLength < 0) throw new ArgumentException("patternLength must be non-negative.", "patternLength");
				if (diagonalMargin < 0) throw new ArgumentException("diagonalMargin must be non-negative.", "diagonalMargin");

				int bound = Math.Min(patternLength, diagonalMargin);

				var column = new Column(-1, bound + 1);

				for (int i = 0; i <= bound; i++)
				{
					column.Add(i);
				}

				return column;
			}

			/// <summary>
			/// Create the next column right after a given existing column
			/// of an edit distance dynamic programming computation.
			/// </summary>
			/// <typeparam name="C">The type of character.</typeparam>
			/// <param name="rowWord">The row-wise word (pattern) in the edit distance matrix.</param>
			/// <param name="maxDistance">The maximum allowed edit distance. Can be infinity.</param>
			/// <param name="distanceFunction">The distance function between two characters.</param>
			/// <param name="currentColumn">The current column.</param>
			/// <param name="nextColumnCharacter">
			/// The character of the column-wise word in the edit distance matrix
			/// corresponding to the requested next column.
			/// </param>
			/// <param name="matchCallback">
			/// An optional callback to be called
			/// when a cell having value less than maxDistance is computed.
			/// </param>
			/// <returns>
			/// Returns the next column if at least one cell is under <paramref name="maxDistance"/>,
			/// else returns null, which means that the edit distance will never be under 
			/// <paramref name="maxDistance"/> for any cell of any subsequent columns.
			/// </returns>
			public static Column CreateNext<C>(
				C[] rowWord, 
				double maxDistance, 
				DistanceFunction<C> distanceFunction, 
				Column currentColumn, 
				C nextColumnCharacter,
				MatchCallback matchCallback = null)
			{
				// Compute the next column of the edit distance matrix via dynamic programming.
				// Start at the first significant row.

				// This is the next column of the dynamic programming for edit distance.
				// It will be created just-in-time as soon as an edit distance
				// less than maxDistance is computed.
				Column nextColumn = null;

				int rowIndexBound =
					Math.Min(currentColumn.StartIndex + currentColumn.Length + 1, rowWord.Length);

				for (int rowIndex = currentColumn.StartIndex; rowIndex < rowIndexBound; rowIndex++)
				{
					// Insertion distance.
					double editDistance = currentColumn[rowIndex] + 1.0;

					if (rowIndex >= 0)
					{
						C wordCharacter = rowWord[rowIndex];

						double replaceCost =
							distanceFunction(wordCharacter, nextColumnCharacter);

						// Merge replacement distance.
						editDistance = Math.Min(editDistance, currentColumn[rowIndex - 1] + replaceCost);
					}

					if (nextColumn != null)
					{
						// Merge deletion distance.
						editDistance = Math.Min(editDistance, nextColumn[rowIndex - 1] + 1.0);
					}

					// Do we have an edit distance less than the maximum?
					if (editDistance <= maxDistance)
					{
						// If yes, then we have a significant value.

						// Have we initialized the next column?
						// Intialize one if not so.
						if (nextColumn == null) nextColumn = new DynamicMatrix.Column(rowIndex);

						// Add the edit distance value calculated.
						// All the other values at indices out of bounds 
						// of explicitly added values will return infinity.
						nextColumn.Add(editDistance);

						if (matchCallback != null)
						{
							matchCallback(rowIndex, editDistance);
						}
					}
					else
					{
						// No, we have an edit distance larger that maxDistance.

						// But, if we have encountered a series of edit distance values 
						// less than maxDistance previously, or equivalently, 
						// if a non-null nextColumn computation 
						// has taken place, this means that this is the end of
						// the significant values band. So, the rest of the nextColumn values
						// from here and on must be left implicitly to be at infinity.
						// So, exit loop early, right now, our dynamic programming of the 
						// nextColumn has completed.
						if (nextColumn != null) break;
					}

				}

				return nextColumn;
			}

			/// <summary>
			/// Create the next column right after a given existing column
			/// of an edit distance dynamic programming computation.
			/// </summary>
			/// <typeparam name="C">The type of character.</typeparam>
			/// <param name="rowWord">The row-wise word (pattern) in the edit distance matrix.</param>
			/// <param name="maxDistance">The maximum allowed edit distance. Can be infinity.</param>
			/// <param name="columnIndex">The index of the column to be created.</param>
			/// <param name="diagonalMargin">The margin of the diagonal of the computed elements of the matrix.</param>
			/// <param name="distanceFunction">The distance function between two characters.</param>
			/// <param name="currentColumn">The current column.</param>
			/// <param name="columnCharacter">
			/// The character of the column-wise word in the edit distance matrix
			/// corresponding to the requested next column.
			/// </param>
			/// <param name="matchCallback">
			/// An optional callback to be called
			/// when a cell having value less than maxDistance is computed.
			/// </param>
			/// <returns>
			/// Returns the next column if at least one cell is under <paramref name="maxDistance"/>,
			/// else returns null, which means that the edit distance will never be under 
			/// <paramref name="maxDistance"/> for any cell of any subsequent columns.
			/// </returns>
			public static Column CreateNext<C>(
				C[] rowWord, 
				double maxDistance, 
				int columnIndex,
				int diagonalMargin,
				DistanceFunction<C> distanceFunction, 
				Column currentColumn, 
				C columnCharacter,
				MatchCallback matchCallback = null)
			{
				// Compute the next column of the edit distance matrix via dynamic programming.
				// Start at the first significant row.

				// This is the next column of the dynamic programming for edit distance.
				// It will be created just-in-time as soon as an edit distance
				// less than maxDistance is computed.
				Column nextColumn = null;

				int rowIndexBound =
					Math.Min(currentColumn.StartIndex + currentColumn.Length + 1, rowWord.Length);

				rowIndexBound = Math.Min(rowIndexBound, columnIndex + diagonalMargin + 1);

				int rowIndexStart = Math.Max(currentColumn.StartIndex, columnIndex - diagonalMargin);

				for (int rowIndex = rowIndexStart; rowIndex < rowIndexBound; rowIndex++)
				{
					// Insertion distance.
					double editDistance = currentColumn[rowIndex] + 1.0;

					if (rowIndex >= 0)
					{
						C wordCharacter = rowWord[rowIndex];

						double replaceCost =
							distanceFunction(wordCharacter, columnCharacter);

						// Merge replacement distance.
						editDistance = Math.Min(editDistance, currentColumn[rowIndex - 1] + replaceCost);
					}

					if (nextColumn != null)
					{
						// Merge deletion distance.
						editDistance = Math.Min(editDistance, nextColumn[rowIndex - 1] + 1.0);
					}

					// Do we have an edit distance less than the maximum?
					if (editDistance <= maxDistance)
					{
						// If yes, then we have a significant value.

						// Have we initialized the next column?
						// Intialize one if not so.
						if (nextColumn == null) nextColumn = new DynamicMatrix.Column(rowIndex);

						// Add the edit distance value calculated.
						// All the other values at indices out of bounds 
						// of explicitly added values will return infinity.
						nextColumn.Add(editDistance);

						if (matchCallback != null)
						{
							matchCallback(rowIndex, editDistance);
						}
					}
					else
					{
						// No, we have an edit distance larger that maxDistance.

						// But, if we have encountered a series of edit distance values 
						// less than maxDistance previously, or equivalently, 
						// if a non-null nextColumn computation 
						// has taken place, this means that this is the end of
						// the significant values band. So, the rest of the nextColumn values
						// from here and on must be left implicitly to be at infinity.
						// So, exit loop early, right now, our dynamic programming of the 
						// nextColumn has completed.
						if (nextColumn != null) break;
					}

				}

				return nextColumn;
			}

			#endregion
		}

		/// <summary>
		/// An edit action.
		/// </summary>
		public abstract class EditCommand : IComparable<EditCommand>
		{
			#region Public properties

			/// <summary>
			/// The index on the source string where the action takes place.
			/// </summary>
			public int SourceIndex { get; private set; }

			/// <summary>
			/// The cost of the action.
			/// </summary>
			public double Cost { get; private set; }

			#endregion

			#region Construction

			/// <summary>
			/// Create.
			/// </summary>
			/// <param name="sourceIndex">The index on the source string where the action takes place.</param>
			/// <param name="cost">The cost of the action.</param>
			public EditCommand(int sourceIndex, double cost)
			{
				if (sourceIndex < -1) throw new ArgumentException("sourceIndex must not be negative.", "sourceIndex");
				if (cost < 0.0) throw new ArgumentException("cost must be non-negative.", "csot");

				this.SourceIndex = sourceIndex;
				this.Cost = cost;
			}

			#endregion

			#region IComparable<EditCommand> Members

			/// <summary>
			/// Compares this action to an other action by their <see cref="SourceIndex"/>.
			/// </summary>
			/// <param name="other">The other action to compare.</param>
			/// <returns>Returns 0 if equal, 1 if this action is greater, else -1.</returns>
			public int CompareTo(EditCommand other)
			{
				if (other == null) throw new ArgumentNullException("other");

				return this.SourceIndex.CompareTo(other.SourceIndex);
			}

			#endregion
		}

		/// <summary>
		/// Delete action.
		/// </summary>
		public class DeleteCommand : EditCommand
		{
			#region Construction

			/// <summary>
			/// Create.
			/// </summary>
			/// <param name="sourceIndex">The index on the source string where the action takes place.</param>
			public DeleteCommand(int sourceIndex)
				: base(sourceIndex, 1.0)
			{

			}

			#endregion
		}

		/// <summary>
		/// Add action.
		/// </summary>
		/// <typeparam name="C">The type of character added.</typeparam>
		public class AddCommand<C> : EditCommand
		{
			#region Construction

			/// <summary>
			/// Create.
			/// </summary>
			/// <param name="sourceIndex">The index on the source string where the action takes place.</param>
			/// <param name="addedCharacter">The added character.</param>
			public AddCommand(int sourceIndex, C addedCharacter)
				: base(sourceIndex, 1.0)
			{
				this.AddedCharacter = addedCharacter;
			}

			#endregion

			#region Public properties

			/// <summary>
			/// The added character.
			/// </summary>
			public C AddedCharacter { get; private set; }

			#endregion
		}

		/// <summary>
		/// Replace action.
		/// </summary>
		/// <typeparam name="C">The type of the replacing character.</typeparam>
		public class ReplaceCommand<C> : EditCommand
		{
			#region Construction

			/// <summary>
			/// Create.
			/// </summary>
			/// <param name="sourceIndex">The index on the source string where the action takes place.</param>
			/// <param name="existingCharacter">The existing character before replacement.</param>
			/// <param name="replacingCharacter">The replacing character.</param>
			/// <param name="cost">The cost of the action.</param>
			public ReplaceCommand(int sourceIndex, C existingCharacter, C replacingCharacter, double cost = 1.0)
				: base(sourceIndex, cost)
			{
				this.ExistingCharacter = existingCharacter;
				this.ReplacingCharacter = replacingCharacter;
			}

			#endregion

			#region Public properties

			/// <summary>
			/// The existing character before replacement.
			/// </summary>
			public C ExistingCharacter { get; private set; }

			/// <summary>
			/// The replacing character.
			/// </summary>
			public C ReplacingCharacter { get; private set; }

			#endregion
		}

		#endregion

		#region Private fields

		private List<Column> columns;

		#endregion

		#region Construction

		/// <summary>
		/// Create.
		/// </summary>
		public DynamicMatrix()
		{
			this.columns = new List<Column>();
		}

		/// <summary>
		/// Create an edit distance matrix between two words.
		/// </summary>
		/// <typeparam name="C">The type of the character of the words.</typeparam>
		/// <param name="rowWord">The word that forms the first row of the matrix.</param>
		/// <param name="columnWord">The word that forms the first column of the matrix.</param>
		/// <param name="maxDistance">
		/// The maximum edit distance computed, infinity allowed. Values beyond this will be marked in the
		/// matrix as infinity.
		/// </param>
		/// <param name="distanceFunction">
		/// The function that returns the edit distance between two characters of
		/// type <typeparamref name="C"/>.
		/// </param>
		/// <returns>
		/// Returns the edit distance matrix for the given two words.
		/// </returns>
		public static DynamicMatrix FromEditDistance<C>(
			C[] rowWord,
			C[] columnWord,
			double maxDistance,
			DistanceFunction<C> distanceFunction)
		{
			if (rowWord == null) throw new ArgumentNullException("rowWord");
			if (columnWord == null) throw new ArgumentNullException("columnWord");
			if (distanceFunction == null) throw new ArgumentNullException("distanceFunction");

			var matrix = new DynamicMatrix();

			var column = Column.CreateInitial(rowWord.Length, maxDistance);

			matrix.AddColumn(column);

			for (int i = 0; i < columnWord.Length; i++)
			{
				C nextColumnCharacter = columnWord[i];

				Column nextColumn = 
					Column.CreateNext(rowWord, maxDistance, distanceFunction, column, nextColumnCharacter);

				// If any cell was found with edit distance under maxDistance, in other words,
				// if the nextColumn was initialized to accomodate these values, then 
				// continue searching for approximate matches, otherwise the search stops here because
				// it is guaranteed that no matches will exist. A null nextColumn signifies a theoretical
				// nextColumn with all cells set to infinity. Remember that a nextColumn is initialized
				// only when a non-infinity value is computed, i.e. an edit distance lower than maxDistance.
				if (nextColumn != null)
				{
					matrix.AddColumn(nextColumn);
					
					column = nextColumn;
				}
				else
				{
					// Fill the rest of the matrix with empty columns.
					for (int j = i; j < columnWord.Length; j++)
					{
						var emptyColumn = new Column(-1, 0);

						matrix.AddColumn(emptyColumn);
					}
					break;
				}

			}

			return matrix;
		}

		/// <summary>
		/// Create an edit distance matrix between two words.
		/// </summary>
		/// <typeparam name="C">The type of the character of the words.</typeparam>
		/// <param name="rowWord">The word that forms the first row of the matrix.</param>
		/// <param name="columnWord">The word that forms the first column of the matrix.</param>
		/// <param name="maxDistance">
		/// The maximum edit distance computed, infinity allowed. Values beyond this will be marked in the
		/// matrix as infinity.
		/// </param>
		/// <param name="distanceFunction">
		/// The function that returns the edit distance between two characters of
		/// type <typeparamref name="C"/>.
		/// </param>
		/// <param name="diagonalMargin">The margin of the diagonal of the computed elements of the matrix.</param>
		/// <returns>
		/// Returns the edit distance matrix for the given two words.
		/// </returns>
		public static DynamicMatrix FromEditDistance<C>(
			C[] rowWord,
			C[] columnWord,
			double maxDistance,
			DistanceFunction<C> distanceFunction,
			int diagonalMargin)
		{
			if (rowWord == null) throw new ArgumentNullException("rowWord");
			if (columnWord == null) throw new ArgumentNullException("columnWord");
			if (distanceFunction == null) throw new ArgumentNullException("distanceFunction");

			var matrix = new DynamicMatrix();

			var column = Column.CreateInitial(rowWord.Length, maxDistance, diagonalMargin);

			matrix.AddColumn(column);

			for (int i = 0; i < columnWord.Length; i++)
			{
				C nextColumnCharacter = columnWord[i];

				Column nextColumn =
					Column.CreateNext(rowWord, maxDistance, i, diagonalMargin, distanceFunction, column, nextColumnCharacter);

				// If any cell was found with edit distance under maxDistance, in other words,
				// if the nextColumn was initialized to accomodate these values, then 
				// continue searching for approximate matches, otherwise the search stops here because
				// it is guaranteed that no matches will exist. A null nextColumn signifies a theoretical
				// nextColumn with all cells set to infinity. Remember that a nextColumn is initialized
				// only when a non-infinity value is computed, i.e. an edit distance lower than maxDistance.
				if (nextColumn != null)
				{
					matrix.AddColumn(column);
				}
				else
				{
					// Fill the rest of the matrix with empty columns.
					for (int j = i; j < columnWord.Length; j++)
					{
						var emptyColumn = new Column(-1, 0);

						matrix.AddColumn(emptyColumn);
					}
					break;
				}

			}

			return matrix;
		}

		/// <summary>
		/// Create an edit distance matrix between two words.
		/// </summary>
		/// <typeparam name="C">The type of the character of the words.</typeparam>
		/// <param name="rowWord">The word that forms the first row of the matrix.</param>
		/// <param name="columnWord">The word that forms the first column of the matrix.</param>
		/// <param name="distanceFunction">
		/// The function that returns the edit distance between two characters of
		/// type <typeparamref name="C"/>.
		/// </param>
		/// <param name="diagonalMargin">The margin of the diagonal of the computed elements of the matrix.</param>
		/// <returns>
		/// Returns the edit distance matrix for the given two words.
		/// </returns>
		public static DynamicMatrix FromEditDistance<C>(
			C[] rowWord,
			C[] columnWord,
			DistanceFunction<C> distanceFunction,
			int diagonalMargin)
		{
			if (rowWord == null) throw new ArgumentNullException("rowWord");
			if (columnWord == null) throw new ArgumentNullException("columnWord");
			if (distanceFunction == null) throw new ArgumentNullException("distanceFunction");

			var matrix = new DynamicMatrix();

			var column = Column.CreateInitial(rowWord.Length, diagonalMargin);

			matrix.AddColumn(column);

			for (int i = 0; i < columnWord.Length; i++)
			{
				C nextColumnCharacter = columnWord[i];

				Column nextColumn =
					Column.CreateNext(rowWord, Double.PositiveInfinity, i, diagonalMargin, distanceFunction, column, nextColumnCharacter);

				// If any cell was found with edit distance under maxDistance, in other words,
				// if the nextColumn was initialized to accomodate these values, then 
				// continue searching for approximate matches, otherwise the search stops here because
				// it is guaranteed that no matches will exist. A null nextColumn signifies a theoretical
				// nextColumn with all cells set to infinity. Remember that a nextColumn is initialized
				// only when a non-infinity value is computed, i.e. an edit distance lower than maxDistance.
				if (nextColumn != null)
				{
					matrix.AddColumn(column);
				}
				else
				{
					// Fill the rest of the matrix with empty columns.
					for (int j = i; j < columnWord.Length; j++)
					{
						var emptyColumn = new Column(-1, 0);

						matrix.AddColumn(emptyColumn);
					}
					break;
				}

			}

			return matrix;
		}

		#endregion

		#region Public fields

		/// <summary>
		/// Number of columns.
		/// </summary>
		public int ColumnsCount
		{
			get
			{
				return this.columns.Count;
			}
		}

		/// <summary>
		/// Get a value of a cell of the matrix.
		/// </summary>
		/// <param name="rowIndex">
		/// Row index. Can be from minus infinity to plus infinity.
		/// </param>
		/// <param name="columnIndex">
		/// Column index. Starts from -1 and ends to <see cref="ColumnsCount"/> - 2.
		/// </param>
		/// <returns>
		/// Returns the cell value.
		/// </returns>
		public double this[int rowIndex, int columnIndex]
		{
			get
			{
				var column = this.columns[columnIndex + 1];
				return column[rowIndex];
			}
		}

		/// <summary>
		/// Get a column of the matrix.
		/// </summary>
		/// <param name="columnIndex">
		/// Column index. Starts from -1 and ends to <see cref="ColumnsCount"/> - 2.
		/// </param>
		public Column this[int columnIndex]
		{
			get
			{
				return this.columns[columnIndex + 1];
			}
		}

		#endregion

		#region Public methods

		/// <summary>
		/// Add a column to the matrix.
		/// </summary>
		/// <param name="column">The column to add.</param>
		public void AddColumn(Column column)
		{
			if (column == null) throw new ArgumentNullException("column");

			this.columns.Add(column);
		}

		/// <summary>
		/// Return the sequence of edit commands required to transform
		/// <paramref name="sourceWord"/> to <paramref name="targetWord"/>.
		/// </summary>
		/// <typeparam name="C">The type of character.</typeparam>
		/// <param name="sourceWord">The source word.</param>
		/// <param name="targetWord">The target word.</param>
		/// <param name="distanceFunction">Distance function for replacement action.</param>
		/// <returns>
		/// Returns the sequence of commands, whose <see cref="EditCommand.SourceIndex"/>
		/// refers to <paramref name="sourceWord"/>.
		/// </returns>
		public static EditCommand[] GetEditCommands<C>(
			C[] sourceWord, 
			C[] targetWord, 
			DistanceFunction<C> distanceFunction)
		{
			if (sourceWord == null) throw new ArgumentNullException("sourceWord");
			if (targetWord == null) throw new ArgumentNullException("targetWord");
			if (distanceFunction == null) throw new ArgumentNullException("distanceFunction");

			var dynamicMatrix = 
				FromEditDistance<C>(
				sourceWord, 
				targetWord, 
				Double.PositiveInfinity,
				distanceFunction);

			return TracebackCommands(dynamicMatrix, sourceWord, targetWord);
		}

		/// <summary>
		/// A <see cref="DistanceFunction{C}"/> which relies on the Equals method
		/// of class <typeparamref name="C"/>, returning 0 if the method returns true, 1 if false.
		/// </summary>
		/// <typeparam name="C">The type of character.</typeparam>
		/// <param name="char1">The first character to compare.</param>
		/// <param name="char2">The second character to compare.</param>
		/// <returns>
		/// Returns 0 if the <typeparamref name="C"/>'s
		/// Object.Equals returns true, 1 if false.
		/// </returns>
		public static double StandardDistanceFunction<C>(C char1, C char2)
		{
			if (char1 == null) throw new ArgumentNullException("char1");
			if (char2 == null) throw new ArgumentNullException("char2");

			if (char1.Equals(char2))
				return 0.0;
			else
				return 1.0;
		}

		#endregion

		#region Private methods

		private static EditCommand[] TracebackCommands<C>(DynamicMatrix dynamicMatrix, C[] sourceWord, C[] targetWord)
		{
			if (dynamicMatrix == null) throw new ArgumentNullException("dynamicMatrix");
			if (targetWord == null) throw new ArgumentNullException("targetWord");

			if (targetWord.Length != dynamicMatrix.ColumnsCount - 1) 
				throw new ArgumentException("targetWord seems to be irrelevant to this dynamic matrix.", "targetWord");

			var editCommands = new List<EditCommand>(Math.Max(sourceWord.Length, targetWord.Length));

			/* The dynamic matrix has starting row and column indices of -1 */

			for (int i = sourceWord.Length - 1, j = targetWord.Length - 1; i > -1 || j > -1; )
			{
				double currentEditDistance = dynamicMatrix[i, j];

				double editDistanceBeforeReplacement = i>= 0 && j >= 0 ? dynamicMatrix[i - 1, j - 1] : double.PositiveInfinity;

				double editDistanceBeforeAddition = j >= 0 ? dynamicMatrix[i, j - 1] : Double.PositiveInfinity;

				double editDistanceBeforeDeletion = i >= 0 ? dynamicMatrix[i - 1, j] : Double.PositiveInfinity;

				// Favor replacement first.
				if (editDistanceBeforeReplacement <= currentEditDistance
					&& editDistanceBeforeReplacement <= editDistanceBeforeAddition
					&& editDistanceBeforeReplacement <= editDistanceBeforeDeletion)
				{

					// If there is an edit distance change, mark a replace command.
					if (editDistanceBeforeReplacement != currentEditDistance)
					{
						// The sourceIndex argument should point to the row position BEFORE the action.
						editCommands.Add(new ReplaceCommand<C>(i - 1, sourceWord[i], targetWord[j], currentEditDistance - editDistanceBeforeReplacement));
					}

					i--; j--;
					continue;
				}

				// Secondly, favor deletion.

				if (editDistanceBeforeDeletion <= editDistanceBeforeAddition)
				{
					// The sourceIndex argument should point to the row position BEFORE the action.
					editCommands.Add(new DeleteCommand(i - 1));

					i--;
					continue;
				}

				// Any other case is insertion.

				// The sourceIndex argument should point to the row position BEFORE the action.
				editCommands.Add(new AddCommand<C>(i, targetWord[j]));

				j--;
			}

			editCommands.Reverse();

			return editCommands.ToArray();
		}

		#endregion
	}
}
