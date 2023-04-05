using Cryptography.ECDSA;
using HiveAPI.CS;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Asset_Transfer_Bot_by_PC_Jones
{
    public class TransferBot
    {
        private static readonly string SplinterlandsAPI = "https://api2.splinterlands.com";
        private static readonly string SplinterlandsAPIFallback = "https://game-api.splinterlands.io";
        private static readonly string API_URL = "https://api.hive.blog/";
        private static char[] Subset = "0123456789abcdefghijklmnopqrstuvwxyz".ToCharArray();
        private HttpClient _httpClient;
        private Random _Random;
        private CHived oHived;
        private bool TransferSPSActivated;
        private bool TransferDECActivated;
        private bool TransferCardsActivated;
        private bool TransferChaosPacksActivated;
        private int keepMinDEC;
        public TransferBot(bool _transferSPS, bool transferDEC, bool transferCards, bool transferChaosPacks, int _keepMinDEC)
        {
            TransferSPSActivated = _transferSPS;
            TransferDECActivated = transferDEC;
            TransferCardsActivated = transferCards;
            TransferChaosPacksActivated = transferChaosPacks;
            _httpClient = Helper.SetupHttpClient();
            _Random = new Random();
            oHived = new CHived(_httpClient, API_URL);
            keepMinDEC = _keepMinDEC;
        }

        private async Task<string> DownloadPageAsync(string url)
        {
            // Use static HttpClient to avoid exhausting system resources for network connections.
            var result = await _httpClient.GetAsync(url);
            if (result.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                WriteToConsole("Splinterlands API block - wait 180 seconds");
                await Task.Delay(180000);
                return await DownloadPageAsync(url);
            }
            var response = await result.Content.ReadAsStringAsync();
            // Write status code.
            return response;
        }
        private async Task<JToken> GetPlayerBalancesAsync(string username)
        {
            try
            {
                string data = await DownloadPageAsync($"{SplinterlandsAPI}/players/balances?username={ username }");
                if (data == null || data.Trim().Length < 10)
                {
                    // Fallback API
                    WriteToConsole($"{username}: Error with splinterlands API for balances, trying fallback api...");
                    data = await DownloadPageAsync($"{SplinterlandsAPIFallback}/players/quests?username={ username }");
                }
                JToken balances = JToken.Parse(data);
                return balances;

            }
            catch (Exception ex)
            {
                WriteToConsole($"{username}: Could not get balances from splinterlands api: {ex}");
            }
            return null;
        }
        public void StartAccount(string username, string postingKey, string activeKey, string mainAccount)
        {
            try
            {
                WriteToConsole($"Starting account {username}");

                // sorry for this garbage code - jones

                var balances = (JArray)GetPlayerBalancesAsync(username).Result;
                string sps = "0";
                try
                {
                    sps = (string)balances.Where(x => (string)x["token"] == "SPS").First()["balance"];
                }
                catch (Exception)
                {
                }
                string dec = "0";
                try
                {
                    dec = (string)balances.Where(x => (string)x["token"] == "DEC").First()["balance"];
                }
                catch (Exception)
                {
                }

                string chaos  = "0";
                try
                {
                    chaos = (string)balances.Where(x => (string)x["token"] == "CHAOS").First()["balance"];
                }
                catch (Exception)
                {
                }

                if (TransferSPSActivated)
                {
                    TransferSPS(username, activeKey, sps, mainAccount);
                }
                if (TransferChaosPacksActivated)
                {
                    TransferPacks(username, activeKey, chaos, mainAccount);
                }
                if (TransferDECActivated)
                {
                    TransferDEC(username, activeKey, dec, mainAccount);
                }
                if (TransferCardsActivated)
                {
                    TransferCards(username, activeKey, mainAccount);
                }
            }
            catch (Exception ex)
            {
                WriteToConsole("Error: " + ex.Message);
                WriteToConsole("Error: " + ex.ToString());
            }
        }

        private bool TransferPacks(string username, string key, string amount, string mainAccount)
        {
            if (amount == "0" || Convert.ToDecimal(amount, CultureInfo.InvariantCulture) < 0.01M)
            {
                WriteToConsole($"No Packs to transfer for account {username}");
                return false;
            }
            WriteToConsole($"Transfering {amount} Chaos Packs for account {username} to {mainAccount}");
            char[] buf = new char[10];
            for (int i = 0; i < buf.Length; i++)
            {
                int index = _Random.Next(Subset.Length);
                buf[i] = Subset[index];
            }

            string json = @"{""to"":""" + mainAccount + @""",""qty"":" + amount.Replace(",", ".") + @",""edition"":""7"",""memo"":null" + @",""app"":""splinterlands/0.7.139"",""n"":""" + new String(buf) + @"""}";
            COperations.custom_json custom_Json = CreateCustomJson(true, false, "sm_gift_packs", username, json);
            object[] operations = new object[] { custom_Json };

            try
            {
                string txid = oHived.broadcast_transaction(operations, new string[] { key });
                WriteToConsole($"{ username}: Sent chaos pack - TX: " + txid);
            }
            catch (Exception ex)
            {
                WriteToConsole($"{ username}: Error at sending chaos pack " + ex.ToString());
                return false;
            }
            return true;
        }

        private COperations.custom_json CreateCustomJson(bool activeKey, bool postingKey, string methodName, string username, string json)
        {
            COperations.custom_json customJsonOperation = new COperations.custom_json
            {
                required_auths = activeKey ? new string[] { username } : new string[0],
                required_posting_auths = postingKey ? new string[] { username } : new string[0],
                id = methodName,
                json = json
            };
            return customJsonOperation;
        }

        private void TransferCards(string username, string key, string mainAccount)
        {
            var playerCards = JToken.Parse(DownloadPageAsync($"https://api2.splinterlands.com/cards/collection/{ username }").Result);

            List<Card> filteredCards = new(playerCards["cards"].Where(card =>
            {
                bool delegated = card["delegated_to"].Type != JTokenType.Null;
                bool locked = card["lock_days"].Type != JTokenType.Null;
                bool onMarket = (string)card["market_listing_type"] == "RENT" ? true : card["market_listing_type"].Type != JTokenType.Null ? true : false;
                bool gladiatorEdition = (int)card["edition"] == 6;

                if (locked)
                {
                    if (card["unlock_date"].Type != JTokenType.Null)
                    {
                        if (DateTime.Now > (DateTime)card["unlock_date"])
                        {
                            locked = false;
                        }
                    }
                }

                return !delegated && !onMarket && !locked && !gladiatorEdition;
            })
            .Select(x => new Card((string)x["card_detail_id"], (string)x["uid"], (string)x["level"], (bool)x["gold"]))
            .Distinct().ToArray());

            List<string> cardIdsToSend = new List<string>();
            foreach (Card card in filteredCards)
            {
                string cardID = card.card_long_id;
                cardIdsToSend.Add(cardID);
            }

            if (cardIdsToSend.Count == 0)
            {
                WriteToConsole($"No cards to transfer for account {username}");
                return;
            }

            WriteToConsole($"Transfering { cardIdsToSend.Count } cards from account {username} to {mainAccount}");
            char[] buf = new char[10];
            for (int i = 0; i < buf.Length; i++)
            {
                int index = _Random.Next(Subset.Length);
                buf[i] = Subset[index];
            }

            string json = "{\"to\": \"" + mainAccount + "\", \"cards\": [ \"" + String.Join("\",\"", cardIdsToSend) + "\" ] }";
            COperations.custom_json custom_Json = CreateCustomJson(true, false, "sm_gift_cards", username, json);
            object[] operations = new object[] { custom_Json };

            try
            {
                string txid = oHived.broadcast_transaction(operations, new string[] { key });
                WriteToConsole($"{ username}: Transfered cards: " + txid);
            }
            catch (Exception ex)
            {
                WriteToConsole($"{ username}: Error at sending cards " + ex.ToString());
            }
        }
        private bool TransferSPS(string username, string key, string amount, string mainAccount)
        {
            if (amount == "0" || Convert.ToDecimal(amount, CultureInfo.InvariantCulture) < 0.01M)
            {
                WriteToConsole($"No SPS to transfer for account {username}");
                return false;
            }
            WriteToConsole($"Transfering {amount} SPS for account {username} to to {mainAccount}");
            char[] buf = new char[10];
            for (int i = 0; i < buf.Length; i++)
            {
                int index = _Random.Next(Subset.Length);
                buf[i] = Subset[index];
            }

            string json = @"{""to"":""" + mainAccount + @""",""qty"":" + amount.Replace(",", ".") + @",""token"":""SPS"",""type"":""withdraw"",""memo"":""" + mainAccount + @""",""app"":""splinterlands/0.7.139"",""n"":""" + new String(buf) + @"""}";
            COperations.custom_json custom_Json = CreateCustomJson(true, false, "sm_token_transfer", username, json);
            object[] operations = new object[] { custom_Json };

            try
            {
                string txid = oHived.broadcast_transaction(operations, new string[] { key });
                WriteToConsole($"{ username}: Sent SPS - TX: " + txid);
            }
            catch (Exception ex)
            {
                WriteToConsole($"{ username}: Error at sending SPS " + ex.ToString());
                return false;
            }
            return true;
        }

        private bool TransferDEC(string username, string key, string amount, string mainAccount)
        {
            decimal decAmountToSend = Convert.ToDecimal(amount, CultureInfo.InvariantCulture) - keepMinDEC;
            if (decAmountToSend < 1)
            {
                WriteToConsole($"No DEC to transfer for account {username}");
                return false;
            }
            WriteToConsole($"Transfering {decAmountToSend} DEC for account {username} to to {mainAccount}");
            char[] buf = new char[10];
            for (int i = 0; i < buf.Length; i++)
            {
                int index = _Random.Next(Subset.Length);
                buf[i] = Subset[index];
            }

            COperations.custom_json customJsonOperation = new COperations.custom_json
            {
                required_auths = new string[] { username },
                required_posting_auths = new string[0],
                id = "sm_token_transfer",
                json = @"{""to"":""" + mainAccount + @""",""qty"":" + decAmountToSend.ToString().Replace(",", ".") + @",""token"":""DEC"",""type"":""withdraw"",""memo"":""" + mainAccount + @""",""app"":""splinterlands/0.7.139"",""n"":""" + new String(buf) + @"""}"
            };

            try
            {
                string txid = oHived.broadcast_transaction(new object[] { customJsonOperation }, new string[] { key });
                WriteToConsole($"{ username}: Sent DEC - TX: " + txid);
            }
            catch (Exception ex)
            {
                WriteToConsole($"{ username}: Error at sending DEC " + ex.ToString());
                return false;
            }
            return true;
        }

        public bool ClaimSPS(string username, string key)
        {
            WriteToConsole($"Claiming stake & ranked reward SPS for account {username}");
            char[] buf = new char[10];
            for (int i = 0; i < buf.Length; i++)
            {
                int index = _Random.Next(Subset.Length);
                buf[i] = Subset[index];
            }

            COperations.custom_json customJsonOperation = new COperations.custom_json
            {
                required_auths = Array.Empty<string>(),
                required_posting_auths = new string[] { username },
                id = "sm_claim_rewards",
                json = @"{""app"":""splinterlands/0.7.139"",""n"":""" + new string(buf) + @"""}"
            };

            try
            {
                string txid = oHived.broadcast_transaction(new object[] { customJsonOperation }, new string[] { key });
                WriteToConsole($"{ username}: Claimed SPS - TX: " + txid);
            }
            catch (Exception ex)
            {
                WriteToConsole($"{ username}: Error at claiming SPS " + ex.ToString());
                return false;
            }
            return true;
        }
        public decimal ClaimSPSAirdrop(string username, string postingKey)
        {
            var bid = "bid_" + Helper.GenerateRandomString(20);
            var sid = "sid_" + Helper.GenerateRandomString(20);
            var ts = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds().ToString();
            var hash = Sha256Manager.GetHash(Encoding.ASCII.GetBytes(username + ts));
            var sig = Secp256K1Manager.SignCompressedCompact(hash, CBase58.DecodePrivateWif(postingKey));
            var signature = Hex.ToString(sig);
            var response = DownloadPageAsync("https://api2.splinterlands.com" + "/players/login?name=" + username + "&ref=&browser_id=" + bid + "&session_id=" + sid + "&sig=" + signature + "&ts=" + ts).Result;

            var token = Helper.DoQuickRegex("\"name\":\"" + username + "\",\"token\":\"([A-Z0-9]{10})\"", response);
            ts = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds().ToString();
            hash = Sha256Manager.GetHash(Encoding.ASCII.GetBytes("hive" + username + ts));
            sig = Secp256K1Manager.SignCompressedCompact(hash, CBase58.DecodePrivateWif(postingKey));
            signature = Hex.ToString(sig);
            response = DownloadPageAsync($"https://ec-api.splinterlands.com/players/claim_sps_airdrop?platform=hive&address={username}&sig={signature}&ts={ts}&token={token}&username={username}").Result;

            if (response.Contains("No SPS airdrop tokens are available to be claimed"))
            {
                WriteToConsole($"No claimable SPS Airdrop for account {username}");
            }
            else if (response.Contains("success\":true"))
            {
                WriteToConsole($"Claimed {Helper.DoQuickRegex("qty\":(.*?),", response)} SPS Airdrop for account {username}");
                return Convert.ToDecimal(Helper.DoQuickRegex("qty\":(.*?),", response), CultureInfo.InvariantCulture);
            }
            else
            {
                WriteToConsole($"Claim SPS Airdrop error for account {username}: {response}");
            }

            return 0;
        }
       
        private static void WriteToConsole(string message)
        {
            string output = $"[{DateTime.Today.ToShortDateString()} {DateTime.Now:hh:mm:ss}] {message}";
            Console.WriteLine(output);
        }
    }
}
