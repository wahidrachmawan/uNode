using System;

namespace MaxyGames.Runtime {
	/// <summary>
	/// A resizable 2D matrix backed by a 1D array.
	/// </summary>
	/// <typeparam name="T">The type of elements stored in the matrix.</typeparam>
	public class Matrix<T> {
		private T[] _data;

		/// <summary>
		/// Gets the number of rows in the matrix.
		/// </summary>
		public int Rows { get; private set; }

		/// <summary>
		/// Gets the number of columns in the matrix.
		/// </summary>
		public int Cols { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="Matrix{T}"/> class.
		/// </summary>
		/// <param name="rows">The number of rows.</param>
		/// <param name="cols">The number of columns.</param>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Thrown if rows or cols are less than or equal to zero.
		/// </exception>
		public Matrix(int rows, int cols) {
			if(rows <= 0) throw new ArgumentOutOfRangeException(nameof(rows));
			if(cols <= 0) throw new ArgumentOutOfRangeException(nameof(cols));

			Rows = rows;
			Cols = cols;
			_data = new T[rows * cols];
		}

		/// <summary>
		/// Gets or sets the element at the specified row and column.
		/// </summary>
		/// <param name="row">The row index.</param>
		/// <param name="col">The column index.</param>
		/// <returns>The value at the specified position.</returns>
		public T this[int row, int col] {
			get {
				ValidateIndex(row, col);
				return _data[row * Cols + col];
			}
			set {
				ValidateIndex(row, col);
				_data[row * Cols + col] = value;
			}
		}

		/// <summary>
		/// Gets the element at the specified row and column.
		/// </summary>
		/// <param name="row">The row index.</param>
		/// <param name="col">The column index.</param>
		/// <returns>The value at the specified position.</returns>
		public T Get(int row, int col) {
			ValidateIndex(row, col);
			return _data[row * Cols + col];
		}

		/// <summary>
		/// Fills the matrix with a specified value.
		/// </summary>
		public void Fill(T value) {
			for(int i = 0; i < _data.Length; i++)
				_data[i] = value;
		}

		/// <summary>
		/// Resets all elements to their default value.
		/// </summary>
		public void Clear() => Fill(default!);

		/// <summary>
		/// Resizes the matrix while preserving existing data.
		/// New cells can be filled with a given default value.
		/// </summary>
		/// <param name="newRows">The new row count.</param>
		/// <param name="newCols">The new column count.</param>
		/// <param name="defaultValue">Optional default value for new cells.</param>
		public void Resize(int newRows, int newCols, T defaultValue = default!) {
			if(newRows <= 0) throw new ArgumentOutOfRangeException(nameof(newRows));
			if(newCols <= 0) throw new ArgumentOutOfRangeException(nameof(newCols));

			var newData = new T[newRows * newCols];

			int minRows = Math.Min(Rows, newRows);
			int minCols = Math.Min(Cols, newCols);

			for(int r = 0; r < minRows; r++) {
				Array.Copy(
					_data, r * Cols,
					newData, r * newCols,
					minCols
				);
			}

			if(!Equals(defaultValue, default(T))) {
				for(int i = 0; i < newData.Length; i++)
					if(Equals(newData[i], default(T)))
						newData[i] = defaultValue;
			}

			_data = newData;
			Rows = newRows;
			Cols = newCols;
		}

		/// <summary>
		/// Sets an entire row with new values.
		/// </summary>
		public void SetRow(int row, T[] values) {
			if(row < 0 || row >= Rows)
				throw new IndexOutOfRangeException($"Row {row} is out of bounds.");
			if(values.Length != Cols)
				throw new ArgumentException("Row length must match number of columns.");

			Array.Copy(values, 0, _data, row * Cols, Cols);
		}

		/// <summary>
		/// Sets an entire column with new values.
		/// </summary>
		public void SetColumn(int col, T[] values) {
			if(col < 0 || col >= Cols)
				throw new IndexOutOfRangeException($"Column {col} is out of bounds.");
			if(values.Length != Rows)
				throw new ArgumentException("Column length must match number of rows.");

			for(int r = 0; r < Rows; r++)
				_data[r * Cols + col] = values[r];
		}

		/// <summary>
		/// Gets a copy of a row as a 1D array.
		/// </summary>
		public T[] GetRow(int row) {
			if(row < 0 || row >= Rows)
				throw new IndexOutOfRangeException($"Row {row} is out of bounds.");

			var result = new T[Cols];
			Array.Copy(_data, row * Cols, result, 0, Cols);
			return result;
		}

		/// <summary>
		/// Gets a copy of a column as a 1D array.
		/// </summary>
		public T[] GetColumn(int col) {
			if(col < 0 || col >= Cols)
				throw new IndexOutOfRangeException($"Column {col} is out of bounds.");

			var result = new T[Rows];
			for(int r = 0; r < Rows; r++)
				result[r] = _data[r * Cols + col];

			return result;
		}

		/// <summary>
		/// Iterates over each cell of the matrix and applies an action.
		/// </summary>
		public void ForEach(Action<int, int, T> action) {
			for(int r = 0; r < Rows; r++)
				for(int c = 0; c < Cols; c++)
					action(r, c, _data[r * Cols + c]);
		}

		/// <summary>
		/// Provides access to the raw 1D backing array (row-major order).
		/// </summary>
		public T[] RawArray => _data;

		/// <summary>
		/// Converts this matrix to a standard 2D array.
		/// </summary>
		public static implicit operator T[,](Matrix<T> matrix) {
			var result = new T[matrix.Rows, matrix.Cols];
			for(int r = 0; r < matrix.Rows; r++)
				Array.Copy(matrix._data, r * matrix.Cols, result, r * matrix.Cols, matrix.Cols);
			return result;
		}

		/// <summary>
		/// Creates a Matrix from a standard 2D array.
		/// </summary>
		public static Matrix<T> FromArray(T[,] array) {
			int rows = array.GetLength(0);
			int cols = array.GetLength(1);
			var fm = new Matrix<T>(rows, cols);

			for(int r = 0; r < rows; r++)
				for(int c = 0; c < cols; c++)
					fm[r, c] = array[r, c];

			return fm;
		}

		private void ValidateIndex(int row, int col) {
			if(row < 0 || row >= Rows)
				throw new IndexOutOfRangeException($"Row {row} is out of bounds.");
			if(col < 0 || col >= Cols)
				throw new IndexOutOfRangeException($"Column {col} is out of bounds.");
		}
	}
}