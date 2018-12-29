﻿using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace TKC
{
    

    /// <summary>
    /// Class for saving Timestamp and name of events from JSON file
    /// </summary>
    internal class EDEvent
    {

        public DateTime timestamp { get; set; }
        public string @event { get; set; }

        public override string ToString()
        {
            return string.Format(" Timestamp {0} Event: {1} ", timestamp, @event);
        }


    }

    /// <summary>
    /// Inherited Class from EDEvent which saves data about thargoid kills
    /// </summary>
    internal class ThargoidKillEvent : EDEvent
    {
        /// <summary>
        /// Reward -- in Credits, tels us which thargoid we killed 10 k Scout, 2 mil Cyclops, 6 mil Basillisk, 10 mil Medusa, 15 mil Hydra
        /// </summary>
        public int reward { get; set; }
        public string awardingFaction_Localised { get; set; }
        public string victimFaction_Localised { get; set; }

        public override string ToString()
        {
            return string.Format(" Timestamp: {0} Event: {1} reward: {2} awardingFaction: {3} victimFaction: {4}",
                timestamp, @event, reward, awardingFaction_Localised, victimFaction_Localised);
        }
    }



    /// <summary>
    /// Singleton class that Reads a Elite dangerous log files and prints thargoid kills
    /// </summary>
    public class JSONReaderSingleton
    {
        public KillCounter counter = new KillCounter();
        
        //Storage variable for singleton
        private static JSONReaderSingleton JSONReaderInstance;
        /// <summary>
        /// JSONReaderSingleton constructor
        /// </summary>
        /// <returns>Single class of JSONReaders</returns>
        public static JSONReaderSingleton getInstance()
        {

            if (JSONReaderInstance == null)
            {
                JSONReaderInstance = new JSONReaderSingleton();
            }

            return JSONReaderInstance;
        }
        /// <summary>
        /// Detects thargoid kill from EDEvent class
        /// </summary>
        /// <param name="e1"> converted JSON text to class </param>
        /// <param name="JSONStringLine"> JSON text </param>
        private void DetectThargoidKill(EDEvent e1, string JSONStringLine)
        {
            try
            {

                ThargoidKillEvent kill;
                if (e1.@event.Equals("FactionKillBond"))
                {

                    kill = JsonConvert.DeserializeObject<ThargoidKillEvent>(JSONStringLine,
                        new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });

                    if (kill.awardingFaction_Localised != null && kill.victimFaction_Localised != null &&
                        kill.awardingFaction_Localised.Equals("Pilots Federation") && kill.victimFaction_Localised.Equals("Thargoids"))

                    {
                        int caseSwitch = kill.reward;
                        switch (caseSwitch)
                        {
                            case 10000:
                                counter.scout++;
                                counter.allTypesKills++;
                                break;
                            case 2000000:
                                counter.cyclops++;
                                counter.allTypesKills++;
                                break;
                            case 6000000:
                                counter.basillisk++;
                                counter.allTypesKills++;
                                break;
                            case 10000000:
                                counter.medusa++;
                                counter.allTypesKills++;
                                break;
                            case 15000000:
                                counter.hydra++;
                                counter.allTypesKills++;
                                break;
                            default:
                                counter.unknown++;
                                counter.allTypesKills++;
                                break;
                        }
                    }
                }
            }catch(Exception e)
            {
                MessageBox.Show("Error: Unknown in method DetectThargoidKill");
                throw;
            }
            
        }

        
        /// <summary>
        /// Method which reads JSON file 
        /// </summary>
        /// <param name="filePath"> - a path to a file</param>
        private void ReadJsonFile(string filePath)
        {
            //debug integer for current line of reading
            int line = 1;
            //debug string for current JSONStringLine
            string JSONStringLine = "";
            StreamReader fileReader = null;
            try
            {
                string path = filePath; //Path of a file
                EDEvent currentEvent;
                fileReader = new System.IO.StreamReader(path); //Streamreader which reads file line by line
                while ((JSONStringLine = fileReader.ReadLine()) != null)
                {
                    //JSON convertor which converts JSON to class variables
                    currentEvent = JsonConvert.DeserializeObject<EDEvent>(JSONStringLine, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
                    line++;
                    DetectThargoidKill(currentEvent, JSONStringLine);
                }
            }
            catch (JsonReaderException e)
            {

            }
            catch (FileNotFoundException e)
            {

                MessageBox.Show("Error: File " + filePath + " not found");
            }
            catch (IOException e)
            {
                
                throw;
            }
            catch (Exception e)
            {
                MessageBox.Show("Error: Unknown error in ReadJsonFile");
                throw;
            }
            finally
            {
                fileReader.Close();
            }
        }

        /// <summary>
        /// Reads last Log in real time while game is running(reads or tries to find a new file to read until user closes application)
        /// </summary>
        public void ReadLastJsonFileInRealTime()
        {
            //integer for current line of reading
            int line = 1;
            //integer which indicates last line that have been read (reader wont read same lines every cycle)
            int endLine = 0;
            //debug string for current JSONStringLine
            string JSONStringLine = "";
            StreamReader fileReader = null;
            try
            {
                //last log which will be read in realtime
                FileInfo lastLog = sortedJournalsList[sortedJournalsList.Count - 1];
                //string for file path
                string path = lastLog.FullName;
                EDEvent currentEvent = null;
                fileReader = new StreamReader(path);

                while (true)
                {
                    if ((JSONStringLine = fileReader.ReadLine()) != null)
                    {
                        try
                        {
                            currentEvent = JsonConvert.DeserializeObject<EDEvent>(JSONStringLine, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
                        }
                        catch (JsonReaderException e)
                        {
                        }
                        
                        try
                        {
                            //true if reader read line which he already read
                            if (line < endLine)
                            {
                                line++;
                            }//true if reader read line which he didnt read before
                            else if (line >= endLine)
                            {
                                line++;
                                DetectThargoidKill(currentEvent, JSONStringLine);
                            }//true if reader finds shutdown event at the end of file
                            if (currentEvent.@event.Equals("Shutdown") == true)
                            {
                                fileReader.Close();
                                //bool which controls if new file last file was found if true restarts reading cycle
                                bool newLastFileFound = false;
                                while (newLastFileFound == false)
                                {

                                    addAction("Thread searching for new file");
                                    GetJournalsInDirectory();
                                    FileInfo logCheck = sortedJournalsList[sortedJournalsList.Count - 1];
                                    if (!logCheck.Name.Equals(lastLog.Name))
                                    {
                                        addAction("Thread found new file");
                                        lastLog = logCheck;
                                        path = lastLog.FullName;
                                        fileReader = new StreamReader(path);
                                        newLastFileFound = true;
                                        break;
                                    }
                                    else
                                    {
                                        Thread.Sleep(5000);
                                    }
                                }
                                continue;
                            }
                        }
                        catch (NullReferenceException)
                        {
                            //catches NullReference exception so that rest of the program can continue
                            //do nothing
                        }

                        //true if reader reaches end of file
                        if (fileReader.EndOfStream == true)
                        {
                            //bool which controls if file have changed if true restarts the reading cycle above
                            bool fileChanged = false;
                            //value which saves last time file changed
                            DateTime currentLastTimeWritten = DateTime.Now;
                            endLine = line;
                            while (fileChanged == false)
                            {
                                lastLog = new FileInfo(path);
                                //true if file changed
                                if (lastLog.LastWriteTime > currentLastTimeWritten)
                                {
                                    addAction("File Changed");
                                    currentLastTimeWritten = lastLog.LastWriteTime;
                                    fileReader = new StreamReader(path);
                                    fileChanged = true;
                                    line = 1;
                                }
                                //else sleeps thread and restarts fileChanged cycle 5 sec later
                                else
                                {
                                    addAction("Waiting for file change");
                                    lastLog = null;
                                    fileReader.Close();
                                    Thread.Sleep(5000);
                                }
                            }
                            //restarts the reading cycle
                            continue;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Error:  " + e.Message);
                throw;
                
            }finally
            {
                fileReader.Close();
            }
        }

        /// <summary>
        /// Reads all Journal files in selected directory
        /// </summary>
        public void ReadDirectory()
        {
            const int numberOfRetries = 20;
            const int delay = 3000;
            try
            {
                counter.ResetThargoidKills();
                List<FileInfo> journalsList = JSONReaderInstance.GetJournalsInDirectory();
                string path = "";
                //reads all Journal files except last one (the one which Elite Dangerous writes in real-time)
                for (int i = 0; i < journalsList.Count - 1; i++)
                {
                    path = journalsList[i].FullName;
                    //Console.WriteLine("Reading: " + path);
                    for (int j = 0; j <= numberOfRetries; ++j)
                    {
                        try
                        {
                            JSONReaderInstance.ReadJsonFile(path);
                            break;
                        }
                        catch (IOException) when (j <= numberOfRetries)
                        {
                            System.Threading.Thread.Sleep(delay);
                        }
                    }
                }
                
                //Initiates garbage collection at the end
                GC.Collect();
            }
            catch (DirectoryNotFoundException e)
            {
                MessageBox.Show("Error: Journals directory not found");
            }
            
        }

        string directoryPath;
        //List of sorted Journals
        List<FileInfo> sortedJournalsList = null;
        /// <summary>
        /// Method which find all Elite dangerous Journals in selected directory
        /// </summary>
        /// <returns>List of Journals </returns>
        private List<FileInfo> GetJournalsInDirectory()
        {
            try
            {
                //finds path to Users folder ("C:\Users\<user>)"
                directoryPath = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)).FullName;
                if (Environment.OSVersion.Version.Major >= 6)
                {
                    directoryPath = Directory.GetParent(directoryPath).ToString();
                }
                //regex that matches ED journals names
                Regex regex = new Regex(@"(Journal)\.(\d{12})\.(\d{2})\.log"); //matches with Journal.123456789109.01.log
                DirectoryInfo directory = new DirectoryInfo(directoryPath + @"\Saved Games\Frontier Developments\Elite Dangerous");
                //field of unsorted files from directory above
                FileInfo[] unsortedJournals = directory.GetFiles("*.log");
                //List for sorted ED Journal files from directory
                //List<FileInfo>
                sortedJournalsList = new List<FileInfo>();

                //matches filenames with regex and sorts ED Journal files from others
                int index = 0;
                while (index < unsortedJournals.Length)
                {
                    if (regex.IsMatch(unsortedJournals[index].Name))
                    {
                        sortedJournalsList.Add(unsortedJournals[index]);
                    }
                    index++;
                }
                //Orders list by Last time it was written into (descending)
                sortedJournalsList = sortedJournalsList.OrderBy(x => x.LastWriteTime).ToList();
                
                return sortedJournalsList;
            }
            catch (DirectoryNotFoundException e)
            {
                return SelectDirectory();

            }
            catch (Exception e)
            {
                MessageBox.Show("Error:  " + e.Message);
                throw;
            }

        }
       
        /// <summary>
        /// Overloaded method GetJournals which gets all Journals from input
        /// </summary>
        /// <param name="directoryPathInput"> - directory path </param>
        /// <returns>List of Journals</returns>
        private List<FileInfo> GetJournalsInDirectory(string directoryPathInput)
        {
            try
            {
                //regex that matches ED journals names
                Regex regex = new Regex(@"(Journal)\.(\d{12})\.(\d{2})\.log"); //matches with Journal.123456789109.01.log
                DirectoryInfo directory = new DirectoryInfo(directoryPathInput);
                //field of unsorted files from directory above
                FileInfo[] unsortedFiles = directory.GetFiles("*.log");
                //List for sorted ED log files from directory
                List<FileInfo> sortedFilesList = new List<FileInfo>();

                //matches filenames with regex and sorts ED log files from others
                int index = 0;
                while (index < unsortedFiles.Length)
                {
                    if (regex.IsMatch(unsortedFiles[index].Name))
                    {
                        sortedFilesList.Add(unsortedFiles[index]);
                        //Console.WriteLine(unsortedFiles[index].Name);
                    }
                    index++;
                }
                //Orders list by Last time it was written into (descending)
                sortedFilesList = sortedFilesList.OrderBy(x => x.LastWriteTime).ToList();
                return sortedFilesList;
            }
            catch (DirectoryNotFoundException e)
            {
                
                throw;

            }
            catch (Exception e)
            {
                MessageBox.Show("Error:  " + e.Message);
                throw;
            }

        }

        /// <summary>
        /// Shows folder browser to user which selects ED journals directory
        /// </summary>
        /// <returns>List of Journals</returns>
        private List<FileInfo> SelectDirectory()
        { 
            //If app cant find default ED log directory. Alerts user to select directory
            MessageBox.Show("Error: Cant find directory. Please select directory where ED journals are located.");
            FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog();
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                return GetJournalsInDirectory(folderBrowserDialog1.SelectedPath);
            }
            else
            {
                return GetJournalsInDirectory("");
            }
        }

        /// <summary>
        /// List which stores string that will be printed in InfoLabel
        /// </summary>
        List<string> currentAction = new List<string>();
        /// <summary>
        /// Controls if list changed or not
        /// </summary>
        public bool listChange;
        /// <summary>
        /// Adds action description in action list
        /// </summary>
        /// <param name="s">Action description in string</param>
        private void addAction(string s)
        {
            if(currentAction.Contains(s))
            {
                int index = currentAction.IndexOf(s);
                string tmp = currentAction[currentAction.Count - 1];
                currentAction[currentAction.Count - 1] = s;
                currentAction[index] = s;
            }
            else
            {
                currentAction.Add(s);
            }
            listChange = true;
        }

        public string getLastAction()
        {
            listChange = false;
            return currentAction[currentAction.Count - 1];
        }
    }
}
