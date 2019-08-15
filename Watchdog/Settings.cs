using System;
using ByteSizeLib;
using CommandLine;
using NLog;

namespace Watchdog
{
    public class Settings
    {
        [Option('p', HelpText = "Process filename to start.", Required = true)]
        public string Filename { get; }

        [Option('a', HelpText = "Arguments to launch process with.", Default = null)]
        public string Arguments { get; }

        [Option('r', HelpText = "Restart application automatically if it crashes.", Default = false)]
        public bool RestartOnCrash { get; }

        [Option("ram-limit", HelpText = "Max amount of ram (private bytes) application can allocate before being restarted. Examples: 100mb, 1024Kb.", Default = null)]
        public string RamLimit { get; }

        [Option('l', HelpText = "Logging level.", Default = LogLevel.Info)]
        public LogLevel LoggingLevel { get; }

        [Option('i', HelpText = "Checking interval, in seconds. Doesn't apply to application exit event - in that case, it is restarted immediately.", Default = 10)]
        public int CheckIntervalSeconds { get; }

        [Option("restart-interval-seconds", Default = 0, HelpText = "If set, causes watchdog to force-restart application every x seconds. If 0, feature is disabled.")]
        public int RestartIntervalSeconds { get; }

        public Settings(string filename, string arguments, bool restartOnCrash, string ramLimit, LogLevel loggingLevel, int checkIntervalSeconds, int restartIntervalSeconds)
        {
            Filename             = filename;
            Arguments            = arguments;
            RestartOnCrash       = restartOnCrash;
            RamLimit             = ramLimit;
            LoggingLevel         = loggingLevel;
            CheckIntervalSeconds = checkIntervalSeconds;
            RestartIntervalSeconds = restartIntervalSeconds;
        }

        public enum LogLevel
        {
            Trace,
            Debug,
            Info,
            Warn,
            Error,
            Fatal,
            Off
        }
    }
}