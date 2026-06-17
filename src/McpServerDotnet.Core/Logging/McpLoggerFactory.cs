using Serilog;
using Serilog.Events;

namespace McpServerDotnet.Core.Logging;

/// <summary>
/// Factory that creates pre-configured Serilog <see cref="ILogger"/> instances
/// suitable for MCP server processes.
/// </summary>
/// <remarks>
/// All output is directed to <b>stderr</b> so that stdout remains exclusive to the
/// MCP protocol wire format. Writing anything to stdout from application code will
/// corrupt the communication channel.
/// </remarks>
public static class McpLoggerFactory
{
    /// <summary>
    /// Creates a Serilog logger that writes structured output to stderr.
    /// </summary>
    /// <param name="minimumLevel">The minimum event level to emit.</param>
    /// <param name="outputTemplate">
    /// The Serilog output template. Defaults to a compact format that includes
    /// timestamp, level, source context, and message.
    /// </param>
    public static Serilog.Core.Logger CreateStderrLogger(
        LogEventLevel minimumLevel = LogEventLevel.Information,
        string? outputTemplate = null)
    {
        const string DefaultTemplate =
            "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

        return new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(
                outputTemplate: outputTemplate ?? DefaultTemplate,
                standardErrorFromLevel: LogEventLevel.Verbose)
            .CreateLogger();
    }

    /// <summary>
    /// Creates a Serilog logger that writes to both stderr and a rolling log file.
    /// </summary>
    /// <param name="logDirectory">Directory in which to write log files.</param>
    /// <param name="minimumLevel">The minimum event level to emit.</param>
    public static Serilog.Core.Logger CreateFileLogger(
        string logDirectory = "logs",
        LogEventLevel minimumLevel = LogEventLevel.Information)
    {
        const string OutputTemplate =
            "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

        return new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(
                outputTemplate: OutputTemplate,
                standardErrorFromLevel: LogEventLevel.Verbose)
            .WriteTo.File(
                path: Path.Combine(logDirectory, "mcp-.log"),
                outputTemplate: OutputTemplate,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7)
            .CreateLogger();
    }
}
