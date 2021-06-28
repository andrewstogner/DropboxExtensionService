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
        #region Custom Structures, Constant, And Static Variables
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
            public bool Exists;
        }

        //Static variables for file watchers
        static List<group> groups;
        static List<FileSystemWatcher> fileWatchers = null; 

        //The location of the file that has the information on the dropbox folder 
        const string dropboxInfoPath = @"Dropbox\info.json";
        static string dropboxPath = "";
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
                if (!(File.Exists("errorlog.txt")))
                {
                    File.Create("errorlog.txt");
                }

                using (StreamWriter sw = new StreamWriter("errorlog.txt"))
                {
                    sw.WriteLine(DateTime.Now + ": ");
                    sw.WriteLine("Problem acessing the AppSettings.xml file with this message below. Make sure it is in the same directory as the executable.");
                    sw.WriteLine(e.Message);
                }

                System.Environment.Exit(0);
            }
            
            groups = doc.Root.Elements("group").Select(x => new group
                  {
                      Path = (string)(profilePath + "\\" + x.Attribute("path")),
                      Filename = (string)x.Attribute("filename"),
                      NewFolder = (string)(dropboxPath + "\\Games\\" + x.Attribute("newfolder")),
                      Exists = Directory.Exists((string)(profilePath + "\\" + x.Attribute("path"))) ? true : false
                  }).ToList();
            #endregion

            #region Create Dropbox Folders & See If The Current Data Is Up To Date
            //Creating the new folders in dropbox for the files if they don't exist
            //Will check the files to see if the most up to date version is already in the dropbox folders (will do this be looking at the modified metadata date)
            if (!(Directory.Exists(dropboxPath + "\\Games")))
            {
                Directory.CreateDirectory(dropboxPath + "\\Games");
            }

            foreach (var entry in groups)
            {
                if (entry.Exists)
                {
                    //Loop through files for the files we want
                    string[] files = Directory.GetFiles(entry.Path);
                    Regex reg = new Regex(entry.Filename);

                    foreach (string file in files)
                    {
                        Match match = reg.Match(file);
                        if (match.Success)
                        {
                            if (!(Directory.Exists(entry.NewFolder)))
                            {
                                //Create directory if it doesnt exist
                                Directory.CreateDirectory(entry.NewFolder);
                            }

                            //Move data into folder if its a new version
                            if (!(File.Exists(Path.Combine(entry.NewFolder, entry.Filename))))
                            {
                                File.Copy(Path.Combine(entry.Path, entry.Filename), Path.Combine(entry.NewFolder, entry.Filename), true);
                            }
                            else
                            {
                                if (File.GetLastWriteTime(Path.Combine(entry.NewFolder, entry.Filename)) < File.GetLastWriteTime(Path.Combine(entry.Path, entry.Filename)))
                                {
                                    File.Copy(Path.Combine(entry.Path, entry.Filename), Path.Combine(entry.NewFolder, entry.Filename), true);
                                }
                            }
                        }
                    }
                }
                else
                {
                    //This is the case that one of the path files doesn't exist
                    //Either way though we'll let the program continue so the paths that do work can get watchers
                    //And we'll just log that it doesn't exist
                    if (!(File.Exists("errorlog.txt")))
                    {
                        File.Create("errorlog.txt");
                    }

                    using (StreamWriter sw = new StreamWriter("errorlog.txt"))
                    {
                        sw.WriteLine(DateTime.Now + ": ");
                        sw.WriteLine("Problem with the provided path:" + entry.Path);
                    }

                    System.Environment.Exit(0);

                }
            }
            #endregion

            #region Setup File Watchers
            //Setting up the filewatchers
            int i = 0;
            foreach (var entry in groups)
            {
                if(entry.Exists)
                {
                    fileWatchers[i] = new FileSystemWatcher();
                    fileWatchers[i].Path = entry.Path;
                    fileWatchers[i].IncludeSubdirectories = false;
                    fileWatchers[i].Created += new FileSystemEventHandler(Watcher_Event);
                    fileWatchers[i].EnableRaisingEvents = true;

                    i++;
                }
            }

            //Check if any file watchers were made
            if (fileWatchers.Equals(null))
            {
                if (!(File.Exists("errorlog.txt")))
                {
                    File.Create("errorlog.txt");
                }

                using (StreamWriter sw = new StreamWriter("errorlog.txt"))
                {
                    sw.WriteLine(DateTime.Now + ": ");
                    sw.WriteLine("No file watchers were made.");
                }

                System.Environment.Exit(0);
            }
            #endregion

        }
        #endregion

        #region On Stop Method
        /// <summary>
        /// This method is the stop method that'll be in charge of any clean up in the case that the service is stopped
        /// </summary>
        protected override void OnStop()
        {
            foreach (var fileWatcher in fileWatchers)
            {
                fileWatcher.EnableRaisingEvents = false;
                fileWatcher.Dispose();
            }
        }
        #endregion

        #region Watcher Event
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="arg"></param>
        private static void Watcher_Event(object sender, FileSystemEventArgs arg)
        {
            //Variables
            string sourcePath = "";
            string destPath = "";
            string file = "";

            //first need to know what file watcher executed
            foreach (var entry in groups)
            {
                if (arg.FullPath.Equals(entry.Path + "\\" + arg.Name))
                {
                    sourcePath = entry.Path;
                    destPath = entry.NewFolder;
                    file = entry.Filename;
                    break;
                }
            }

            //Check if a match was found
            if (sourcePath.Equals(""))
            {
                if (!(File.Exists("errorlog.txt")))
                {
                    File.Create("errorlog.txt");
                }

                using (StreamWriter sw = new StreamWriter("errorlog.txt"))
                {
                    sw.WriteLine(DateTime.Now + ": ");
                    sw.WriteLine("Unable to find a match for path - " + arg.FullPath);
                }
            }
            else
            {
                //Check if it is a valid file
                Regex reg = new Regex(file);
                Match match = reg.Match(arg.Name);

                if (match.Success)
                {
                    //Check that the new directory exist
                    if (!Directory.Exists(dropboxPath + "\\Games"))
                    {
                        Directory.CreateDirectory(dropboxPath + "\\Games");
                    }

                    if (!Directory.Exists(destPath))
                    {
                        Directory.CreateDirectory(destPath);
                    }

                    //Copy new file over
                    File.Copy(arg.FullPath, Path.Combine(destPath, arg.Name), true);
                }
            }

        }
        #endregion
    }
}
