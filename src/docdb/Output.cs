using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;

namespace DocDB;

public class Output : IOutput
{
    private class OutputColors
    {
        public OutputColors()
        {
            if (!Console.IsOutputRedirected)
            {
                var background = Console.BackgroundColor;
                var foreground = Console.ForegroundColor;

                if (!IsDarkColor(background))
                {
                    ErrorColor = ConsoleColor.DarkRed;
                    WarningColor = ConsoleColor.DarkYellow;
                    DebugColor = ConsoleColor.DarkGray;
                    MessageColor = foreground;
                }
            }
        }

        public Color ErrorColor { get; set; } = ConsoleColor.Red;
        public Color WarningColor { get; set; } = ConsoleColor.Yellow;
        public Color DebugColor { get; set; } = ConsoleColor.Gray;
        public Color MessageColor { get; set; } = ConsoleColor.White;

        private static bool IsDarkColor(Color color) => IsDarkColor(color.R, color.G, color.B);
        private static bool IsDarkColor(int r, int g, int b)
        {
            double brightness = Math.Sqrt(0.299 * Math.Pow(r, 2) + 0.587 * Math.Pow(g, 2) + 0.114 * Math.Pow(b, 2));
            return brightness < 128;
        }
    }

    private static readonly Lazy<OutputColors> s_outputColors = new(() => new());


    public Output(bool isDebugEnabled, bool warningsAsErrors)
    {
        IsDebugEnabled = isDebugEnabled;
        WarningsAsErrors = warningsAsErrors;
    }

    public bool HasErrors { get; private set; }
    public bool IsDebugEnabled { get; }
    public bool WarningsAsErrors { get; }

    public void Error(string message)
    {
        WriteMessage(s_outputColors.Value.ErrorColor, "error: ", message);
        HasErrors = true;
    }

    public void Warning(string message)
    {
        if (WarningsAsErrors)
        {
            HasErrors = true;
            WriteMessage(s_outputColors.Value.ErrorColor, "warning: ", message);
        }
        else
        {
            WriteMessage(s_outputColors.Value.WarningColor, "warning: ", message);
        }
    }

    public void Message(string message)
    {
        WriteMessage(s_outputColors.Value.MessageColor, null, message);
    }

    public void Debug(string message)
    {
        WriteMessage(s_outputColors.Value.DebugColor, null, message);
    }

    internal static void WriteError(string message)
    {
        WriteMessage(s_outputColors.Value.ErrorColor, "error: ", message);
    }

    internal static void WriteException(Exception ex)
    {
        if (ex is CommandAppException cae)
        {
            if (cae.Pretty is { } pretty)
            {
                AnsiConsole.Write(pretty);
            }
            else
            {
                WriteError(ex.Message);
            }
        }
        else
        {
            AnsiConsole.WriteException(ex, new ExceptionSettings()
            {
                Format = ExceptionFormats.ShortenEverything,
                Style = new()
                {
                    Exception = s_outputColors.Value.MessageColor,
                    Message = s_outputColors.Value.ErrorColor,
                    Method = s_outputColors.Value.MessageColor,
                    Parenthesis = s_outputColors.Value.MessageColor,
                    ParameterName = s_outputColors.Value.DebugColor,
                    ParameterType = s_outputColors.Value.DebugColor,
                    LineNumber = s_outputColors.Value.MessageColor,
                    Path = s_outputColors.Value.MessageColor
                },
            });
        }
    }

    private static void WriteMessage(Style? style, string? prefix, string message)
    {
        AnsiConsole.Console.Write(new NonBreakingText($"docdb: {prefix}{message}\n", style ?? Style.Plain));
    }

    class NonBreakingText(string text, Style style) : IRenderable
    {
        private readonly Paragraph _paragraph = new(text, style);

        public Measurement Measure(RenderOptions options, int maxWidth) => ((IRenderable)_paragraph).Measure(options, int.MaxValue);
        public IEnumerable<Segment> Render(RenderOptions options, int maxWidth) => ((IRenderable)_paragraph).Render(options, int.MaxValue);
    }
}
