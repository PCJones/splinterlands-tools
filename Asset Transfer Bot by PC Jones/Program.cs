using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Globalization;

namespace Asset_Transfer_Bot_by_PC_Jones
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = System.AppContext.BaseDirectory;
            var directory = System.IO.Path.GetDirectoryName(path);
            var config = ReadConfig(directory + "/config/asset_transfer_config.txt");

            bool transferCards = false;
            bool claimSPS = false;
            bool claimSPSAirdrop = false;
            bool transferSPS = false;
            bool transferDEC = false;
            bool transferChaosPacks = false;
            int keepMinDEC = 0;
            string answer1 = "";
            string answer2 = "-";
            if (config.useConfig)
            {
                transferCards = config.sendCards;
                claimSPS = config.claimSPS;
                claimSPSAirdrop = config.claimSPSAirdrop;
                transferSPS = config.sendSPS;
                transferDEC = config.sendDEC;
                keepMinDEC = config.keepDEC;
                transferChaosPacks = config.sendChaosPacks;
                answer1 = config.mainAccount;
                answer2 = config.mainAccount;
                WriteToConsole("Receiving Account: " + answer1.ToString());
                WriteToConsole("Transfer Cards: " + transferCards.ToString());
                WriteToConsole("Claim SPS: " + claimSPS.ToString());
                WriteToConsole("Claim SPS Airdrop: " + claimSPSAirdrop.ToString());
                WriteToConsole("Transfer SPS: " + transferSPS.ToString());
                WriteToConsole("Transfer DEC: " + transferDEC.ToString());
                WriteToConsole("Will keep " + keepMinDEC + " DEC on all accounts");
            }
            else
            {
                WriteToConsole("Don't use config file!");
                WriteToConsole("Enter account to transfer to");
                answer1 = Console.ReadLine();
                WriteToConsole("Enter account to transfer to again");
                answer2 = Console.ReadLine();
                while (answer1 != answer2)
                {
                    WriteToConsole("Username doesn't match!");
                    WriteToConsole("Enter account to transfer to");
                    answer1 = Console.ReadLine();
                    WriteToConsole("Enter account to transfer to again");
                    answer2 = Console.ReadLine();
                }

                WriteToConsole("Transfer cards (y/n, enter is also yes)");
                transferCards = Console.ReadLine() == "n" ? false : true;
                WriteToConsole("Transfer Cards: " + transferCards.ToString());
                WriteToConsole("Transfer Chaos Packs (y/n, enter is also yes)");
                transferChaosPacks = Console.ReadLine() == "n" ? false : true;
                WriteToConsole("Transfer Chaos Packs: " + transferChaosPacks.ToString());
                WriteToConsole("Claim and transfer SPS (y/n, enter is also yes)");
                claimSPS = Console.ReadLine() == "n" ? false : true;
                transferSPS = claimSPS;
                /*claimSPSAirdrop = Console.ReadLine() == "n" ? false : true;
                transferSPS = claimSPSAirdrop;*/
                WriteToConsole("Claim and transfer SPS: " + claimSPS.ToString());
                WriteToConsole("Transfer DEC (y/n, enter is also yes)");
                transferDEC = Console.ReadLine() == "n" ? false : true;
                WriteToConsole("Transfer DEC: " + transferDEC.ToString());
                if (transferDEC)
                {
                    WriteToConsole("Keep n DEC on account? (0 or enter to disable)");
                    var answerMinDEC = Console.ReadLine();
                    keepMinDEC = answerMinDEC == "" ? 0 : Convert.ToInt32(answerMinDEC);
                    WriteToConsole("Will keep " + keepMinDEC + " DEC on all accounts");
                }

                WriteToConsole("Press any key to start");
                Console.ReadLine();
            }

            string[] usernamesRaw = File.ReadAllText(directory + "/config/accounts.txt").Split(Environment.NewLine);
            TransferBot transferBot = new TransferBot(transferSPS, transferDEC, transferCards, transferChaosPacks, keepMinDEC);
            if (claimSPSAirdrop || claimSPS)
            {
                foreach (string loginData in usernamesRaw)
                {
                    try
                    {
                        if (loginData.Trim().Length < 3)
                        {
                            continue;
                        }
                        string username = loginData.Split(':')[0].Trim();
                        string postingKey = loginData.Split(':')[1].Trim();

                        if (claimSPSAirdrop) transferBot.ClaimSPSAirdrop(username, postingKey);
                        if (claimSPS)
                        {
                            transferBot.ClaimSPS(username, postingKey);
                            WriteToConsole("Sleeping 3 seconds to avoid SPS being lost (splinterlands bug)...");
                            Task.Delay(2950).Wait();
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteToConsole("Error: " + ex.Message);
                        WriteToConsole("Error: " + ex.ToString());
                        WriteToConsole("Skipping account");
                    }
                }
                WriteToConsole("Waiting 30 seconds to receive claimed SPS");
                Thread.Sleep(30000);
            }

            foreach (string loginData in usernamesRaw)
            {
                try
                {
                    if (loginData.Trim().Length < 3)
                    {
                        continue;
                    }
                    string loginDataTrimmed = loginData.Trim();

                    string username = loginDataTrimmed.Split(':')[0].Trim();
                    string postingKey = loginDataTrimmed.Split(':')[1].Trim();
                    string activeKey = loginDataTrimmed.Split(':')[2].Trim();
                    string mainAccount = loginDataTrimmed.Split(':').Length > 3 ? loginDataTrimmed.Split(':')[3].Trim() : answer1.Trim();
                    if (username == answer1 && mainAccount == answer1)
                    {
                        continue;
                    }

                    transferBot.StartAccount(username, postingKey, activeKey, mainAccount);


                    WriteToConsole($"Finished account {username}");
                }
                catch (Exception ex)
                {
                    WriteToConsole("Error: " + ex.Message);
                    WriteToConsole("Error: " + ex.ToString());
                    WriteToConsole("Skipping account");
                }
            }
        }

        private static (bool useConfig, bool claimSPSAirdrop, bool claimSPS, bool sendSPS, bool sendDEC, bool sendCards, bool sendChaosPacks, int keepDEC, string mainAccount) ReadConfig(string filePath)
        {
            if (!File.Exists(filePath))
            {
                WriteToConsole("No config.txt in config folder!");
                Console.ReadKey();
                Environment.Exit(0);
            }

            WriteToConsole("Reading config...");
            bool useConfig = false;
            bool claimSPSAirdrop = false;
            bool claimSPS = false;
            bool sendSPS = false;
            bool sendDEC = false;
            bool sendCards = false;
            bool sendChaosPacks = false;
            int keepDEC = 0;
            string mainAccount = "";
            foreach (string setting in File.ReadAllLines(filePath))
            {
                string[] temp = setting.Split('=');
                if (temp.Length != 2 || setting[0] == '#')
                {
                    continue;
                }

                switch (temp[0])
                {
                    case "USE_CONFIG":
                        useConfig = Boolean.Parse(temp[1]);
                        break;
                    case "ONLY_SEND_DEC_ABOVE":
                        keepDEC = Convert.ToInt32(temp[1]);
                        break;
                    case "SEND_SPS":
                        sendSPS = Boolean.Parse(temp[1]);
                        break;
                    case "SEND_CARDS":
                        sendCards = Boolean.Parse(temp[1]);
                        break;
                    case "SEND_DEC":
                        sendDEC = Boolean.Parse(temp[1]);
                        break;
                    case "CLAIM_SPS":
                        claimSPS = Boolean.Parse(temp[1]);
                        break;  
                    case "CLAIM_SPS_AIRDROP":
                        claimSPSAirdrop = Boolean.Parse(temp[1]);
                        break;
                    case "SEND_CHAOS_PACK":
                        sendChaosPacks = Boolean.Parse(temp[1]);
                        break;
                    case "RECEIVING_ACCOUNT":
                        mainAccount = temp[1].Trim().ToLower();
                        break;
                    default:
                        break;
                }
            }

            return (useConfig, claimSPSAirdrop, claimSPS, sendSPS, sendDEC, sendCards, sendChaosPacks, keepDEC, mainAccount);
        }

        private static void WriteToConsole(string message)
        {
            string output = $"[{DateTime.Today.ToShortDateString()} {DateTime.Now:hh:mm:ss}] {message}";
            Console.WriteLine(output);
        }
    }
}
