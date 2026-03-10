namespace Spectre.Console;

internal static class TableRenderer
{
    private static readonly Style _defaultHeadingStyle = Color.Silver;
    private static readonly Style _defaultCaptionStyle = Color.Grey;

    // Cache small padding segments to avoid repeated string + Segment allocations.
    // Padding values are typically 0-4 in practice (Spectre default is 1).
    private static readonly Segment[] _paddingCache = CreatePaddingCache(16);

    private static Segment[] CreatePaddingCache(int size)
    {
        var cache = new Segment[size];
        for (var i = 0; i < size; i++)
        {
            cache[i] = new Segment(new string(' ', i));
        }

        return cache;
    }

    private static Segment GetPaddingSegment(int width)
    {
        if (width > 0 && width < _paddingCache.Length)
        {
            return _paddingCache[width];
        }

        return new Segment(new string(' ', width));
    }

    public static List<Segment> Render(TableRendererContext context, List<int> columnWidths)
    {
        // Can't render the table?
        if (context.TableWidth <= 0 || context.TableWidth > context.MaxWidth || HasNegativeWidth(columnWidths))
        {
            return
            [
                ..new[]
                {
                    new Segment("…", context.BorderStyle)
                }
            ];
        }

        var result = new List<Segment>();
        result.AddRange(RenderAnnotation(context, context.Title, _defaultHeadingStyle));

        // Pre-allocate the row result list once and reuse across all cell rows.
        // A typical row has ~(columns * 4) segments (border + left pad + content + right pad).
        var rowResult = new List<Segment>(columnWidths.Count * 4);

        // Iterate all rows
        foreach (var (index, isFirstRow, isLastRow, row) in context.Rows.Enumerate())
        {
            var cellHeight = 1;

            // Get the list of cells for the row and calculate the cell height.
            // Store rendered lines, calculated width, column index, and span for each cell.
            var cells = new List<(List<SegmentLine>? Lines, int Width, int ColumnIndex, int Span)>(columnWidths.Count);
            var columnIndex = 0;

            foreach (var item in row)
            {
                var cell = item;
                var span = 1;

                // Check if this is a spanning cell
                if (item is TableCell tableCell)
                {
                    cell = tableCell.Content;
                    span = tableCell.ColumnSpan;
                }

                // Calculate the total width for this cell (including spanned columns)
                var cellWidth = columnWidths[columnIndex];
                if (span > 1)
                {
                    // Add widths of spanned columns plus separator widths
                    for (var i = 1; i < span; i++)
                    {
                        if (columnIndex + i < columnWidths.Count)
                        {
                            // Add separator width (assuming 1 character separator)
                            if (context.ShowBorder)
                            {
                                cellWidth += 1;
                            }

                            cellWidth += columnWidths[columnIndex + i];

                            // Add padding from intermediate columns
                            if (context.ShowBorder || context.IsGrid)
                            {
                                cellWidth += context.Columns[columnIndex + i].Padding.GetLeftSafe();
                                cellWidth += context.Columns[columnIndex + i].Padding.GetRightSafe();
                            }
                        }
                    }
                }

                var justification = context.Columns[columnIndex].Alignment;
                var childContext = context.Options with
                {
                    Justification = justification
                };

                var lines = Segment.SplitLines(cell.Render(childContext, cellWidth));
                cellHeight = Math.Max(cellHeight, lines.Count);
                cells.Add((lines, cellWidth, columnIndex, span));

                // Add null placeholders for spanned columns
                for (var i = 1; i < span; i++)
                {
                    cells.Add((null, 0, columnIndex + i, 0));
                }

                columnIndex += span;
            }

            // Show top of header?
            if (isFirstRow && context.ShowBorder)
            {
                var separator = context.Border.GetColumnRow(TablePart.Top, columnWidths, context.Columns);
                result.Add(new Segment(separator, context.BorderStyle));
                result.Add(Segment.LineBreak);
            }

            // Show footer separator?
            if (context.ShowFooters && isLastRow && context.ShowBorder && context.HasFooters)
            {
                var textBorder = context.Border.GetColumnRow(TablePart.FooterSeparator, columnWidths, context.Columns);
                if (!string.IsNullOrEmpty(textBorder))
                {
                    result.Add(new Segment(textBorder, context.BorderStyle));
                    result.Add(Segment.LineBreak);
                }
            }

            // Make cells the same shape (skip null placeholders for spanning)
            for (var i = 0; i < cells.Count; i++)
            {
                if (cells[i].Lines != null && cells[i].Lines?.Count < cellHeight)
                {
                    var lines = cells[i].Lines;
                    while (lines?.Count < cellHeight)
                    {
                        lines.Add(new SegmentLine());
                    }
                }
            }

            // Determine the indices of the first and last non-null cells
            // to correctly apply border edges when spanning cells create trailing nulls.
            var firstNonNullIndex = cells.FindIndex(c => c.Lines != null);
            var lastNonNullIndex = cells.FindLastIndex(c => c.Lines != null);

            // Iterate through each cell row using a simple for loop instead of Enumerable.Range
            for (var cellRowIndex = 0; cellRowIndex < cellHeight; cellRowIndex++)
            {
                // Reuse the pre-allocated list instead of allocating a new one per cell row
                rowResult.Clear();

                foreach (var (cellIndex, _, _, cellData) in cells.Enumerate())
                {
                    // Skip cells that are part of a span from a previous cell
                    if (cellData.Lines == null)
                    {
                        continue;
                    }

                    var isFirstCell = cellIndex == firstNonNullIndex;
                    var isLastCell = cellIndex == lastNonNullIndex;
                    var actualColumnIndex = cellData.ColumnIndex;
                    var cell = cellData.Lines;
                    var cellWidth = cellData.Width;
                    var cellSpan = cellData.Span;

                    if (isFirstCell && context.ShowBorder)
                    {
                        // Show left column edge
                        var part = isFirstRow && context.ShowHeaders
                            ? TableBorderPart.HeaderLeft
                            : TableBorderPart.CellLeft;
                        rowResult.Add(new Segment(context.Border.GetPart(part), context.BorderStyle));
                    }

                    // Pad column on left side.
                    if (context.ShowBorder || context.IsGrid)
                    {
                        var leftPadding = context.Columns[actualColumnIndex].Padding.GetLeftSafe();
                        if (leftPadding > 0)
                        {
                            rowResult.Add(GetPaddingSegment(leftPadding));
                        }
                    }

                    // Add content
                    rowResult.AddRange(cell[cellRowIndex]);

                    // Pad cell content right — use Segment.CellCount() instead of LINQ Sum()
                    var length = Segment.CellCount(cell[cellRowIndex]);
                    if (length < cellWidth)
                    {
                        rowResult.Add(GetPaddingSegment(cellWidth - length));
                    }

                    // Pad column on the right side (use the LAST column in the span)
                    var rightColumnIndex = actualColumnIndex + cellSpan - 1;
                    if (context.ShowBorder || (context.HideBorder && !isLastCell) ||
                        (context.HideBorder && isLastCell && context.IsGrid && context.PadRightCell))
                    {
                        var rightPadding = context.Columns[rightColumnIndex].Padding.GetRightSafe();
                        if (rightPadding > 0)
                        {
                            rowResult.Add(GetPaddingSegment(rightPadding));
                        }
                    }

                    if (isLastCell && context.ShowBorder)
                    {
                        // Add right column edge
                        var part = isFirstRow && context.ShowHeaders
                            ? TableBorderPart.HeaderRight
                            : TableBorderPart.CellRight;
                        rowResult.Add(new Segment(context.Border.GetPart(part), context.BorderStyle));
                    }
                    else if (context.ShowBorder)
                    {
                        // Add column separator
                        // We should ALWAYS add separator after a cell, unless this is the last cell
                        var part = isFirstRow && context.ShowHeaders
                            ? TableBorderPart.HeaderSeparator
                            : TableBorderPart.CellSeparator;
                        rowResult.Add(new Segment(context.Border.GetPart(part), context.BorderStyle));
                    }
                }

                // Is the row larger than the allowed max width?
                if (Segment.CellCount(rowResult) > context.MaxWidth)
                {
                    result.AddRange(Segment.Truncate(rowResult, context.MaxWidth));
                }
                else
                {
                    result.AddRange(rowResult);
                }

                result.Add(Segment.LineBreak);
            }

            // Show header separator?
            if (isFirstRow && context.ShowBorder && context.ShowHeaders && context.HasRows)
            {
                var separator = context.Border.GetColumnRow(TablePart.HeaderSeparator, columnWidths, context.Columns);
                result.Add(new Segment(separator, context.BorderStyle));
                result.Add(Segment.LineBreak);
            }

            // Show row separator, if headers are hidden show separator after the first row
            if (context.Border.SupportsRowSeparator && context.ShowRowSeparators &&
                (!isFirstRow || (isFirstRow && !context.ShowHeaders)) &&
                !isLastRow)
            {
                var hasVisibleFootes = context is { ShowFooters: true, HasFooters: true };
                var isNextLastLine = index == context.Rows.Count - 2;

                var isRenderingFooter = hasVisibleFootes && isNextLastLine;
                if (!isRenderingFooter)
                {
                    var separator = context.Border.GetColumnRow(TablePart.RowSeparator, columnWidths, context.Columns);
                    result.Add(new Segment(separator, context.BorderStyle));
                    result.Add(Segment.LineBreak);
                }
            }

            // Show bottom of footer?
            if (isLastRow && context.ShowBorder)
            {
                var separator = context.Border.GetColumnRow(TablePart.Bottom, columnWidths, context.Columns);
                result.Add(new Segment(separator, context.BorderStyle));
                result.Add(Segment.LineBreak);
            }
        }

        result.AddRange(RenderAnnotation(context, context.Caption, _defaultCaptionStyle));
        return result;
    }

    private static bool HasNegativeWidth(List<int> columnWidths)
    {
        for (var i = 0; i < columnWidths.Count; i++)
        {
            if (columnWidths[i] < 0)
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<Segment> RenderAnnotation(TableRendererContext context, TableTitle? header,
        Style defaultStyle)
    {
        if (header == null)
        {
            return [];
        }

        var paragraph = new Markup(header.Text, header.Style ?? defaultStyle)
            .Justify(Justify.Center)
            .Overflow(Overflow.Ellipsis);

        // Render the paragraphs
        var segments = new List<Segment>();
        segments.AddRange(((IRenderable)paragraph).Render(context.Options, context.TableWidth));

        segments.Add(Segment.LineBreak);
        return segments;
    }
}