using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

using System.Text.RegularExpressions;

namespace DropboxExtensionService
{
    static class Program
    {
        #region Main Function
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new DropboxExtensionService()
            };
            ServiceBase.Run(ServicesToRun);
        }
        #endregion
    }
}
