using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using ByteSizeLib;
using JetBrains.Annotations;
using NLog;
using Timer = System.Timers.Timer;

namespace Watchdog
{
    public class WatchDogController
    {
        static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        readonly Settings                _settings;
        readonly ExecutionFlowController _execution;
        readonly long                    _ramPrivateByteLimit;
        readonly TimeSpan                _checkingInterval;
        readonly TimeSpan                _restartInterval;
        readonly SemaphoreSlim           _semaphore = new SemaphoreSlim(1, 1);

        public WatchDogController(Settings settings, ExecutionFlowController execution)
        {
            _settings  = settings;
            _execution = execution;

            if (!string.IsNullOrEmpty(settings.RamLimit))
            {
                if (!ByteSize.TryParse(settings.RamLimit, out var ramLimit))
                {
                    Logger.Fatal("Cannot parse memory limit");
                    _execution.ExitWithCode(ExitCode.SettingsParseError);
                }
                _ramPrivateByteLimit = (long) ramLimit.Bytes;
                Logger.Info($"Private memory limit is set to {_ramPrivateByteLimit} bytes");
            }
            if (settings.RestartIntervalSeconds != 0)
            {
                _restartInterval = TimeSpan.FromSeconds(settings.RestartIntervalSeconds);
                Logger.Info($"Restart interval is set to {_restartInterval}");
            }
            _checkingInterval = TimeSpan.FromSeconds(_settings.CheckIntervalSeconds);
        }

        public async Task Start()
        {
            await StartImpl();
        }

        async Task StartImpl([CanBeNull] Process oldProcess = null)
        {
            while (true)
            {
                var process = StartProcess(oldProcess);
                oldProcess = await CheckingLoop(process);
            }
        }

        Process StartProcess([CanBeNull] Process oldProcess)
        {
            lock (this)
            {
                if (oldProcess != null && !oldProcess.HasExited)
                    throw new InvalidOperationException();
                var process = Process.Start(_settings.Filename, _settings.Arguments);
                if (process == null)
                {
                    Logger.Fatal("Cannot start process");
                    _execution.ExitWithCode(ExitCode.CannotStartProcess);
                }
                return process;
            }
        }

        async Task<Process> CheckingLoop(Process process)
        {
            if (_restartInterval != default)
            {
                var timer = new Timer(_restartInterval.TotalMilliseconds);
                timer.Start();
                timer.Elapsed += async (sender, args) =>
                {
                    await _semaphore.WaitAsync();
                    try
                    {
                        if (!process.HasExited)
                        {
                            Logger.Info("Stopping process due to restart interval");
                            await StopProcess();
                        }
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                };
            }

            while (true)
            {
                await _semaphore.WaitAsync();

                try
                {
                    if (process.HasExited)
                    {
                        Logger.Warn("Process has exited and not yet restarted!");
                        return process;
                    }

                    if (_ramPrivateByteLimit > 0)
                    {
                        long processRamUsageBytes = process.PrivateMemorySize64;
                        if (processRamUsageBytes > _ramPrivateByteLimit)
                        {
                            Logger.Info($"Process RAM usage is {processRamUsageBytes} which is more than allowed limit of {_ramPrivateByteLimit} bytes");
                            await StopProcess();
                        }
                    }
                }
                finally
                {
                    _semaphore.Release();
                }

                await Task.Delay(_checkingInterval);
            }

            /* --- --- */

            async Task StopProcess()
            {
                Logger.Info("Trying to close process gracefully...");
                process.CloseMainWindow();
                await Task.Delay(TimeSpan.FromSeconds(10));
                if (!process.HasExited)
                {
                    Logger.Warn("Cannot close process gracefully, killing it...");
                    process.Kill();
                }
                Logger.Info("Process stopped!");
            }
        }
    }
}