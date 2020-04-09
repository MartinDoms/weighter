using System;
using System.IO;

namespace Weighter
{
    public interface IConfig
    {
        string AppName { get; }
        string AppDataDir { get; }
        string AppDir { get; }
        string WeighInSpreadsheetId { get; }

        string WeighInSheetName { get; }
        string WeighInSheetDateColumn { get; }
        string WeighInSheetEntryColumn { get; }
    }

    public class Config : IConfig
    {
        public string AppName => "Weighter";

        public string AppDataDir => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        public string AppDir => Path.Combine(AppDataDir, AppName);

        public string WeighInSpreadsheetId => "1kz2P2JAQM8-7hTxNmceP1g7Z3T-YKW2-OaeYn8PBhxk";

        public string WeighInSheetName => "Weigh-in";

        public string WeighInSheetDateColumn => "A";

        public string WeighInSheetEntryColumn => "C";
    }
}