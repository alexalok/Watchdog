using System;
using JetBrains.Annotations;

namespace Watchdog
{
    public class ExecutionFlowController
    {
        [ContractAnnotation("=> halt")]
        public void ExitWithCode(ExitCode exitCode)
        {
            if (exitCode == ExitCode.Ok)
                throw new ArgumentException();
            Exit(exitCode);
        }

        [ContractAnnotation("=> halt")]
        public void ExitGracefully()
        {
            Exit(ExitCode.Ok);
        }

        [ContractAnnotation("=> halt")]
        void Exit(ExitCode exitCode)
        {
            WaitForUser();
            Environment.Exit((int) exitCode);
        }

        void WaitForUser()
        {
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }

    public enum ExitCode
    {
        Ok,
        CannotStartProcess,
        SettingsParseError
    }
}