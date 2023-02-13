﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DelegationBot_by_PC_Jones
{
    public record Card : IComparable
    {
        public string card_detail_id { get; init; }
        public string level { get; init; }
        public bool gold { get; init; }
        public bool starter { get; init; }
        [JsonIgnore]
        public string card_long_id { get; init; }

        public Card(string cardId, string _card_long_id, string _level, bool _gold, bool _starter)
        {
            card_detail_id = cardId;
            card_long_id = _card_long_id;
            level = _level;
            gold = _gold;
            starter = _starter;
        }

        public int CompareTo(object? obj)
        {
            if (obj == null)
            {
                return 1;
            }

            Card? otherCard = obj as Card;
            if (otherCard != null)
            {
                int ownCardLevel = Convert.ToInt32(this.level);
                int otherCardLevel = Convert.ToInt32(otherCard.level);
                if (ownCardLevel == otherCardLevel)
                {
                    if (this.gold == otherCard.gold)
                    {
                        if (this.starter == otherCard.starter)
                        {
                            return 0;
                        }
                        else if (!this.starter && otherCard.starter)
                        {
                            return 1;
                        }
                        else
                        {
                            return -1;
                        }
                    }
                    else if (this.gold)
                    {
                        return 1;
                    }
                    else
                    {
                        return -1;
                    }
                }
                else if (ownCardLevel > otherCardLevel)
                {
                    return 1;
                }
                else
                {
                    return -1;
                }
            }
            else
                throw new ArgumentException("Object is not a Card");
        }
    }
}
