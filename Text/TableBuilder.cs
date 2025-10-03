using System.Text;

namespace Maynard.Text;

public class TableBuilder
{
    private readonly StringBuilder _builder = new();

    private TableBuilder Append<T>(Func<T> call)
    {
        _builder.Append(call());
        return this;
    }
    public TableBuilder Append(object value) => Append(() => value);

    public TableBuilder Cell(string text, int width, int padding = 1, Align alignment = Align.Left)
    {
        int space = width - text.Length;
        if (space <= 0)
        {
            _builder.Append(text);
            return this;
        }

        int leading = 0;
        int trailing = 0;
        
        switch (alignment)
        {
            case Align.Center:
                leading = Math.Min(padding, space / 2);
                trailing = space - leading;
                break;
            case Align.Right:
                trailing = Math.Min(padding, space);
                leading = space - trailing;
                break;
            case Align.Left:
            default:
                leading = Math.Min(padding, space);
                trailing = space - leading;
                break;
        }
        
        _builder.Append(' ', leading);
        _builder.Append(text);
        _builder.Append(' ', trailing);
        return this;
    }

    public TableBuilder X() => Append(BoxDrawing.X);
    public TableBuilder Space(int length = 1) => Append(() => new string(' ', length));

    public TableBuilder NewLine(int count = 1)
    {
        while (count-- > 0)
            _builder.Append(Environment.NewLine);
        return this;
    }
    public TableBuilder Cross(BoxDrawing.SolidStyle style, BoxDrawing.Direction styleApplication = default) 
        => Append(() => BoxDrawing.Cross(style, styleApplication));
    public TableBuilder HorizontalLine(BoxDrawing.LineStyle style, int length) 
        => Append(() => BoxDrawing.HorizontalLine(style, length));
    public TableBuilder VerticalLine(BoxDrawing.LineStyle style, BoxDrawing.Direction styleApplication = BoxDrawing.Direction.Up | BoxDrawing.Direction.Down) 
        => Append(() => BoxDrawing.VerticalLine(style, styleApplication));
    public TableBuilder Line(BoxDrawing.LineDirection direction, BoxDrawing.LineStyle style, BoxDrawing.Direction styleApplication = default) 
        => Append(() => BoxDrawing.Line(direction, style, styleApplication));
    
    public TableBuilder Corner(BoxDrawing.CornerType type, BoxDrawing.SolidStyle style, BoxDrawing.Direction styleApplication = default) 
        => Append(() => BoxDrawing.Corner(type, style, styleApplication));
    
    public TableBuilder Tee(BoxDrawing.TeeType direction, BoxDrawing.SolidStyle style, BoxDrawing.Direction styleApplication = default) 
        => Append(() => BoxDrawing.Tee(direction, style, styleApplication));

    public override string ToString() => _builder.ToString();
    
    public static implicit operator string(TableBuilder builder) => builder.ToString();

    public enum Align
    {
        Left,
        Center,
        Right
    }
}