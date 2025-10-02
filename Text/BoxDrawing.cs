using System.Runtime.CompilerServices;
using System.Text;
using Maynard.ErrorHandling;
using Maynard.Extensions;

namespace Maynard.Text;

/* Unicode helper class for drawing boxes; very useful for pretty-printing.
             0    1    2    3    4    5    6    7    8    9    A    B    C    D    E    F
   U+250x    ─    ━    │    ┃    ┄    ┅    ┆    ┇    ┈    ┉    ┊    ┋    ┌    ┍    ┎    ┏
   U+251x    ┐    ┑    ┒    ┓    └    ┕    ┖    ┗    ┘    ┙    ┚    ┛    ├    ┝    ┞    ┟
   U+252x    ┠    ┡    ┢    ┣    ┤    ┥    ┦    ┧    ┨    ┩    ┪    ┫    ┬    ┭    ┮    ┯
   U+253x    ┰    ┱    ┲    ┳    ┴    ┵    ┶    ┷    ┸    ┹    ┺    ┻    ┼    ┽    ┾    ┿
   U+254x    ╀    ╁    ╂    ╃    ╄    ╅    ╆    ╇    ╈    ╉    ╊    ╋    ╌    ╍    ╎    ╏
   U+255x    ═    ║    ╒    ╓    ╔    ╕    ╖    ╗    ╘    ╙    ╚    ╛    ╜    ╝    ╞    ╟
   U+256x    ╠    ╡    ╢    ╣    ╤    ╥    ╦    ╧    ╨    ╩    ╪    ╫    ╬    ╭    ╮    ╯
   U+257x    ╰    ╱    ╲    ╳    ╴    ╵    ╶    ╷    ╸    ╹    ╺    ╻    ╼    ╽    ╾    ╿
 */

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

public static class BoxDrawing
{
    private static InternalException GenerateNewUnsupportedException(string parameterName, [CallerMemberName] string caller = null, params Enum[] enums)
    {
        List<object> parameters = [new
        {
            ParameterName = parameterName,
            Enums = enums
        }];
        parameters.AddRange(enums
            .Where(e => !e.IsDefault())
            .Select<Enum, object>(e =>
            {
                string name = e.GetType().Name;
                if (e.GetAttribute<FlagsAttribute>() == null)
                    return new
                    {
                        EnumType = name,
                        Value = e.GetDisplayName()
                    };

                Enum[] flags = e.GetFlags();
                return !flags.Any()
                    ? null
                    : new
                    {
                        EnumType = name,
                        Flags = flags.Select(flag => flag.GetDisplayName())
                    };
            })
            .Where(obj => obj != null));
            
        return new InternalException($"Unsupported value in {caller} for parameter '{parameterName}'.", ErrorCode.InvalidValue, data: parameters);
    }
    public static char X() => '╳';
    public static char Cross(SolidStyle style, Direction styleApplication = default)
    {
        InternalException NewUnsupportedException(string parameterName) => GenerateNewUnsupportedException(parameterName, nameof(Cross), style, styleApplication);
        return style switch
        {
            SolidStyle.Light => '┼',
            SolidStyle.Heavy => '╋',
            SolidStyle.Double => '╬',
            SolidStyle.LightAndHeavyMixed => styleApplication switch
            {
                Direction.Up => '╀',
                Direction.Right => '┾',
                Direction.Down => '╁',
                Direction.Left => '┽',
                Direction.Up | Direction.Right => '╄',
                Direction.Right | Direction.Down => '╆',
                Direction.Down | Direction.Left => '╅',
                Direction.Left | Direction.Up => '╃',
                Direction.Up | Direction.Down => '╂',
                Direction.Right | Direction.Left => '┿',
                Direction.Up | Direction.Right | Direction.Down => '╊',
                Direction.Right | Direction.Down | Direction.Left => '╈',
                Direction.Down | Direction.Left | Direction.Up => '╉',
                Direction.Left | Direction.Up | Direction.Right => '╇',
                _ => throw NewUnsupportedException(nameof(styleApplication))
            },
            SolidStyle.LightAndDoubleMixed => styleApplication switch
            {
                Direction.Right | Direction.Down => '╪',
                Direction.Up | Direction.Down => '╫',
                _ => throw NewUnsupportedException(nameof(styleApplication))
            },
            _ => throw NewUnsupportedException(nameof(style))
        };
    }
    public static string HorizontalLine(LineStyle style, int length) => new(Line(LineDirection.Horizontal, style), length);
    public static char VerticalLine(LineStyle style, Direction styleApplication = Direction.Up | Direction.Down) => Line(LineDirection.Vertical, style, styleApplication);
    public static char Line(LineDirection direction, LineStyle style, Direction styleApplication = default)
    {
        InternalException NewUnsupportedException(string parameterName) => GenerateNewUnsupportedException(parameterName, nameof(Line), direction, style, styleApplication);
        return direction switch
        {
            LineDirection.Up => style switch
            {
                LineStyle.Light => '╵',
                LineStyle.Heavy => '╹',
                _ => throw NewUnsupportedException(nameof(style))
            },
            LineDirection.Right => style switch
            {
                LineStyle.Light => '╶',
                LineStyle.Heavy => '╺',
                _ => throw NewUnsupportedException(nameof(style))
            },
            LineDirection.Down => style switch
            {
                LineStyle.Light => '╷',
                LineStyle.Heavy => '╻',
                _ => throw NewUnsupportedException(nameof(style))
            },
            LineDirection.Left => style switch
            {
                LineStyle.Light => '╴',
                LineStyle.Heavy => '╸',
                _ => throw NewUnsupportedException(nameof(style))
            },
            LineDirection.Vertical => style switch
            {
                LineStyle.Light => '│',
                LineStyle.Heavy => '┃',
                LineStyle.Double => '║',
                LineStyle.Dashes => '┊',
                LineStyle.HeavyDashes => '┋',
                LineStyle.LongDashes => '╎',
                LineStyle.LongHeavyDashes => '╏',
                LineStyle.LightAndHeavyMixed => styleApplication switch
                {
                    Direction.Up => '╿',
                    Direction.Down => '╽',
                    _ => throw NewUnsupportedException(nameof(styleApplication))
                },
                _ => throw NewUnsupportedException(nameof(style))
            },
            LineDirection.Horizontal => style switch
            {
                LineStyle.Light => '─',
                LineStyle.Heavy => '━',
                LineStyle.Double => '═',
                LineStyle.Dashes => '┈',
                LineStyle.HeavyDashes => '┉',
                LineStyle.LongDashes => '╌',
                LineStyle.LongHeavyDashes => '╍',
                LineStyle.LightAndHeavyMixed => styleApplication switch
                {
                    Direction.Right => '╼',
                    Direction.Left => '╾',
                    _ => throw NewUnsupportedException(nameof(styleApplication))
                },
                _ => throw NewUnsupportedException(nameof(style))
            },
            LineDirection.DiagonalDown => '╲',
            LineDirection.DiagonalUp => '╱',
            _ => throw NewUnsupportedException(nameof(direction))
        };
    }
    public static char Corner(CornerType type, SolidStyle style, Direction styleApplication = default)
    {
        InternalException NewUnsupportedException(string parameterName) => GenerateNewUnsupportedException(parameterName, nameof(Corner), type, style, styleApplication);
        return type switch
        {
            CornerType.TopRightArc => '╮',
            CornerType.BottomRightArc => '╯',
            CornerType.BottomLeftArc => '╰',
            CornerType.TopLeftArc => '╭',
            CornerType.TopRight => style switch
            {
                SolidStyle.Light => '┐',
                SolidStyle.Heavy => '┓',
                SolidStyle.Double => '╗',
                SolidStyle.LightAndHeavyMixed => styleApplication switch
                {
                    Direction.Up => '┑',
                    Direction.Right => '┒',
                    _ => throw NewUnsupportedException(nameof(styleApplication))
                },
                SolidStyle.LightAndDoubleMixed => styleApplication switch
                {
                    Direction.Up => '╕',
                    Direction.Right => '╖',
                    _ => throw NewUnsupportedException(nameof(styleApplication))
                },
                _ => throw NewUnsupportedException(nameof(style))
            },
            CornerType.BottomRight => style switch
            {
                SolidStyle.Light => '┘',
                SolidStyle.Heavy => '┛',
                SolidStyle.Double => '╝',
                SolidStyle.LightAndHeavyMixed => styleApplication switch
                {
                    Direction.Right => '┚',
                    Direction.Down => '┙',
                    _ => throw NewUnsupportedException(nameof(styleApplication))
                },
                SolidStyle.LightAndDoubleMixed => styleApplication switch
                {
                    Direction.Right => '╜',
                    Direction.Down => '╛',
                    _ => throw NewUnsupportedException(nameof(styleApplication))
                },
                _ => throw NewUnsupportedException(nameof(style))
            },
            CornerType.BottomLeft => style switch
            {
                SolidStyle.Light => '└',
                SolidStyle.Heavy => '┗',
                SolidStyle.Double => '╚',
                SolidStyle.LightAndHeavyMixed => styleApplication switch
                {
                    Direction.Down => '┕',
                    Direction.Left => '╙',
                    _ => throw NewUnsupportedException(nameof(styleApplication))
                },
                SolidStyle.LightAndDoubleMixed => styleApplication switch
                {
                    Direction.Down => '╘',
                    Direction.Left => '╙',
                    _ => throw NewUnsupportedException(nameof(styleApplication))
                },
                _ => throw NewUnsupportedException(nameof(style))
            },
            CornerType.TopLeft => style switch
            {
                SolidStyle.Light => '┌',
                SolidStyle.Heavy => '┏',
                SolidStyle.Double => '╔',
                SolidStyle.LightAndHeavyMixed => styleApplication switch
                {
                    Direction.Up => '╒',
                    Direction.Left => '┎',
                    _ => throw NewUnsupportedException(nameof(styleApplication))
                },
                SolidStyle.LightAndDoubleMixed => styleApplication switch
                {
                    Direction.Up => '╘',
                    Direction.Left => '╓',
                    _ => throw NewUnsupportedException(nameof(styleApplication))
                },
                _ => throw NewUnsupportedException(nameof(style))
            },
            _ => throw NewUnsupportedException(nameof(type))
        };
    }

    public static char Tee(TeeType direction, SolidStyle style, Direction styleApplication = default)
    {
        InternalException NewUnsupportedException(string parameterName) => GenerateNewUnsupportedException(parameterName, nameof(Tee), direction, style, styleApplication);
        // For sanity's sake, the order in which the code is organized is clockwise: Up > Right > Down > Left
        return direction switch
        {
            TeeType.FacingUp => style switch
            {
                SolidStyle.Light => '┴',
                SolidStyle.Heavy => '┻',
                SolidStyle.Double => '╩',
                SolidStyle.LightAndHeavyMixed => styleApplication switch
                {
                    Direction.Up => '┸',
                    Direction.Right => '┶',
                    Direction.Left => '┵',
                    Direction.Up | Direction.Right => '┺',
                    Direction.Up | Direction.Left => '┹',
                    Direction.Right | Direction.Left => '┷',
                    _ => throw NewUnsupportedException(nameof(styleApplication))
                },
                SolidStyle.LightAndDoubleMixed => styleApplication switch
                {
                    Direction.Up => '╨',
                    Direction.Right | Direction.Left => '╧',
                    _ => throw NewUnsupportedException(nameof(styleApplication))
                },
                _ => throw NewUnsupportedException(nameof(style))
            },
            TeeType.FacingRight => style switch
            {
                SolidStyle.Light => '├',
                SolidStyle.Heavy => '┣',
                SolidStyle.Double => '╠',
                SolidStyle.LightAndHeavyMixed => styleApplication switch
                {
                    Direction.Up => '┞',
                    Direction.Right => '┝',
                    Direction.Down => '┟',
                    Direction.Up | Direction.Right => '┡',
                    Direction.Up | Direction.Down => '┠',
                    Direction.Right | Direction.Down => '┢',
                    _ => throw NewUnsupportedException(nameof(styleApplication))
                },
                SolidStyle.LightAndDoubleMixed => styleApplication switch
                {
                    Direction.Right => '╞',
                    Direction.Up | Direction.Down => '╟',
                    _ => throw NewUnsupportedException(nameof(styleApplication))
                },
                _ => throw NewUnsupportedException(nameof(style))
            },
            
            TeeType.FacingDown => style switch
            {
                SolidStyle.Light => '┬',
                SolidStyle.Heavy => '┳',
                SolidStyle.Double => '╦',
                SolidStyle.LightAndHeavyMixed => styleApplication switch
                {
                    Direction.Right => '┮',
                    Direction.Down => '┰',
                    Direction.Left => '┭',
                    Direction.Right | Direction.Down => '┲',
                    Direction.Right | Direction.Left => '┯',
                    Direction.Down | Direction.Left => '┱',
                    _ => throw NewUnsupportedException(nameof(styleApplication))
                },
                SolidStyle.LightAndDoubleMixed => styleApplication switch
                {
                    Direction.Down => '╥',
                    Direction.Right | Direction.Left => '╤',
                    _ => throw NewUnsupportedException(nameof(styleApplication))
                },
                _ => throw NewUnsupportedException(nameof(style))
            },
            TeeType.FacingLeft => style switch
            {
                SolidStyle.Light => '┤',
                SolidStyle.Heavy => '┫',
                SolidStyle.Double => '╣',
                SolidStyle.LightAndHeavyMixed => styleApplication switch
                {
                    Direction.Up => '┦',
                    Direction.Down => '┧',
                    Direction.Left => '┥',
                    Direction.Up | Direction.Down => '┨',
                    Direction.Up | Direction.Left => '┩',
                    Direction.Down | Direction.Left => '┪',
                    _ => throw NewUnsupportedException(nameof(styleApplication))
                },
                SolidStyle.LightAndDoubleMixed => styleApplication switch
                {
                    Direction.Left => '╡',
                    Direction.Up | Direction.Down => '╢',
                    _ => throw NewUnsupportedException(nameof(styleApplication))
                },
                _ => throw NewUnsupportedException(nameof(style))
            },
            _ => throw NewUnsupportedException(nameof(direction))
        };
    }
    
    public enum TeeType
    {
        FacingUp = 10,
        FacingRight = 20,
        FacingDown = 30,
        FacingLeft = 40
    }

    public enum SolidStyle
    {
        Light = 10,
        Heavy = 20,
        Double = 30,
        LightAndHeavyMixed = 40,
        LightAndDoubleMixed = 50
    }

    public enum LineStyle
    {
        Light = 10,
        Heavy = 20,
        LightAndHeavyMixed = 30,
        Double = 40,
        Dashes = 50,
        LongDashes = 60,
        HeavyDashes = 70,
        LongHeavyDashes = 80
    }

    [Flags]
    public enum Direction
    {
        Up = 0b_0000_0001,
        Down = 0b_0000_0010,
        Left = 0b_0000_0100,
        Right = 0b_0000_1000
    }

    public enum CornerType
    {
        TopRight = 10,
        BottomRight = 20,
        BottomLeft = 30,
        TopLeft = 40,
        TopRightArc = 50,
        BottomRightArc = 60,
        BottomLeftArc = 70,
        TopLeftArc = 80
    }

    public enum LineDirection
    {
        Up = 10,
        Down = 20,
        Left = 30,
        Right = 40,
        Vertical = 50,
        Horizontal = 60,
        DiagonalDown = 70,
        DiagonalUp = 80
    }
}
