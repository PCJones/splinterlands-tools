using HiveAPI.CS;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Linq;

namespace Delegation_Bot_by_PC_Jones
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = AppContext.BaseDirectory;
            var directory = Path.GetDirectoryName(path);

            var (delegateFrom, delegateToAccountsTxtFile, delegateTo, cardsToDelegate, cardsToDelegateRegular, cardsToDelegateGold, keepBestCardOnMain) = ReadConfig(directory + "/config/delegation_config.txt");
            string[] usernamesRaw = File.ReadAllText(directory + "/config/delegation_accounts.txt").Split(Environment.NewLine);

            if (delegateToAccountsTxtFile)
            {
                delegateTo = usernamesRaw.Select(x => x.Split(':')[0].Trim()).ToList();
            }

            string activeKey = usernamesRaw.First(x => x.Split(':')[0].Trim() == delegateFrom).Split(':')[2];

            if (activeKey == null)
            {
                Console.WriteLine("Error - main is not in delegation_accounts.txt");
                Console.ReadLine();
                Environment.Exit(0);
            }

            delegateTo.Remove(delegateFrom);

            if (delegateTo.Count == 0)
            {
                Console.WriteLine("Error: No accounts to delegate to!");
                Console.ReadLine();
                Environment.Exit(0);
            }

            JArray cardDetails = JArray.Parse(Helper.DownloadPageAsync("https://api2.splinterlands.com/cards/get_details").Result);
            CHived oHived = new CHived(Helper.HttpClient, "https://api.deathwing.me/");

            List<Card> availableCards = Delegation.GetDelegatableCards(delegateFrom);

            int customJsonCount = 0;
            foreach (var account in delegateTo)
            {
                var delegatedCards = Delegation.DelegateCards(oHived, account, activeKey, delegateFrom, cardsToDelegate, cardsToDelegateRegular,
                    cardsToDelegateGold, availableCards, cardDetails, keepBestCardOnMain).Result;

                if (delegatedCards)
                {
                    customJsonCount++;
                }

                // I'm not sure anymore why this is needed but I think it's to avoid hitting the 5 transactions/block hive limit
                while (customJsonCount > 3)
                {
                    Task.Delay(3000).Wait();
                    customJsonCount = 0;
                }
            }

            Console.WriteLine("Delegations finished!");
            Console.ReadLine();
        }

        private static (string delegateFrom, bool delegateToAccountsTxtFile, List<string> delegateTo, string[] cardsToDelegate, string[] cardsToDelegateRegular, string[] cardsToDelegateGold, bool keepBestCardOnMain) ReadConfig(string filePath)
        {
            if (!File.Exists(filePath))
            {
                WriteToConsole("No delegation_settings.txt in config folder!");
                Console.ReadKey();
                Environment.Exit(0);
            }

            WriteToConsole("Reading config...");
            string delegateFrom = "";
            bool delegateToAccountsTxtFile = false;
            List<string> delegateTo = new();
            string[] cardsToDelegate = Array.Empty<String>();
            string[] cardsToDelegateRegular = Array.Empty<String>();
            string[] cardsToDelegateGold = Array.Empty<String>();
            bool keepBestCardOnMain = true;


            foreach (string setting in File.ReadAllLines(filePath))
            {
                string[] temp = setting.Split('=');
                if (temp.Length != 2 || setting[0] == '#')
                {
                    continue;
                }

                switch (temp[0].Trim())
                {
                    case "DELEGATE_FROM":
                        delegateFrom = temp[1].Trim().ToLower();
                        break;
                    case "DELEGATE_TO_ACCOUNTS_TXT_FILE":
                        delegateToAccountsTxtFile = Boolean.Parse(temp[1]);
                        break;
                    case "DELEGATE_TO":
                        delegateTo = new (temp[1].Split(','));
                        break;
                    case "CARDS_TO_DELEGATE":
                        cardsToDelegate = temp[1].Split(',');
                        break;
                    case "CARDS_TO_DELEGATE_REGULAR":
                        cardsToDelegateRegular = temp[1].Split(',');
                        break;
                    case "CARDS_TO_DELEGATE_GOLD":
                        cardsToDelegateGold = temp[1].Split(',');
                        break;
                    case "KEEP_BEST_CARD_ON_MAIN":
                        keepBestCardOnMain = bool.Parse(temp[1]);
                        break;
                    default:
                        break;
                }
            }

            return (delegateFrom, delegateToAccountsTxtFile, delegateTo, cardsToDelegate, cardsToDelegateRegular, cardsToDelegateGold, keepBestCardOnMain);
        }

        private static void WriteToConsole(string message)
        {
            string output = $"[{DateTime.Today.ToShortDateString()} {DateTime.Now:hh:mm:ss}] {message}";
            Console.WriteLine(output);
        }
    }
}