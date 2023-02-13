using HiveAPI.CS;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Delegation_Bot_by_PC_Jones
{
    internal class Delegation
    {
        private static Random Random = new();
        private static char[] Subset = "0123456789abcdefghijklmnopqrstuvwxyz".ToCharArray();
        public static List<Card> GetDelegatableCards(string main)
        {
            try
            {
                string data = Helper.DownloadPageAsync($"https://api2.splinterlands.com/cards/collection/{ main }").Result;
                List<Card> availableCards = new(JToken.Parse(data)["cards"].Where(card =>
                {
                    bool cardIsDelegated = card["delegated_to"].Type != JTokenType.Null;
                    bool listedOnMarket = card["market_listing_type"].Type != JTokenType.Null;

                    return !cardIsDelegated && !listedOnMarket;
                })
                .Select(x => new Card((string)x["card_detail_id"], (string)x["uid"], (string)x["level"], (bool)x["gold"], false))
                .Distinct().ToArray());

                availableCards.Sort();
                availableCards.Reverse();

                return availableCards;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not get cards from main: " + ex.Message);
            }
            return new List<Card>();
        }
        public static async Task<bool> DelegateCards(CHived oHived, string to, string activeKey, string from, string[] cardsToDelegate, string[] cardsToDelegateRegular, string[] cardsToDelegateGold, List<Card> availableCards, JArray cardDetails, bool keepBestCardOnMain)
        {
            try
            {
                int skip = keepBestCardOnMain ? 1 : 0;
                string data = await Helper.DownloadPageAsync($"https://api2.splinterlands.com/cards/collection/{ to }");
                List<Card> userCards = new(JToken.Parse(data)["cards"].Where(card =>
                {
                    string currentUser = card["delegated_to"].Type == JTokenType.Null ? (string)card["player"] : (string)card["delegated_to"];
                    bool listedOnMarket = card["market_listing_type"].Type != JTokenType.Null;

                    return currentUser == to && !listedOnMarket;
                })
                .Select(x => new Card((string)x["card_detail_id"], (string)x["uid"], (string)x["level"], (bool)x["gold"], false))
                .Distinct().ToArray());

                List<string> cardsToDelegateString = new();
                List<string> ranOutOfString = new();
                List<Card> cardsToDelegateList = new();

                foreach (var cardId in cardsToDelegateGold)
                {
                    if (userCards.Any(x => x.card_detail_id == cardId && x.gold)
                        || cardId == "")
                    {
                        continue;
                    }
                    var cardToDelegate = availableCards.Where(x => x.card_detail_id == cardId && x.gold).Skip(skip).FirstOrDefault();
                    string cardName = (string)cardDetails[Convert.ToInt32(cardId) - 1]["name"] + "(Gold)";
                    if (cardToDelegate == null)
                    {
                        ranOutOfString.Add(cardName);
                        continue;
                    }

                    cardsToDelegateString.Add(cardName);
                    cardsToDelegateList.Add(cardToDelegate);
                    availableCards.Remove(cardToDelegate);
                }

                foreach (string cardId in cardsToDelegateRegular)
                {
                    if (userCards.Any(x => x.card_detail_id == cardId && !x.gold)
                        || cardId == "")
                    {
                        continue;
                    }
                    var cardToDelegate = availableCards.Where(x => x.card_detail_id == cardId && !x.gold).Skip(skip).FirstOrDefault();
                    string cardName = (string)cardDetails[Convert.ToInt32(cardId) - 1]["name"] + "(Regular)";
                    if (cardToDelegate == null)
                    {
                        ranOutOfString.Add(cardName);
                        continue;
                    }

                    cardsToDelegateString.Add(cardName);
                    cardsToDelegateList.Add(cardToDelegate);
                    availableCards.Remove(cardToDelegate);
                }

                foreach (var cardId in cardsToDelegate)
                {
                    if (userCards.Any(x => x.card_detail_id == cardId)
                        || cardId == "")
                    {
                        continue;
                    }
                    var cardToDelegate = availableCards.Where(x => x.card_detail_id == cardId).Skip(skip).FirstOrDefault();
                    string cardName = (string)cardDetails[Convert.ToInt32(cardId) - 1]["name"] + "(Gold & Regular)";
                    if (cardToDelegate == null)
                    {
                        ranOutOfString.Add(cardName);
                        continue;
                    }

                    cardsToDelegateString.Add(cardName);
                    cardsToDelegateList.Add(cardToDelegate);
                    availableCards.Remove(cardToDelegate);
                }

                if (ranOutOfString.Any())
                {
                    Console.WriteLine($"{to}: Ran out of cards: " + String.Join(", ", ranOutOfString));
                }

                bool delegatedCards = false;
                if (cardsToDelegateString.Any())
                {
                    Console.WriteLine($"{to}: Delegating cards: " + string.Join("," + Environment.NewLine, cardsToDelegateString));
                    delegatedCards = true;
                }
                else
                {
                    return delegatedCards;
                }

                List<string> cardIdsToDelegate = new();
                foreach (Card card in cardsToDelegateList)
                {
                    string cardID = card.card_long_id;
                    cardIdsToDelegate.Add(cardID);
                }

                char[] buf = new char[10];
                for (int i = 0; i < buf.Length; i++)
                {
                    int index = Random.Next(Subset.Length);
                    buf[i] = Subset[index];
                }

                string json = "{\"to\": \"" + to + "\", \"cards\": [ \"" + String.Join("\",\"", cardIdsToDelegate) + "\" ] }";
                COperations.custom_json custom_Json = CreateCustomJson(true, false, "sm_delegate_cards", from, json);
                object[] operations = new object[] { custom_Json };

                try
                {
                    string txid = oHived.broadcast_transaction(operations, new string[] { activeKey });
                    await Task.Delay(500);
                    Console.WriteLine($"{ to}: Delegating cards: " + txid);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ to}: Error at delegating cards " + ex.ToString());
                }
                return delegatedCards;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{to}: Error - {ex.Message}");
            }
            return true;
        }

        private static COperations.custom_json CreateCustomJson(bool activeKey, bool postingKey, string methodName, string username, string json)
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
    }
}
