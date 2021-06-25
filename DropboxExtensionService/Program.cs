using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace DropboxExtensionService
{
    static class Program
    {
        #region Main Method
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            if (Environment.UserInteractive)
            {
                DropboxExtensionService service1 = new DropboxExtensionService();
                service1.TestStartupAndStop(args);
            }
            else
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                new DropboxExtensionService()
                };
                ServiceBase.Run(ServicesToRun);
            }
        }
        #endregion

    }
}
