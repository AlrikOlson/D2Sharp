namespace D2Sharp;

/// <summary>
/// Represents detailed error information from a failed D2 diagram rendering.
/// </summary>
public class D2Error
{
    /// <summary>
    /// Gets the error message.
    /// </summary>
    public string Message { get; init; } = "";

    /// <summary>
    /// Gets the line number where the error occurred, if available.
    /// </summary>
    public int? LineNumber { get; init; }

    /// <summary>
    /// Gets the column number where the error occurred, if available.
    /// </summary>
    public int? Column { get; init; }

    /// <summary>
    /// Gets the content of the line where the error occurred, if available.
    /// </summary>
    public string? LineContent { get; init; }

    /// <summary>
    /// Gets the line content split into parts before, at, and after the error position for highlighting.
    /// </summary>
    /// <returns>A tuple containing the text before the error, the error character, and the text after the error.</returns>
    public (string beforeError, string errorPart, string afterError) GetHighlightedLineParts()
    {
        if (string.IsNullOrEmpty(LineContent) || !Column.HasValue || Column.Value <= 0)
        {
            return (LineContent ?? "", "", "");
        }

        int highlightIndex = Column.Value - 1;
        if (highlightIndex >= LineContent.Length)
        {
            highlightIndex = LineContent.Length - 1;
        }

        string beforeError = LineContent[..highlightIndex];
        string errorPart = LineContent.Substring(highlightIndex, 1);
        string afterError = highlightIndex + 1 < LineContent.Length ? LineContent[(highlightIndex + 1)..] : "";

        return (beforeError, errorPart, afterError);
    }
}
