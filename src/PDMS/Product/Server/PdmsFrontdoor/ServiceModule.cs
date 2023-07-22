namespace Microsoft.PrivacyServices.DataManagement.Frontdoor
{
    using System;
    using System.Diagnostics;
    using System.Linq;

    public static class ServiceModule
    {
        private const string ProcessName = "Microsoft.PrivacyServices.DataManagement.Frontdoor";

        public static void Stop()
        {
            var previousProcess = GetProcessByName(ProcessName, false);
            previousProcess?.Kill();
        }

        public static void StartInternal()
        {
            using (var process = Start())
            {
                void Print(object sender, DataReceivedEventArgs e)
                {
                    Log(e.Data);
                }

                process.OutputDataReceived += (DataReceivedEventHandler)Print;
                process.BeginOutputReadLine();

                process.ErrorDataReceived += (DataReceivedEventHandler)Print;
                process.BeginErrorReadLine();

                if (!process.WaitForExit(360000))
                {
                    process.Kill();
                    throw new Exception("Service start up failed.");
                }
            }
        }

        private static void Log(string message)
        {
            Trace.WriteLine(message);
            Trace.Flush();
        }

        private static Process Start()
        {
            Program.Main(null);
            return GetProcessByName(ProcessName, true);
        }

        private static Process GetProcessByName(string name, bool wait)
        {
            Process process = null;

            do
            {
                process = Process.GetProcessesByName(name).SingleOrDefault();
                System.Threading.Thread.Sleep(100);
            }
            while (process == null && wait);

            return process;
        }
    }
}