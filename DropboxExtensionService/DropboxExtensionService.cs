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
using System.Text.RegularExpressions;

namespace DropboxExtensionService
{
    public partial class DropboxExtensionService : ServiceBase
    {
        #region Custom Structures and Constant Variables
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

        //The location of the file that has the information on the dropbox folder 
        const string dropboxInfoPath = @"Dropbox\info.json";
        string dropboxPath = "";
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
            #region Variables
            //Variables
            XDocument doc = null;
            #endregion

            #region Dynamic File Locations
            //This part is to get the appdata path dynamically for the computer its on
            //This will hopefully eventually be replaced by the custom installer
            string profilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).ToString();
            #endregion

            #region Getting Dropbox Location
            //Finding the location of the dropbox folder (could find it in the Dropbox\info.json file)
            //Two ways to do this I could just hope that the person used the default install location but this seems unnecessarily risky
            //dropboxPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).ToString() + "\\Dropbox";

            //The second option is using the info.json file that should always be in the AppData folder
            //Going to use this less risky way and get the info from this info.json file
            var jsonPath = Path.Combine(Environment.GetEnvironmentVariable("LocalAppData"), dropboxInfoPath);
            if (!File.Exists(jsonPath)) jsonPath = Path.Combine(Environment.GetEnvironmentVariable("AppData"), dropboxInfoPath);
            if (!File.Exists(jsonPath))
            {
                if (File.Exists("errorlog.txt"))
                {
                    using (StreamWriter sw = new StreamWriter("errorlog.txt"))
                    {
                        sw.WriteLine(DateTime.Now + ":");
                        sw.WriteLine("Could not locate dropbox's info.json file.");
                    }

                    System.Environment.Exit(0);
                }
                else
                {
                    File.Create("errorlog.txt");
                    using (StreamWriter sw = new StreamWriter("errorlog.txt"))
                    {
                        sw.WriteLine(DateTime.Now + ":");
                        sw.WriteLine("Could not locate dropbox's info.json file.");
                    }

                    System.Environment.Exit(0);
                }
            }
            //Can improve this later
            dropboxPath = File.ReadAllText(jsonPath).Split('\"')[5].Replace(@"\\", @"\");
            #endregion

            #region Reading AppSettings.xml
            //Reading the AppSettings.xml file
            try
            {
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
                      Path = (string)(profilePath + "\\" + x.Attribute("path")),
                      Filename = (string)x.Attribute("filename"),
                      NewFolder = (string)(dropboxPath + "\\" + x.Attribute("newfolder"))
                  }).ToList();
            #endregion

            #region Create Dropbox Folders & See If The Current Data Is Up To Date
            //Creating the new folders in dropbox for the files if they don't exist
            //Will check the files to see if the most up to date version is already in the dropbox folders (will do this be looking at the modified metadata date)
            foreach (var entry in groups)
            {
                if (!(Directory.Exists(entry.NewFolder)))
                {
                    //Create directory and move the data into it
                    Directory.CreateDirectory(entry.NewFolder);
                }
                else
                {
                    //Check the data to see if it is up to date

                }
            }
            #endregion

            #region Setup File Watchers
            //Setting up different threads to watch each file

            #endregion

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
