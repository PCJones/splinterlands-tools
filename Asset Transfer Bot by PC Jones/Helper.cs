using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Asset_Transfer_Bot_by_PC_Jones
{
    public static class Helper
    {
        public static char[] Subset = "0123456789abcdefghijklmnopqrstuvwxyz".ToCharArray();        
        public static Random Random = new();
        public static string GenerateMD5Hash(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }
        public static string GenerateRandomString(int n)
        {
            char[] buf = new char[n];
            for (int i = 0; i < buf.Length; i++)
            {
                int index = Random.Next(Subset.Length);
                buf[i] = Subset[index];
            }

            return new string(buf);
        }

        public static string DoQuickRegex(string Pattern, string Match)
        {
            Regex r = new Regex(Pattern, RegexOptions.Singleline);
            return r.Match(Match).Groups[1].Value;
        }
    }
}
