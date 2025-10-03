using System.Text.Json;
using Maynard.Json.Utilities;
using Maynard.Logging;
using Maynard.Text;

namespace Maynard.Extensions;

internal static class TableBuilderExtension
{
    #region LoggingHelpers
    internal static TableBuilder AppendIndentedLogTitle(this TableBuilder builder, string message, bool isFirstIndent = false, bool hasMoreLines = true, bool isFinal = false)
    {
        builder
            .NewLine()
            .AppendLogUntilMessage();
        if (isFirstIndent) // The first line should branch off the table.
        {
            builder.Corner(BoxDrawing.CornerType.BottomLeft, BoxDrawing.SolidStyle.Light);

            if (hasMoreLines) //  └──┬── Some Message
                builder
                    .HorizontalLine(BoxDrawing.LineStyle.Light, 2)
                    .Tee(BoxDrawing.TeeType.FacingDown, BoxDrawing.SolidStyle.Light)
                    .HorizontalLine(BoxDrawing.LineStyle.Light, 2);
            else //  └───── Some Message
                builder.HorizontalLine(BoxDrawing.LineStyle.Light, 5);

            builder
                .Space()
                .Append(message);
        }
        else
            builder
                .Space(3)
                .Tee(BoxDrawing.TeeType.FacingRight, BoxDrawing.SolidStyle.Light)
                .HorizontalLine(BoxDrawing.LineStyle.Light, 2)
                .Space()
                .Append(message);

        if (!hasMoreLines)
            builder.NewLine(2);
        return builder;
    }
    internal static TableBuilder AppendLogUntilMessage(this TableBuilder builder, string timestamp = "", string owner = "", string severity = "")
    {
        BoxDrawing.LineStyle style = string.IsNullOrWhiteSpace(timestamp) ? BoxDrawing.LineStyle.Dashes : BoxDrawing.LineStyle.Light;
        return builder
            .Cell(timestamp, Log.Configuration.Printing.LengthTimestampColumn)
            .VerticalLine(style)
            .Cell(owner, Log.Configuration.Printing.LengthOwnerColumn)
            .VerticalLine(style)
            .Cell(severity, Log.Configuration.Printing.LengthSeverityColumn);
    }
    internal static TableBuilder AppendIndentedLogMessageLine(this TableBuilder builder, string line, bool returnIndent)
    {
        builder
            .NewLine()
            .AppendLogUntilMessage();
            
        if (!returnIndent)
            builder
                .Space(3)
                .VerticalLine(BoxDrawing.LineStyle.Light);
        else
            builder
                .Corner(BoxDrawing.CornerType.TopLeft, BoxDrawing.SolidStyle.Light)
                .HorizontalLine(BoxDrawing.LineStyle.Light, 2)
                .Corner(BoxDrawing.CornerType.BottomRight, BoxDrawing.SolidStyle.Light);
        return builder
            .Space()
            .Append(line);
    }
    
    internal static TableBuilder AppendIndentedLogExtra(this TableBuilder builder, object extra, bool returnIndent = false)
    {
        Queue<string> lines = new(JsonSerializer
            .Serialize(extra, JsonHelper.PrettyPrintingOptions)
            .Split(Environment.NewLine));
        builder.AppendIndentedLogTitle(extra is Exception ? "Exception Detail" : "Log Data", !returnIndent, lines.Count > 1);
            
        while (lines.TryDequeue(out string line))
            builder.AppendIndentedLogMessageLine(line, returnIndent: returnIndent && lines.Count == 0);
        return builder;
    }
    #endregion
}