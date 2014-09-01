using System;
using System.ServiceProcess;

namespace Terminator
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static void Main()
        {
            var serviceToRun = new TerminatorService();
            if (Environment.UserInteractive)
            {
                // This used to run the service as a console (development phase only)

                serviceToRun.Start();

                Console.WriteLine("Press Enter to terminate ...");
                Console.ReadLine();

                serviceToRun.Stop();
            }
            else
            {
                ServiceBase.Run(serviceToRun);
            }
        }
    }
}
