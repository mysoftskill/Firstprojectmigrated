using System;

namespace StandingQueryExtensionManagerApp
{
    class Program
    {
        // This program is never run.  This csproj file is used to build the Geneva SQE manager application package for service fabric.
        // This doesn't work in an sfproj file, so the suggested method is to have a dummy csproj application. 
        static void Main()
        {
            Console.WriteLine("Hello World!");
            Console.ReadKey();
        }
    }
}
