# splinterlands-tools
A collection of useful splinterlands tools

Join the [Discord server](https://discord.gg/hwSr7KNGs9) for support and to help developing!

Please note that I never originally intended to publish the source code of these tools, so the code is often pure garbage - I'm really sorry about that :D

Pull requests to add new features or improve the code base are very welcome!

## Asset Transfer Bot
**Features:**
For all your accounts:
 - Send all cards to your main
 - Send all DEC to main (you can keep a fixed amount on the accounts)
 - Claim staked SPS & Ranked rewards
 - Claim SPS airdrop (legacy)
 - Send all SPS to main
 - Send chaos packs to main

**Urgently needed features/updates:**
- Replace HttpWebRequest class with an HttpClient that supports gzip compression to avoid hitting splinterlands API limit
- Add a feature to unstake SPS

## Delegation Bot
**Features:**
- Automatically delegate specified cards to all your accounts
- The bot will not delegate the card if the account already has it (owned, delegated, or rented)
- Option to specify if the bot should delegate regular, gold or both foils for each card
- Option to keep the best version of a card on the main

## Reward Statistics Tool Features
-  Creates an Excel sheet (.csv) of all your accounts quest/season reward for every day

## System requirements for all tools
- RAM: 256MB (if you have 10.000+ cards on a single account you might need more for some tools)
- OS: Windows, Linux, MacOS, with some tweaks Android
- CPU: Yes
