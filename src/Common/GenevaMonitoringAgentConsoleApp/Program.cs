namespace GenevaMonitoringAgentConsoleApp
{
    using System;

    class Program
    {
        // This program is never run.  This csproj file is used to build the Geneva Monitoring application package for service fabric.
        // This doesn't work in an sfproj file, so the suggested method is to have a dummy csproj application. 
        static void Main()
        {

            Console.WriteLine("Hello World!");
            Console.ReadKey();
        }
    }
}
