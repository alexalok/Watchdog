using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ByteSizeLib;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using CommandLine;
using NLog;
using NLog.Targets;

namespace Watchdog
{
    class Program
    {
        static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        static async Task Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            var settings = ParseSettings(args);
            BootstrapLogging(settings);
            var container = BootstrapContainer(settings);

            var controller = container.Resolve<WatchDogController>();
            await controller.Start();
        }

        static Settings ParseSettings(IEnumerable<string> args)
        {
            Settings settings = null;
            Parser.Default.ParseArguments<Settings>(args).
                WithParsed(s => settings = s).
                WithNotParsed((e) =>
                {
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                    Environment.Exit(0);
                });
            return settings;
        }

        static void BootstrapLogging(Settings settings)
        {
            var config   = new NLog.Config.LoggingConfiguration();
            var minLevel = LogLevel.FromOrdinal((int) settings.LoggingLevel);

            var coloredConsoleTarget = new ColoredConsoleTarget();
            config.AddRule(minLevel, LogLevel.Fatal, coloredConsoleTarget);

            var fileTarget = new FileTarget()
            {
                FileName                = "logs/watchdog.log",
                ArchiveFileName         = "logs/watchdog.{#}.log",
                Layout                  = "${longdate}|\t${message}",
                ConcurrentWrites        = false,
                ArchiveAboveSize        = (long) ByteSize.FromMegaBytes(10).Bytes,
                KeepFileOpen            = true,
                OpenFileCacheTimeout    = (int) TimeSpan.FromSeconds(30).TotalSeconds,
                OpenFileCacheSize       = 20,
                AutoFlush               = false,
                OpenFileFlushTimeout    = (int) TimeSpan.FromSeconds(10).TotalSeconds,
                CreateDirs              = true,
                MaxArchiveFiles         = 5,
                ArchiveNumbering        = ArchiveNumberingMode.Rolling,
                ArchiveOldFileOnStartup = true,
            };
            config.AddRuleForAllLevels(fileTarget);

            LogManager.Configuration = config;
        }

        static IWindsorContainer BootstrapContainer(Settings settings)
        {
            var container = new WindsorContainer();

            container.Register(
                Component.For<Settings>().Instance(settings),
                Component.For<WatchDogController>().LifestyleSingleton(),
                Component.For<ExecutionFlowController>().LifestyleSingleton()
            );

            return container;
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.Fatal(e.ExceptionObject.ToString);
            if (e.IsTerminating)
                LogManager.Shutdown();
        }
    }
}