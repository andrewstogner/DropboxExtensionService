using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;

namespace DropboxExtensionService
{
    public partial class DropboxExtensionService : ServiceBase
    {
        #region Custom Structures
        /// <summary>
        /// Custom structure thats meant for collecting data from the AppSettings.exml file
        /// </summary>
        public struct group
        {
            /*public group (string path, string filename, string newfolder)
            {
                Path = path;
                Filename = filename;
                NewFolder = newfolder;
            }*/

            public string Path; //{ get; }
            public string Filename; //{ get; }
            public string NewFolder; //{ get; }
        }
        #endregion

        #region Test Method (for debugging)
        /// <summary>
        /// This method is simply here to make debugging easier
        /// </summary>
        /// <param name="args">A default parameter</param>
        internal void TestStartupAndStop(string[] args)
        {
            this.OnStart(args);
            Console.ReadLine();
            this.OnStop();
        }
        #endregion

        #region Initializer
        /// <summary>
        /// This is the initializer where certain settings are also turned on to make this program more robust
        /// </summary>
        public DropboxExtensionService()
        {
            InitializeComponent();
            this.CanHandlePowerEvent = true;
            this.CanHandleSessionChangeEvent = true;
            this.CanPauseAndContinue = true;
            this.CanShutdown = true;
            this.CanStop = true;
        }
        #endregion

        #region On Start Method
        /// <summary>
        /// This is the start method that'll be in charge of setting up my file watchers based on the AppSettings.xml file
        /// </summary>
        /// <param name="args">A default parameter</param>
        protected override void OnStart(string[] args)
        {
            //Reading the AppSettings.xml file
            XDocument doc = null;
            try {
                doc = XDocument.Load("AppSettings.xml");
            }
            catch (Exception e)
            {
                if (File.Exists("errorlog.txt"))
                {
                    using (StreamWriter sw = new StreamWriter("errorlog.txt")) {
                        sw.WriteLine(DateTime.Now + ":");
                        sw.WriteLine("Problem acessing the AppSettings.xml file with this message below. Make sure it is in the same directory as the executable.");
                        sw.WriteLine(e.Message);
                    }

                    System.Environment.Exit(0);
                }
                else
                {
                    File.Create("errorlog.txt");
                    using (StreamWriter sw = new StreamWriter("errorlog.txt"))
                    {
                        sw.WriteLine(DateTime.Now + ":");
                        sw.WriteLine("Problem acessing the AppSettings.xml file with this message below. Make sure it is in the same directory as the executable.");
                        sw.WriteLine(e.Message);
                    }

                    System.Environment.Exit(0);
                }
            }
            
            var groups = doc.Root.Elements("group").Select(x => new group
                  {
                      Path = (string)x.Attribute("path"),
                      Filename = (string)x.Attribute("filename"),
                      NewFolder = (string)x.Attribute("newfolder")
                  }).ToList();

        }
        #endregion

        #region On Stop Method
        /// <summary>
        /// This method is the stop method that'll be in charge of any clean up in the case that the service is stopped
        /// </summary>
        protected override void OnStop()
        {

        }
        #endregion
    }
}
