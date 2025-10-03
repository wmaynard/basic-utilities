using System.Text;
using System.Text.Json;
using Maynard.Auth;
using Maynard.Configuration;
using Maynard.ErrorHandling;
using Maynard.Json.Utilities;
using Maynard.Text;
using Maynard.Time;
using Maynard.Tools.Extensions;

namespace Maynard.Logging;

public class LogData
{
    public Severity Severity { get; set; }
    public string Message { get; set; }
    public TokenInfo TokenInfo { get; set; }
    public long Timestamp { get; set; }
    public string Url { get; set; }
    public object Data { get; set; }
    public string ApplicationVersion { get; set; }
    public string LibraryVersion { get; set; }
    public int OwnerId { get; set; }
    public Exception Exception { get; set; }

    private string OwnerName => Log.Configuration.OwnerNames.TryGetValue(OwnerId, out string name)
        ? name
        : Log.Configuration.OwnerNames[Log.Configuration.DefaultOwner];

    private static readonly int _lengthOwner = Math.Max(5, Log.Configuration.OwnerNames.Values.Max(name => name.Length));
    private static readonly int _lengthSeverity = Enum.GetNames<Severity>().Max(name => name.Length);
    private const int LENGTH_MESSAGE = 150;

    internal static string GetHeaders() => new TableBuilder()
        .Cell(LogConfiguration.PrintingConfiguration.HEADER_TIMESTAMP, Log.Configuration.Printing.LengthTimestampColumn)
        .VerticalLine(BoxDrawing.LineStyle.Heavy)
        .Cell(LogConfiguration.PrintingConfiguration.HEADER_OWNER, Log.Configuration.Printing.LengthOwnerColumn)
        .VerticalLine(BoxDrawing.LineStyle.Heavy)
        .Cell(LogConfiguration.PrintingConfiguration.HEADER_SEVERITY, Log.Configuration.Printing.LengthSeverityColumn)
        .VerticalLine(BoxDrawing.LineStyle.Heavy)
        .Cell(LogConfiguration.PrintingConfiguration.HEADER_MESSAGE, Log.Configuration.Printing.LengthMessageColumn)
        .NewLine()
        .HorizontalLine(BoxDrawing.LineStyle.Heavy, Log.Configuration.Printing.LengthTimestampColumn)
        .Cross(BoxDrawing.SolidStyle.LightAndHeavyMixed, BoxDrawing.Direction.Up | BoxDrawing.Direction.Left | BoxDrawing.Direction.Right)
        .HorizontalLine(BoxDrawing.LineStyle.Heavy, Log.Configuration.Printing.LengthOwnerColumn)
        .Cross(BoxDrawing.SolidStyle.LightAndHeavyMixed, BoxDrawing.Direction.Up | BoxDrawing.Direction.Left | BoxDrawing.Direction.Right)
        .HorizontalLine(BoxDrawing.LineStyle.Heavy, Log.Configuration.Printing.LengthSeverityColumn)
        .Cross(BoxDrawing.SolidStyle.LightAndHeavyMixed, BoxDrawing.Direction.Up | BoxDrawing.Direction.Left | BoxDrawing.Direction.Right)
        .HorizontalLine(BoxDrawing.LineStyle.Heavy, Log.Configuration.Printing.LengthMessageColumn);
    
    
    
    // Timestamp | Owner | Severity | Message
    // 
    // Timestamp | Owner | Severity | This is a sample message that will wrap if too long
    //                                Continued message here, can be disabled with DisableLineWrap()
    public override string ToString()
    {
        TableBuilder builder = new TableBuilder()
            .AppendLogUntilMessage(Log.Configuration.Printing.TimestampToString(Timestamp), OwnerName, Severity.ToString().ToUpper())
            .VerticalLine(BoxDrawing.LineStyle.Light)
            .Space();
        
        // If the log is short or wrap is disabled, simply add it to our output.
        if (Log.Configuration.DisableWrap || Message.Length <= Log.Configuration.Printing.LengthMessageColumn)
            builder.Append(Message);
        // The log is too long and we need to wrap the words for printing.
        else
        {
            Queue<string> words = new(Message.Split(' '));
            int length = 0;
            bool wordAdded = false;
            while (words.TryDequeue(out string word))
            {
                if (length + word.Length + 1 > Log.Configuration.Printing.LengthMessageColumn && wordAdded)
                    builder
                        .NewLine()
                        .AppendLogUntilMessage()
                        .VerticalLine(BoxDrawing.LineStyle.Light)
                        .Space();
                builder
                    .Space()
                    .Append(word);
                length += word.Length + 1;
                wordAdded = true;
            }
        }
        
        if (Severity < Log.Configuration.MinimumExtraSeverity || (Data ?? Exception) == null)
            return builder;
        bool printData = Data != null && Log.Configuration.Printing.PrintData;
        if (printData)
            builder.AppendIndentedLogExtra(Data, returnIndent: Exception == null);
        if (Exception != null && Log.Configuration.Printing.PrintExceptions)
            builder.AppendIndentedLogExtra(Exception, printData);
        
        return builder;
    }


}

internal static class TableBuilderExtension
{
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
}