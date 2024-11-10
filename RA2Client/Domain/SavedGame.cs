using System;
using System.IO;
using ClientCore;
using OpenMcdf;
using Rampastring.Tools;

namespace Ra2Client.Domain
{
    /// <summary>
    /// A single-player saved game.
    /// </summary>
    public class SavedGame
    {
        const string SAVED_GAME_PATH = "Saved Games/";

        public SavedGame(string fileName)
        {
            FileName = fileName;
        }

        public string FileName { get; private set; }
        public string GUIName { get; private set; }
        public DateTime LastModified { get; private set; }

        /// <summary>
        /// Get the saved game's name from a .sav file.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private static string GetArchiveName(Stream file)
        {
            var cf = new CompoundFile(file);
            var success = cf.RootStorage.TryGetStream("Scenario Name", out var storage);
            if (!success || storage == null)
            {
                success = cf.RootStorage.TryGetStream("Scenario Description", out storage);
                if (!success || storage == null)
                {
                    throw new InvalidOperationException("Unable to find Scenario Name or Scenario Description in the file.");
                }
            }

            var archiveNameBytes = storage.GetData();
            var archiveName = System.Text.Encoding.Unicode.GetString(archiveNameBytes);
            archiveName = archiveName.TrimEnd('\0');
            return archiveName;
        }

        /// <summary>
        /// Reads and sets the saved game's name and last modified date, and returns true if succesful.
        /// </summary>
        /// <returns>True if parsing the info was succesful, otherwise false.</returns>
        public bool ParseInfo()
        {
            try
            {
                FileInfo savedGameFileInfo = SafePath.GetFile(ProgramConstants.GamePath, SAVED_GAME_PATH, FileName);

                //if (savedGameFileInfo.Exists == false)
                //{
                //    savedGameFileInfo = SafePath.GetFile(ProgramConstants.GamePath, FileName);
                //}

                using (Stream file = savedGameFileInfo.Open(FileMode.Open, FileAccess.Read))
                {
                    GUIName = GetArchiveName(file);
                }

                LastModified = savedGameFileInfo.LastWriteTime;
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log("An error occured while parsing saved game " + FileName + ":" +
                    ex.Message);

                return false;
            }
        }
    }
}
