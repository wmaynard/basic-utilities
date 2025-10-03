using System.Text;
using System.Text.Json;
using Maynard.Auth;
using Maynard.Configuration;
using Maynard.ErrorHandling;
using Maynard.Extensions;
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
    
    
    /* Sample Log
    Timestamp              ┃ Owner ┃ Level ┃ Message                                                                                                                                              
    ━━━━━━━━━━━━━━━━━━━━━━━╇━━━━━━━╇━━━━━━━╇━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    [PDT] 17:00:04.441     │ Will  │ GOOD  │ Logging configured successfully!
    [PDT] 17:00:04.655     │ Will  │ GOOD  │ Connected to MongoDB.
    [PDT] 17:00:04.660     │ Will  │ GOOD  │ Mongo configured successfully!
    [PDT] 17:00:06.508     │ Will  │ GOOD  │ Maynard Tools configured successfully!
    [PDT] 17:00:06.552     │ Will  │ ERROR │ This is a dummy error.
                           ┊       ┊       └──┬── Log Data
                           ┊       ┊          │ {
                           ┊       ┊          │   "foo": "Bar"
                           ┊       ┊          │ }
                           ┊       ┊          ├── Exception Detail
                           ┊       ┊          │ {
                           ┊       ┊          │   "Message": "This is a dummy exception.",
                           ┊       ┊          │   "StackTrace": null,
                           ┊       ┊          │   "Type": "Exception"
                           ┊       ┊       ┌──┘ }
    [PDT] 17:00:06.556     │ Will  │ GOOD  │ This is a dummy good message.
    */
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