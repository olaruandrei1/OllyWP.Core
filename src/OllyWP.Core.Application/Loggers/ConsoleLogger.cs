using OllyWP.Core.Domain.Constants;
using OllyWP.Core.Domain.Entities;

namespace OllyWP.Core.Application.Loggers;

/// <summary>
/// Static console logger for debugging OllyWP operations
/// Cross-platform safe with ASCII characters only
/// </summary>
public static class ConsoleLogger
{
    private static readonly Lock _lock = new();
    private static CancellationTokenSource? _loadingCts;

    public static bool UseColors { get; set; } = true;
    public static bool Enabled { get; set; } = true;

    #region Basic Logging

    /// <summary>
    /// Logs an informational message
    /// </summary>
    public static void Information(string message)
    {
        if (!Enabled) 
            return;

        lock (_lock)
        {
            StopLoading();
            WriteWithIcon(LoggerIcons.ICON_INFO, ConsoleColor.Cyan, "INFO", message);
        }
    }

    /// <summary>
    /// Logs a success message
    /// </summary>
    public static void Success(string message)
    {
        if (!Enabled)
            return;

        lock (_lock)
        {
            StopLoading();
            WriteWithIcon(LoggerIcons.ICON_SUCCESS, ConsoleColor.Green, "SUCCESS", message);
        }
    }

    /// <summary>
    /// Logs a warning message
    /// </summary>
    public static void Warning(string message)
    {
        if (!Enabled) 
            return;

        lock (_lock)
        {
            StopLoading();
            WriteWithIcon(LoggerIcons.ICON_WARNING, ConsoleColor.Yellow, "WARNING", message);
        }
    }

    /// <summary>
    /// Logs an error message
    /// </summary>
    public static void Error(string message, Exception? exception = null)
    {
        if (!Enabled)
            return;

        lock (_lock)
        {
            StopLoading();
            WriteWithIcon(LoggerIcons.ICON_ERROR, ConsoleColor.Red, "ERROR", message);

            if (exception == null) 
                return;

            if (UseColors) Console.ForegroundColor = ConsoleColor.DarkRed;

            Console.WriteLine($"      Exception: {exception.GetType().Name}");
            Console.WriteLine($"      Message: {exception.Message}");

            if (!string.IsNullOrWhiteSpace(exception.StackTrace))
            {
                var stackLines = exception.StackTrace.Split('\n').Take(3);
                foreach (var line in stackLines)
                {
                    Console.WriteLine($"      {line.Trim()}");
                }
            }

            if (UseColors) Console.ResetColor();
        }
    }

    /// <summary>
    /// Logs a fatal error (critical failure)
    /// </summary>
    public static void Fatal(string message, Exception? exception = null)
    {
        if (!Enabled)
            return;

        lock (_lock)
        {
            StopLoading();

            if (UseColors)
            {
                Console.BackgroundColor = ConsoleColor.DarkRed;
                Console.ForegroundColor = ConsoleColor.White;
            }

            Console.Write($" [{LoggerIcons.ICON_FATAL}] FATAL ");

            if (UseColors) 
                Console.ResetColor();

            if (UseColors) 
                Console.ForegroundColor = ConsoleColor.Red;

            Console.Write($" [{DateTime.Now:HH:mm:ss}] ");
            Console.WriteLine(message);

            if (UseColors) 
                Console.ResetColor();

            if (exception == null) 
                return;

            if (UseColors)
                Console.ForegroundColor = ConsoleColor.DarkRed;

            Console.WriteLine($"      {exception}");

            if (UseColors) 
                Console.ResetColor();
        }
    }

    /// <summary>
    /// Logs a debug message
    /// </summary>
    public static void Debug(string message)
    {
        if (!Enabled) 
            return;

        lock (_lock)
        {
            StopLoading();
            WriteWithIcon(LoggerIcons.ICON_DEBUG, ConsoleColor.Gray, "DEBUG", message);
        }
    }

    #endregion

    #region Loading Effects

    /// <summary>
    /// Starts a loading animation with rotating spinner and dots
    /// </summary>
    public static void StartLoading(string message)
    {
        if (!Enabled) 
            return;

        lock (_lock)
        {
            StopLoading();

            _loadingCts = new CancellationTokenSource();
            
            var token = _loadingCts.Token;

            Task.Run(async () =>
            {
                char[] frames = ['|', '/', '-', '\\'];
                string[] dots = ["", ".", "..", "..."];

                int frameIndex = 0;
                int dotIndex = 0;

                while (!token.IsCancellationRequested)
                {
                    lock (_lock)
                    {
                        if (UseColors) 
                            Console.ForegroundColor = ConsoleColor.Cyan;

                        Console.Write($"\r[{frames[frameIndex]}] {message}{dots[dotIndex]}   ");

                        if (UseColors) 
                            Console.ResetColor();
                    }

                    frameIndex = (frameIndex + 1) % frames.Length;

                    if (frameIndex == 0)
                        dotIndex = (dotIndex + 1) % dots.Length;

                    try
                    {
                        await Task.Delay(100, token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }

                lock (_lock)
                {
                    Console.Write("\r" + new string(' ', Math.Min(Console.WindowWidth - 1, 120)) + "\r");
                }
            }, token);
        }
    }

    /// <summary>
    /// Stops the current loading animation
    /// </summary>
    public static void StopLoading()
    {
        if (_loadingCts == null) return;

        _loadingCts.Cancel();
        _loadingCts.Dispose();
        _loadingCts = null;

        Thread.Sleep(50);
    }

    #endregion

    #region Progress Bar

    /// <summary>
    /// Displays a progress bar
    /// </summary>
    public static void Progress(int current, int total, string? message = null)
    {
        if (!Enabled) 
            return;

        lock (_lock)
        {
            const int barLength = 40;

            double percentage = (double)current / total;
            int filledLength = (int)(barLength * percentage);
            string bar = new string('#', filledLength) + new string('-', barLength - filledLength);
            string percentText = $"{percentage * 100:F0}%";

            if (UseColors) 
                Console.ForegroundColor = ConsoleColor.Cyan;

            Console.Write($"\r[{bar}] {percentText} ({current}/{total})");

            if (!string.IsNullOrWhiteSpace(message))
                Console.Write($" - {message}");

            if (UseColors) 
                Console.ResetColor();

            if (current >= total)
                Console.WriteLine();
        }
    }

    #endregion

    #region Results Display

    /// <summary>
    /// Displays single notification result (for when not using batches)
    /// </summary>
    public static void SingleResult(OllyResponse response)
    {
        if (!Enabled) return;

        lock (_lock)
        {
            Console.WriteLine();
            Console.WriteLine("============================================================");

            if (UseColors)
                Console.ForegroundColor = ConsoleColor.Cyan;
            
            Console.WriteLine("              NOTIFICATION RESULT                           ");
            
            if (UseColors)
                Console.ResetColor();

            Console.WriteLine("============================================================");

            WriteResultLine("Recipients", response.TotalRecipients.ToString(), ConsoleColor.White);

            if (response.Success)
                WriteResultLine("Status", "SUCCESS", ConsoleColor.Green);
            else
                WriteResultLine("Status", "FAILED", ConsoleColor.Red);

            WriteResultLine("Successful", response.SuccessfulDeliveries.ToString(), ConsoleColor.Green);
            WriteResultLine("Failed", response.FailedDeliveries.ToString(), ConsoleColor.Red);
            WriteResultLine("Elapsed Time", $"{response.ElapsedTime.TotalMilliseconds:F2}ms", ConsoleColor.Cyan);

            if (response.FailedDeliveries > 0)
            {
                Console.WriteLine();

                if (UseColors)
                    Console.ForegroundColor = ConsoleColor.Yellow;

                Console.WriteLine("Failed Recipients:");

                if (UseColors)
                    Console.ResetColor();

                foreach (var failed in response.FailedResults.Take(5))
                {
                    if (UseColors) 
                        Console.ForegroundColor = ConsoleColor.Red;
                    
                    Console.Write($"  [{LoggerIcons.ICON_ERROR}] ");
                    
                    if (UseColors) 
                        Console.ResetColor();
                    
                    Console.WriteLine($"{failed.Status}: {failed.ErrorMessage}");
                }

                if (response.FailedDeliveries > 5)
                    Console.WriteLine($"  ... and {response.FailedDeliveries - 5} more");
            }

            Console.WriteLine("============================================================");
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Displays batch processing results in a formatted table
    /// </summary>
    public static void BatchResults(OllyResponse response)
    {
        if (!Enabled) return;

        lock (_lock)
        {
            Console.WriteLine();
            Console.WriteLine("============================================================");

            if (UseColors) 
                Console.ForegroundColor = ConsoleColor.Cyan;

            Console.WriteLine("              BATCH PROCESSING RESULTS                      ");

            if (UseColors) 
                Console.ResetColor();

            Console.WriteLine("============================================================");

            WriteResultLine("Total Batches", response.TotalBatches.ToString(), ConsoleColor.White);
            WriteResultLine("Total Recipients", response.TotalRecipients.ToString(), ConsoleColor.White);
            WriteResultLine("Successful", response.SuccessfulDeliveries.ToString(), ConsoleColor.Green);
            WriteResultLine("Failed", response.FailedDeliveries.ToString(), ConsoleColor.Red);
            WriteResultLine("Elapsed Time", $"{response.ElapsedTime.TotalSeconds:F2}s", ConsoleColor.Cyan);

            Console.WriteLine("============================================================");
            Console.WriteLine();
        }
    }

    private static void WriteResultLine(string label, string value, ConsoleColor valueColor)
    {
        Console.Write($"{label,-20}: ");

        if (UseColors)
            Console.ForegroundColor = valueColor;

        Console.WriteLine(value);

        if (UseColors)
            Console.ResetColor();
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Clears the console
    /// </summary>
    public static void Clear()
    {
        if (!Enabled) return;

        Console.Clear();
    }

    /// <summary>
    /// Writes a blank line
    /// </summary>
    public static void NewLine()
    {
        if (!Enabled) return;

        Console.WriteLine();
    }

    /// <summary>
    /// Writes a separator line
    /// </summary>
    public static void Separator()
    {
        if (!Enabled) return;

        if (UseColors)
            Console.ForegroundColor = ConsoleColor.DarkGray;

        Console.WriteLine(new string('-', 60));

        if (UseColors)
            Console.ResetColor();
    }

    /// <summary>
    /// Writes a section header
    /// </summary>
    public static void Header(string text)
    {
        if (!Enabled) return;

        Console.WriteLine();

        if (UseColors)
            Console.ForegroundColor = ConsoleColor.Cyan;

        Console.WriteLine($"=== {text} ===");

        if (UseColors)
            Console.ResetColor();

        Console.WriteLine();
    }

    #endregion

    #region Helper Methods

    private static void WriteWithIcon(char icon, ConsoleColor color, string level, string message)
    {
        if (UseColors)
            Console.ForegroundColor = color;

        Console.Write($"[{icon}] [{level}]");

        if (UseColors)
            Console.ResetColor();

        Console.Write($" [{DateTime.Now:HH:mm:ss.fff}] ");
        Console.WriteLine(message);
    }

    #endregion
}