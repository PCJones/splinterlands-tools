using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Reward_Statistics_Tool_by_PC_Jones
{
    class Program
    {
        private static HttpClient _HttpClient = SetupHttpClient();
        static void Main(string[] args)
        {
            string path = System.AppContext.BaseDirectory;
            var directory = System.IO.Path.GetDirectoryName(path);
            string seperator = ";";
            if (File.Exists(directory + "/csv_seperator.txt"))
            {
                seperator = File.ReadAllText(directory + "/csv_seperator.txt").Trim();
            }

            string[] usernames = File.ReadAllLines(directory + "/config/reward_statistics_accounts.txt")
                .Where(x => x.Trim().Length > 1).Select(x => x.Split(':')[0]).ToArray();

            Console.WriteLine("Get data of last n days or enter date:");
            string answer = Console.ReadLine();
            bool useDesiredDate = false;
            int daysToGet = 30;
            if (DateTime.TryParse(answer, out DateTime desiredDate))
            {
                useDesiredDate = true;
            }
            else
            {
                daysToGet = Convert.ToInt32(answer);
            }

            JArray cardDetails = JArray.Parse(DownloadPageAsync("https://game-api.splinterlands.io/cards/get_details").Result);

            // market details are here because I originally wanted to add the value of the chest as well. Never got to it though
            //JArray marketDetails = JArray.Parse(DownloadPageAsync("https://cache-api.splinterlands.com/market/for_sale_grouped").Result);

            Dictionary<string, Dictionary<DateTime, Dictionary<string, double>>> rewards = new Dictionary<string, Dictionary<DateTime, Dictionary<string, double>>>();
            rewards.Add("dec", new Dictionary<DateTime, Dictionary<string, double>>());
            rewards.Add("sps", new Dictionary<DateTime, Dictionary<string, double>>());
            rewards.Add("pack", new Dictionary<DateTime, Dictionary<string, double>>());
            rewards.Add("merits", new Dictionary<DateTime, Dictionary<string, double>>());
            rewards.Add("potion:gold", new Dictionary<DateTime, Dictionary<string, double>>());
            rewards.Add("potion:legendary", new Dictionary<DateTime, Dictionary<string, double>>());

            
            // Note by jones: sorry for this mess of code :-)
            foreach (string username in usernames)
            {
                JArray rewardHistory = JArray.Parse(DownloadPageAsync($"https://api2.splinterlands.com/players/history?username={ username }&types=claim_reward").Result);

                foreach (JObject reward in rewardHistory)
                {
                    if (((string)reward["success"]).ToLower().Trim() != "true")
                    {
                        continue;
                    }
                    var result = JToken.Parse((string)reward["result"]);

                    foreach (JObject rewardType in result["rewards"])
                    {
                        string type = (string)rewardType["type"];

                        string rewardName = type;
                        
                        DateTime date = DateTime.ParseExact(((string)reward["created_date"]).Split(' ')[0], "MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                        if (useDesiredDate &&  date.Date != desiredDate.Date) 
                        {
                            continue;
                        }
                        double rewardAmount = (double)rewardType["quantity"];

                        switch (type)
                        {
                            case "potion":
                                rewardName += ":" + (string)rewardType["potion_type"];
                                break;
                            case "dec":
                                break;
                            case "merits":
                                break;
                            case "sps":
                                break;
                            case "pack":
                                break;
                            case "credits":
                                break;
                            case "reward_card":
                                int cardID = (int)rewardType["card"]["card_detail_id"];
                                bool gold = (bool)rewardType["card"]["gold"];
                                var cardInfo = cardDetails[cardID - 1];
                                rewardName = gold ? "GOLD:" + (string)cardInfo["name"] : (string)cardInfo["name"];
                                break;
                            default:
                                Console.WriteLine("UNKNOWN REWARD TYPE! - " + type);
                                break;
                        }


                        if (!rewards.ContainsKey(rewardName))
                        {
                            rewards.Add(rewardName, new Dictionary<DateTime, Dictionary<string, double>>());
                        }
                        if (!rewards[rewardName].ContainsKey(date))
                        {
                            rewards[rewardName].Add(date, new Dictionary<string, double>());
                        }
                        if (!rewards[rewardName][date].ContainsKey(username))
                        {
                            rewards[rewardName][date].Add(username, rewardAmount);
                        }
                        else
                        {
                            rewards[rewardName][date][username] = rewards[rewardName][date][username] + rewardAmount;
                        }
                    }
                }
            }

            List<string> csvText = new List<string>();
            string rewardText = "";
            foreach (string reward in rewards.Keys)
            {
                rewardText += seperator +  reward;
            }

            csvText.Add(rewardText);
            List<double> sums = new List<double>();
            for (int i = 0; i < daysToGet; i++)
            {
                Dictionary<string, Dictionary<Point, double>> userNameRows = new Dictionary<string, Dictionary<Point, double>>();
                int columnCount = 0;
                if ((!useDesiredDate || (useDesiredDate && DateTime.Today.AddDays(i * -1).Date == desiredDate.Date)))
                {
                    csvText.Add(Environment.NewLine + DateTime.Today.AddDays(i * -1).ToShortDateString());
                    foreach (var reward in rewards.Values)
                    {
                        columnCount++;
                        foreach (var reward2 in reward)
                        {
                            if (DateTime.Today.AddDays(i * -1).Date == reward2.Key.Date)
                            {
                                foreach (var reward3 in reward2.Value)
                                {
                                    if (!userNameRows.ContainsKey(reward3.Key))
                                    {
                                        userNameRows.Add(reward3.Key, new Dictionary<Point, double>());
                                        userNameRows[reward3.Key].Add(new Point(columnCount, csvText.Count), reward3.Value);
                                        csvText.Add(reward3.Key);
                                    }
                                    else
                                    {
                                        userNameRows[reward3.Key].Add(new Point(columnCount, userNameRows[reward3.Key].ElementAt(0).Key.Y), reward3.Value);
                                        //csvText[userNameRows[reward3.Key]] = ";" + reward3.Value;
                                    }
                                }
                            }
                        }
                    }
                }


                foreach (var row in userNameRows)
                {
                    string csvRow = "";
                    int columnIndex = -1;
                    for (int ii = 1; ii < rewards.Keys.Count + 1; ii++)
                    {
                        csvRow += seperator;
                        foreach (var reward in row.Value)
                        {
                            if (reward.Key.X == ii)
                            {
                                csvRow += reward.Value;
                                columnIndex = reward.Key.Y;
                                while (sums.Count < ii + 1)
                                {
                                    sums.Add(0);
                                }
                                sums[ii] += reward.Value;
                            }
                        }
                    }
                    if (columnIndex >= 0)
                    {
                        csvText[columnIndex] += csvRow;
                    }
                }
            }

            csvText.Add(String.Join(seperator, sums.ToArray()));
            csvText.Add(rewardText);
            string fileName = useDesiredDate ? desiredDate.ToShortDateString().Replace("/", "_") + ".csv" : "last_" + daysToGet.ToString() + "_days.csv";
            File.WriteAllText(fileName, String.Join(Environment.NewLine, csvText.ToArray()));

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var p = new Process();
                p.StartInfo = new ProcessStartInfo(directory + "/" + fileName)
                {
                    UseShellExecute = true
                };
                p.Start();
            }
        }

        public async static Task<string> DownloadPageAsync(string url)
        {
            // Use static HttpClient to avoid exhausting system resources for network connections.
            var result = await _HttpClient.GetAsync(url);
            if (result.StatusCode == HttpStatusCode.TooManyRequests || result.StatusCode == HttpStatusCode.BadGateway)
            {
                Console.WriteLine($"Splinterlands rate limit - sleeping for {40} seconds");
                await Task.Delay(40 * 1000).ConfigureAwait(false);
                return await DownloadPageAsync(url);
            }
            var response = await result.Content.ReadAsStringAsync();
            // Write status code.
            return response;
        }

        private static HttpClient SetupHttpClient()
        {
            var clientHandler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            var httpClient = new HttpClient(clientHandler);
            httpClient.Timeout = new TimeSpan(0, 2, 15);
            httpClient.DefaultRequestHeaders.Add("User-Agent", "USB-Reward/1.0");
            httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));
            return httpClient;
        }
    }
}
