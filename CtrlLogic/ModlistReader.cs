using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DayZServerControllerUI.CtrlLogic
{
    internal class ModlistReader
    {
        private readonly FileInfo _modlistInfo;

        public ModlistReader(FileInfo modlistInfo)
        {
            if (!modlistInfo.Exists)
                throw new ArgumentException($"Modlist not found {modlistInfo}!");

            _modlistInfo = modlistInfo;
        }

        /// <summary>
        /// Gets the IDs and Trivial ModNames from the Modlist
        /// </summary>
        /// <returns></returns>
        public async Task<Dictionary<long, string>?> GetModsFromFile()
        {
            Dictionary<long, string>? modDict = new();

            using StreamReader sr = new(_modlistInfo.FullName);
            while(!sr.EndOfStream)
            {
                string? modListLine = await sr.ReadLineAsync();

                if (String.IsNullOrEmpty(modListLine))
                    continue;

                string[] splittedModListLine = modListLine.Split(',');

                if (splittedModListLine.Length != 2 || 
                    String.IsNullOrEmpty(splittedModListLine[0]) || String.IsNullOrEmpty(splittedModListLine[1]))
                    continue;

                if (!Int64.TryParse(splittedModListLine[0], out Int64 workshopID))
                    continue;

                modDict.Add(workshopID, splittedModListLine[1]);
            }

            return modDict;
        }
    }
}
