using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;

// FSW mostly lifted from: http://www.switchonthecode.com/tutorials/csharp-snippet-tutorial-using-the-filesystemwatcher-class
// http://msdn.microsoft.com/en-us/library/system.io.filesystemwatcher.changed.aspx

// nsca on windows, with or without nsclient++: http://www.nsclient.org/nscp/discussion/message/1939
// ncsa win32 binary: http://exchange.nagios.org/directory/Addons/Passive-Checks/NSCA-Win32-Client/details


namespace ConsoleApplication1
{
    public class Program
    {
        // yea yea yea, well, it's too much.  Make it portable yourself.
        public static int CountOfFilesAffected, HowManyFileOperationsToTrack;
        public static List<string> ExclusionStringList;
        public static List<string> InclusionStringList;

        //I use Main() to handle the arguments and set the settings that the FileSystemWatcher will use
        public static void Main(string[] args)
        {
            //these suckers will be passed
            int FSWBufferSize = 0;
            string TargetDirectoryForTrackingOperations = "";
            bool IncludeSubDirectories = false, ExcludeDFSRPrivateFiles = false, ExcludeSMBTestFiles = false;
            List<string> FSWFilterList = new List<string>();
            List<string> FSWEventsList = new List<string>();
            List<string> ExclusionStringList = new List<string>();
            List<string> InclusionStringList = new List<string>();

            //this sucker stays put
            List<string> verbatimArgs = new List<string>();


            if (args.Length == 0)
            {
                ExitProg("UNKNOWN no arguments");
            }

            //read the suckers into an array list
            int failcount = 0;
            foreach (string arg in args)
            {
                verbatimArgs.Add(arg);
                Console.WriteLine(arg);

                //check for required arguments
                if (arg == "-N" || arg == "-D" || arg == "-WL")
                {
                    failcount = failcount + 1;
                }
            }

            //don't feel like being conditional
            if (failcount < 3)
            {
                ExitProg("UNKNOWN You're missing some required arguments.");

            }


            //set the variables for the settings

            //sets -N number of changes to track
            //try to convert the given -N argument to a number, if it fails, then it's not a number, and exitprog
            try
            {
                HowManyFileOperationsToTrack = Convert.ToInt32(verbatimArgs[verbatimArgs.IndexOf("-N") + 1]);
            }
            catch (InvalidCastException e)
            {
                ExitProg("UNKNOWN " + e);
            }

            //sets -D to the target directory for the operation
            //get the folder of the TargetDirectoryForTrackingOperations, if it fails, then it isn't a folder, and exitprog
            try
            {
                TargetDirectoryForTrackingOperations = verbatimArgs[verbatimArgs.IndexOf("-D") + 1];
            }
            catch (InvalidCastException e)
            {
                ExitProg("UNKNOWN " + e);
            }

            //sets -WL to the watchlist of events to allow the FileSystemWatcher to raise
            //gets the list and .split() them by @":", if the strings don't match any of the given list (CH, CR, DE, RE) then exitprog
            if (verbatimArgs.Contains(@"-WL") == true)
            {
                foreach (string FSWEventToWatch in verbatimArgs[verbatimArgs.IndexOf("-WL") + 1].Split(':'))
                {
                    if (FSWEventToWatch == @"CH" || FSWEventToWatch == @"CR" || FSWEventToWatch == @"DE" || FSWEventToWatch == @"RE")
                    {
                        FSWEventsList.Add(FSWEventToWatch);
                    }
                    else
                    {
                        //This argument doesn't set defaults, so we'll fail at this.
                        ExitProg("UNKNOWN Provide proper -WL parameters");

                    }
                }

            }

            //sets -FL to the filterlist of events to allow the FileSystemWatcher's events to raise
            //gets the list and .split() them by @":", if the strings don't match any of the given list (FN:DN:AT:SZ:LW:LA:CT:SEC)
            if (verbatimArgs.Contains(@"-FL") == true)
            {
                foreach (string FSWFiltersToWatch in verbatimArgs[verbatimArgs.IndexOf("-FL") + 1].Split(':'))
                {
                    if (FSWFiltersToWatch == @"FN" || FSWFiltersToWatch == @"DN" || FSWFiltersToWatch == @"AT" || FSWFiltersToWatch == @"SZ" || FSWFiltersToWatch == @"LW" || FSWFiltersToWatch == @"LA" || FSWFiltersToWatch == @"CT" || FSWFiltersToWatch == @"SEC")
                    {
                        FSWFilterList.Add(FSWFiltersToWatch);
                    }
                    else
                    {
                        //This argument doesn't set defaults, so we'll fail at this.
                        ExitProg("UNKNOWN Provide proper -FL parameters");

                    }
                }

            }



            //set the rest of the non required, explicit arguments

            if (verbatimArgs.Contains(@"-S") == true)
            {
                IncludeSubDirectories = true;
            }

            if (verbatimArgs.Contains(@"-EX") == true)
            {

                if (verbatimArgs[verbatimArgs.IndexOf("-EX") + 1].Contains("D"))
                {
                    ExcludeDFSRPrivateFiles = true;
                }
                else if (verbatimArgs[verbatimArgs.IndexOf("-EX") + 1].Contains("S"))
                {
                    ExcludeSMBTestFiles = true;
                }
                else
                {
                    //then neither of these strings were provided.
                    //no error really necessary.  I'm loose like that.
                }
            }

            if (verbatimArgs.Contains(@"-EXSTR") == true)
            {
                foreach (string FileExclusionString in verbatimArgs[verbatimArgs.IndexOf("-EX") + 1].Split(':'))
                {
                    ExclusionStringList.Add(FileExclusionString);
                }

                //during FSW event raised: if *file.path*.contains(any string entry in fileexclusionlist) then disregard.,

            }

            if (verbatimArgs.Contains(@"-INCSTR") == true)
            {
                foreach (string FileExclusionString in verbatimArgs[verbatimArgs.IndexOf("-EX") + 1].Split(':'))
                {
                    InclusionStringList.Add(FileExclusionString);
                }

                //during FSW event raised: if *file.path*.contains(any string entry in InclusionStringList) then disregard.

            }

            if (verbatimArgs.Contains(@"-BUFF") == true)
            {
                FSWBufferSize = verbatimArgs.IndexOf("-BUFF") + 1;

            }


            //Thread.Sleep(10000);

            // phew! made it through with no errors.  Time to run our program.
            //create and configure the FSWatcher... passing all the configurations
            CreateFSWatcher(FSWBufferSize, TargetDirectoryForTrackingOperations, FSWEventsList, FSWFilterList, IncludeSubDirectories, ExcludeDFSRPrivateFiles, ExcludeSMBTestFiles);


        }



        public static void CreateFSWatcher(
            int FSWBufferSize,
            string TargetDirectoryForTrackingOperations,
            List<string> FSWWatchList,
            List<string> FSWFilterList,
            bool IncludeSubDirectories,
            bool ExcludeDFSRPrivateFiles,
            bool ExcludeSMBTestFiles
            )
        {

            //instantiate and initialize a System.IO.FileSystemWatcher()
            FileSystemWatcher fswatcher = new FileSystemWatcher();


            //configure the FileSystemWatcher fswatcher

            //fswatcher.Filter filter can not take multiple strings! it's only a single wildcard file filter, which is restrictive!
            // this could affect performance due to buffer overflows.
            // I will only use this if a single string is contained in InclusionStringList
            if (InclusionStringList.Count == 1)
            {
                fswatcher.Filter = "*" + InclusionStringList[0] + "*";
            }


            //includesubdirectories
            fswatcher.IncludeSubdirectories = IncludeSubDirectories;

            //internalbuffersize
            if (FSWBufferSize <= 65536 & FSWBufferSize >= 4096)
            {
                fswatcher.InternalBufferSize = FSWBufferSize;
            }
            else
            {
                fswatcher.InternalBufferSize = 8192;
            }


            //notifyfilter
            NotifyFilters NotifyFilterValue = 0;

            foreach (string filter in FSWFilterList)
            {
                //FN:DN:AT:SZ:LW:LA:CT:SEC
                // thank you MSFT! http://msdn.microsoft.com/en-us/library/system.io.notifyfilters(VS.71).aspx
                switch (filter)
                {
                    case "FN":
                        NotifyFilterValue = NotifyFilterValue + 1;
                        break;
                    case "DN":
                        NotifyFilterValue = NotifyFilterValue + 2;
                        break;
                    case "AT":
                        NotifyFilterValue = NotifyFilterValue + 4;
                        break;
                    case "SZ":
                        NotifyFilterValue = NotifyFilterValue + 8;
                        break;
                    case "LW":
                        NotifyFilterValue = NotifyFilterValue + 16;
                        break;
                    case "LA":
                        NotifyFilterValue = NotifyFilterValue + 32;
                        break;
                    case "CT":
                        NotifyFilterValue = NotifyFilterValue + 64;
                        break;
                    case "SEC":
                        NotifyFilterValue = NotifyFilterValue + 256;
                        break;
                    default:
                        // won't ever be hit, so may as well shoot the moon
                        //NotifyFilterValue = 256;
                        break;
                }
            }
            fswatcher.NotifyFilter = NotifyFilterValue;

            //path
            fswatcher.Path = TargetDirectoryForTrackingOperations;


            fswatcher.Renamed += new RenamedEventHandler(fswatcher_Renamed);
            fswatcher.Deleted += new FileSystemEventHandler(fswatcher_Deleted);
            fswatcher.Changed += new FileSystemEventHandler(fswatcher_Changed);
            fswatcher.Created += new FileSystemEventHandler(fswatcher_Created);
            fswatcher.EnableRaisingEvents = true;

            while (Console.Read() != 'q') ;


        }


        public static void fswatcher_Renamed(object sender, RenamedEventArgs e)
        {

            CountOfFilesAffected = CountOfFilesAffected + 1;

            if (HowManyFileOperationsToTrack <= CountOfFilesAffected)
            {
                Send_NSCA();
            }

        }

        public static void fswatcher_Deleted(object sender, FileSystemEventArgs e)
        {
            CountOfFilesAffected = CountOfFilesAffected + 1;

            if (HowManyFileOperationsToTrack <= CountOfFilesAffected)
            {
                Send_NSCA();
            }

        }

        public static void fswatcher_Changed(object sender, FileSystemEventArgs e)
        {
            CountOfFilesAffected = CountOfFilesAffected + 1;

            if (HowManyFileOperationsToTrack <= CountOfFilesAffected)
            {
                Send_NSCA();
            }

        }

        public static void fswatcher_Created(object sender, FileSystemEventArgs e)
        {
            CountOfFilesAffected = CountOfFilesAffected + 1;

            if (HowManyFileOperationsToTrack <= CountOfFilesAffected)
            {
                Send_NSCA();
            }

        }

        public static void Send_NSCA()
        {
            //CountOfFilesAffected

            //http://msdn.microsoft.com/en-us/library/system.diagnostics.process.standardoutput.aspx

            // This is the code for the base process
            Process send_nsca_proc = new Process();
            
            ProcessStartInfo procstartinfo = new ProcessStartInfo();
            procstartinfo.CreateNoWindow = true;
            procstartinfo.UseShellExecute = false;
            procstartinfo.WindowStyle = ProcessWindowStyle.Hidden;

            procstartinfo.FileName = "send_nsca.exe";
            procstartinfo.Arguments = "";
            
            send_nsca_proc.StartInfo = procstartinfo;
            send_nsca_proc.Start();
            send_nsca_proc.WaitForExit();
            send_nsca_proc.Close();



        }

        public static void ExitProg(string nagstatus)
        {
            Console.WriteLine(nagstatus);
            Thread.Sleep(10000);

            if (nagstatus.ToLower().Contains("unknown"))
            {
                Environment.Exit(3);
            }

            if (nagstatus.ToLower().Contains("critical"))
            {
                Environment.Exit(2);
            }

            if (nagstatus.ToLower().Contains("warning"))
            {
                Environment.Exit(1);
            }

            if (nagstatus.ToLower().Contains("ok"))
            {
                Environment.Exit(0);
            }

        }

    }
}


