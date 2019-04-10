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
    /// Singleton class that Reads a Elite dangerous log files and prints thargoid kills
    /// </summary>
    /// <exception cref="ArgumentException">User didn't selected directory.</exception>
    ///<exception cref="DirectoryNotFoundException">Reader didnt found logs dir.</exception>
    public class JSONReaderSingleton
    {
        //Error Logger
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public KillCounter counter = new KillCounter();
        private readonly string JournalsDirPath;

        //Storage variable for singleton
        private static JSONReaderSingleton JSONReaderInstance;

        /// <summary>
        /// Constructor of JSORReaderSingleton Class
        /// </summary>
        /// <param name="JournalsDirPath">Path where journals are located</param>
        private JSONReaderSingleton(string JournalsDirPath)
        {
            if(CheckIfLogDirExists(JournalsDirPath) == true)
            {
                this.JournalsDirPath = JournalsDirPath;
            }
            else
            {
                MessageBox.Show("Error: Cant find directory. Please select directory where ED journals are located.");
                log.Debug($"Directory not found.\r\nPath: {JournalsDirPath}");

                int numberOfRetries = 0;
                do
                {
                    JournalsDirPath = SelectDirectory();
                    if (numberOfRetries >= 3)
                    {
                        MessageBox.Show("Error: U selected directory with no log files too many times. Program now terminates.");
                        throw new ArgumentException("User selected wrong directory too many times");
                    }
                    if (CheckIfLogDirExists(JournalsDirPath) == false)
                    {
                        MessageBox.Show("Error: Cant find directory. Please select directory where ED journals are located.");
                        log.Debug($"Directory not found.\r\nPath: {JournalsDirPath}");
                        numberOfRetries++;
                    }
                    else
                    {
                        if(CheckIfDirContainsLogs(JournalsDirPath) == true)
                        {
                            this.JournalsDirPath = JournalsDirPath;
                            break;
                        }
                        else
                        {
                            MessageBox.Show($"Error: No logs detected in directory. Select valid directory.\r\nPath: {JournalsDirPath}");
                            log.Debug($"Directory doesnt contain logs.\r\nPath: {JournalsDirPath}");
                            numberOfRetries++;
                        }
                        
                    }
                } while (true);
            }
            
        }

        /// <summary>
        /// JSONReaderSingleton method that creates only one object of JSONReaderSingleton
        /// </summary>
        /// <returns>Single class of JSONReaders</returns>
        public static JSONReaderSingleton GetInstance(string JournalsDirPath)
        {
            if (JSONReaderInstance == null)
            {
                JSONReaderInstance = new JSONReaderSingleton(JournalsDirPath);
            }
            else
            {
                MessageBox.Show("Warning: Cant create more then one object of JSONReaderSingleton");
                log.Warn("Warning: Tried to create more then one object of JSONReaderSingleton");
            }

            return JSONReaderInstance;
        }

        /// <summary>
        /// Checks if directory exists
        /// </summary>
        /// <param name="JournalsDirPath">Path to selected directory</param>
        /// <returns>true - dir existe, false - dir doesnt exist</returns>
        private Boolean CheckIfLogDirExists(string JournalsDirPath)
        {
            if (Directory.Exists(JournalsDirPath) == true)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Check if selected directory contains logs
        /// </summary>
        /// <param name="JournalsDirPath">Path to selected directory</param>
        /// <returns>true - contains logs, false - doesnt contain logs</returns>
        private Boolean CheckIfDirContainsLogs(string JournalsDirPath)
        {
            DirectoryInfo directory = new DirectoryInfo(JournalsDirPath);//directoryPath + @"\Saved Games\Frontier Developments\Elite Dangerous"
            FileInfo[] unsortedJournals = directory.GetFiles("*.log");
            if (unsortedJournals.Length == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Detects thargoid kill from EDEvent class
        /// </summary>
        /// <param name="e1"> converted JSON text to class </param>
        /// <param name="JSONStringLine"> JSON text </param>
        private void DetectThargoidKill(EDEvent e1, string JSONStringLine)
        {
            ThargoidKillEvent kill;
            if (e1.@event.Equals("FactionKillBond"))
            {
                kill = JsonConvert.DeserializeObject<ThargoidKillEvent>(JSONStringLine,new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
                if (kill.awardingFaction_Localised == null || kill.victimFaction_Localised == null)
                {
                    log.Debug($"Faulty line passed to DetectThargoidKill. AwardingFaction and VictimFaction are null\r\n Text of line - {JSONStringLine}");
                }
                else if (kill.awardingFaction_Localised.Equals("Pilots Federation") || kill.victimFaction_Localised.Equals("Thargoids"))
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
                            log.Info($"NEW THARGOID TYPE - Found unknown new type of thargoid. Credits for kill: {kill.reward}");
                            break;
                    }
                }
            }
        }
        
        /// <summary>
        /// Method which reads JSON file 
        /// </summary>
        /// <param name="filePath"> - a path to a file</param>
        private void ReadJsonFile(string filePath)
        {
            //debug string for current JSONStringLine
            string JSONStringLine = "";
            FileStream fileStream = null;
            StreamReader reader = null;
            try
            {
                fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                reader = new StreamReader(fileStream);
                EDEvent currentEvent = null;

                while (!reader.EndOfStream)
                {
                    try
                    {
                        JSONStringLine = reader.ReadLine();
                        currentEvent = JsonConvert.DeserializeObject<EDEvent>(JSONStringLine, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
                        DetectThargoidKill(currentEvent, JSONStringLine);
                    }
                    catch (JsonReaderException ex)
                    {
                        log.Debug("JSONReader exception occured", ex);
                        continue;
                    }
                }
            }
            catch (Exception)
            {
                log.Error($"Unknown error while reading file {filePath}");
                throw;
            }
            finally
            {
                //closes stream and reader in case of some unknown error
                if(fileStream != null)
                {
                    fileStream.Close();
                }
                if(reader != null)
                {
                    reader.Close();
                }
            }
        }

        /// <summary>
        /// Reads last Log while game is running(reads or tries to find a new file to read until user closes application) REQUIRES another thread
        /// </summary>
        /// 
        public void ReadLastJsonWhilePlaying()
        {
            //integer for current line of reading
            int line = 1;
            //debug string for current JSONStringLine
            string JSONStringLine = "";
            FileStream fileStream = null;
            StreamReader reader = null;
            try
            {
                //gets journals in directory
                List<FileInfo> sortedJournalsList = GetJournalsInDirectory();
                //last log which will be read in realtime
                FileInfo lastLog = sortedJournalsList[sortedJournalsList.Count - 1];
                //string for file path
                string path = lastLog.FullName;
                EDEvent currentEvent = null;
                fileStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                reader = new StreamReader(fileStream);
                do
                {
                    //if reader is not at the end of log file readline
                    if (!reader.EndOfStream)
                    {
                        try
                        {
                            JSONStringLine = reader.ReadLine();
                            currentEvent = JsonConvert.DeserializeObject<EDEvent>(JSONStringLine, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
                            DetectThargoidKill(currentEvent, JSONStringLine);
                            line++;
                        }
                        
                        catch (JsonReaderException ex)
                        {
                            log.Debug(ex.Message, ex);
                            line++;
                        }
                    }
                    //reader is at the end of the line checks if file changed
                    else if (reader.EndOfStream)
                    {
                        bool fileChanged = false;
                        //last time file was changed
                        DateTime currentLastTimeWritten = DateTime.Now;
                        do
                        {
                            lastLog = new FileInfo(path);
                            //gets journals in directory
                            sortedJournalsList = GetJournalsInDirectory();
                            //gets current last log
                            FileInfo logCheck = sortedJournalsList[sortedJournalsList.Count - 1];
                                
                            //true if file changed
                            if (lastLog.LastWriteTime > currentLastTimeWritten)
                            {
                                currentLastTimeWritten = lastLog.LastWriteTime;
                                fileChanged = true;
                            }
                            //True if at the end of file and directory has a newer log file
                            else if (reader.EndOfStream && !logCheck.Name.Equals(lastLog.Name))
                            {
                                line = 1;
                                lastLog = logCheck;
                                path = lastLog.FullName;
                                fileStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                                reader = new StreamReader(fileStream);
                                fileChanged = true;
                            }
                            //else sleeps thread and restarts fileChanged cycle 5 sec later
                            else
                            {
                                lastLog = null;
                                Thread.Sleep(5000);
                            }
                        } while (fileChanged == false);
                        //restarts the reading cycle
                        continue;
                    }
                } while (true);
                
                
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                //closes stream and reader in case of some unknown error
                if (fileStream != null)
                {
                    fileStream.Close();
                }
                if (reader != null)
                {
                    reader.Close();
                }
            }
        }

        /// <summary>
        /// Reads all Journal files in selected directory
        /// </summary>
        public void ReadDirectory()
        {
            const int NUMBER_OF_RETRIES = 1;
            const int DELAY = 10;
            counter.ResetThargoidKills();
            List<FileInfo> journalsList = JSONReaderInstance.GetJournalsInDirectory();
            string path = "";
            //reads all Journal files except last one (the one which Elite Dangerous writes in real-time)
            for (int i = 0; i < journalsList.Count - 1; i++)
            {
                path = journalsList[i].FullName;
                //Console.WriteLine("Reading: " + path);
                for (int j = 0; j <= NUMBER_OF_RETRIES; ++j)
                {
                    try
                    {
                        JSONReaderInstance.ReadJsonFile(path);
                        break;
                    }
                    catch (IOException) when (j < NUMBER_OF_RETRIES)
                    {
                        System.Threading.Thread.Sleep(DELAY);
                    }
                    catch (IOException ex)
                    {
                        log.Info($"Cant access file: {path}", ex);
                    }
                }
            }
            //Initiates garbage collection at the end
            GC.Collect();
        }

        /// <summary>
        /// Method which find all Elite dangerous Journals in selected directory
        /// </summary>
        /// <returns>List of Journals </returns>
        ///<exception cref="DirectoryNotFoundException">Logs Direcory not found.</exception>
        private List<FileInfo> GetJournalsInDirectory()
        {
            //regex that matches ED journals names
            Regex regex = new Regex(@"(Journal)\.(\d{12})\.(\d{2})\.log"); //matches with Journal.123456789109.01.log
            DirectoryInfo directory = new DirectoryInfo(JournalsDirPath);//directoryPath + @"\Saved Games\Frontier Developments\Elite Dangerou"
            FileInfo[] unsortedJournals = null;
            try
            {
                //field of unsorted files from directory above
                unsortedJournals = directory.GetFiles("*.log");
            }
            catch (DirectoryNotFoundException ex)
            {
                log.Error(ex.Message, ex);
                throw ex;
            }
            //List for sorted ED Journal files from directory
            //List<FileInfo>
            List<FileInfo> sortedJournalsList = new List<FileInfo>();

            //matches filenames with regex and sorts ED Journal files from others
            for (int i = 0; i < unsortedJournals.Length; i++)
            {
                if (regex.IsMatch(unsortedJournals[i].Name))
                {
                    sortedJournalsList.Add(unsortedJournals[i]);
                }
            }
            //Orders list by Last time it was written into (descending)
            sortedJournalsList = sortedJournalsList.OrderBy(x => x.LastWriteTime).ToList();
            return sortedJournalsList;
        }

        /// <summary>
        /// Shows folder browser to user which selects ED journals directory
        /// </summary>
        /// <returns>List of Journals</returns>
        /// <exception cref="ArgumentException">User didn't selected directory.</exception>
        private string SelectDirectory()
        {
            
            FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog();
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                return folderBrowserDialog1.SelectedPath;
            }
            else
            {
                throw new ArgumentException("User didnt selected directory.");
            }
        }
    }
}
