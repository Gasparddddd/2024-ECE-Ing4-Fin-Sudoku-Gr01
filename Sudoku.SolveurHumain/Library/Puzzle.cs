﻿using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace Kermalis.SudokuSolver;

public sealed class Puzzle
{
	public ReadOnlyCollection<Region> Rows { get; }
	public ReadOnlyCollection<Region> Columns { get; }
	public ReadOnlyCollection<Region> Blocks { get; }
	public ReadOnlyCollection<ReadOnlyCollection<Region>> Regions { get; }

	public bool IsCustom { get; }
	public readonly Cell[] _board;
	internal readonly Region[] RowsI;
	internal readonly Region[] ColumnsI;
	internal readonly Region[] BlocksI;
	internal readonly Region[][] RegionsI;

	public Cell this[int col, int row] => _board[Utils.CellIndex(col, row)];

	public Puzzle(int[][] board, bool isCustom)
	{
		IsCustom = isCustom;

		_board = new Cell[81];
		for (int col = 0; col < 9; col++)
		{
			for (int row = 0; row < 9; row++)
			{
				_board[Utils.CellIndex(col, row)] = new Cell(this, board[col][row], new SPoint(col, row));
			}
		}

		RowsI = new Region[9];
		Rows = new ReadOnlyCollection<Region>(RowsI);
		ColumnsI = new Region[9];
		Columns = new ReadOnlyCollection<Region>(ColumnsI);
		BlocksI = new Region[9];
		Blocks = new ReadOnlyCollection<Region>(BlocksI);
		RegionsI = [RowsI, ColumnsI, BlocksI];
		Regions = new ReadOnlyCollection<ReadOnlyCollection<Region>>([Rows, Columns, Blocks]);

		var cellsCache = new Cell[9];
		for (int i = 0; i < 9; i++)
		{
			int j;
			for (j = 0; j < 9; j++)
			{
				cellsCache[j] = _board[Utils.CellIndex(j, i)];
			}
			RowsI[i] = new Region(cellsCache);

			for (j = 0; j < 9; j++)
			{
				cellsCache[j] = _board[Utils.CellIndex(i, j)];
			}
			ColumnsI[i] = new Region(cellsCache);

			j = 0;
			int x = i % 3 * 3;
			int y = i / 3 * 3;
			for (int col = x; col < x + 3; col++)
			{
				for (int row = y; row < y + 3; row++)
				{
					cellsCache[j++] = _board[Utils.CellIndex(col, row)];
				}
			}
			BlocksI[i] = new Region(cellsCache);
		}

		for (int i = 0; i < 81; i++)
		{
			_board[i].InitRegions();
		}
		for (int i = 0; i < 81; i++)
		{
			_board[i].InitVisibleCells();
		}
	}

	internal void RefreshCandidates()
	{
		for (int i = 0; i < 81; i++)
		{
			Cell cell = _board[i];
			for (int digit = 1; digit <= 9; digit++)
			{
				cell.CandI.Set(digit, true);
			}
		}
		for (int i = 0; i < 81; i++)
		{
			Cell cell = _board[i];
			if (cell.Value != Cell.EMPTY_VALUE)
			{
				cell.SetValue(cell.Value);
			}
		}
	}

	public static Puzzle CreateCustom()
	{
		int[][] board = new int[9][];
		for (int col = 0; col < 9; col++)
		{
			board[col] = new int[9];
		}
		return new Puzzle(board, true);
	}
	public static Puzzle Parse(ReadOnlySpan<string> inRows)
	{
		if (inRows.Length != 9)
		{
			throw new InvalidDataException("Puzzle must have 9 rows.");
		}

		int[][] board = new int[9][];
		for (int col = 0; col < 9; col++)
		{
			board[col] = new int[9];
		}

		for (int row = 0; row < 9; row++)
		{
			string line = inRows[row];
			if (line.Length != 9)
			{
				throw new InvalidDataException($"Row {row} must have 9 values.");
			}

			for (int col = 0; col < 9; col++)
			{
				if (int.TryParse(line[col].ToString(), out int value) && value is >= 1 and <= 9)
				{
					board[col][row] = value;
				}
				else
				{
					board[col][row] = Cell.EMPTY_VALUE; // Anything else can represent Cell.EMPTY_VALUE
				}
			}
		}

		return new Puzzle(board, false);
	}

	public void Reset()
	{
		for (int i = 0; i < 81; i++)
		{
			Cell cell = _board[i];
			if (cell.Value != cell.OriginalValue)
			{
				cell.SetValue(Cell.EMPTY_VALUE);
			}
		}
	}
	/// <summary>Returns true if any digit is repeated. Can be called even if the puzzle isn't solved yet.</summary>
	public bool CheckForErrors()
	{
		for (int digit = 1; digit <= 9; digit++)
		{
			for (int i = 0; i < 9; i++)
			{
				if (BlocksI[i].CheckForDuplicateValue(digit)
					|| RowsI[i].CheckForDuplicateValue(digit)
					|| ColumnsI[i].CheckForDuplicateValue(digit))
				{
					return true;
				}
			}
		}
		return false;
	}

	public override string ToString()
	{
		var sb = new StringBuilder();
		for (int row = 0; row < 9; row++)
		{
			for (int col = 0; col < 9; col++)
			{
				Cell cell = _board[Utils.CellIndex(col, row)];
				if (cell.OriginalValue == Cell.EMPTY_VALUE)
				{
					sb.Append('-');
				}
				else
				{
					sb.Append(cell.OriginalValue);
				}
			}
			if (row != 8)
			{
				sb.AppendLine();
			}
		}
		return sb.ToString();
	}
	public string ToStringFancy()
	{
		var sb = new StringBuilder();
		for (int row = 0; row < 9; row++)
		{
			if (row % 3 == 0)
			{
				for (int col = 0; col < 13; col++)
				{
					sb.Append('—');
				}
				sb.AppendLine();
			}
			for (int col = 0; col < 9; col++)
			{
				if (col % 3 == 0)
				{
					sb.Append('┃');
				}

				Cell cell = _board[Utils.CellIndex(col, row)];
				if (cell.Value == Cell.EMPTY_VALUE)
				{
					sb.Append(' ');
				}
				else
				{
					sb.Append(cell.Value);
				}

				if (col == 8)
				{
					sb.Append('┃');
				}
			}
			sb.AppendLine();
			if (row == 8)
			{
				for (int col = 0; col < 13; col++)
				{
					sb.Append('—');
				}
			}
		}
		return sb.ToString();
	}
}
