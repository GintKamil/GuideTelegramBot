using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBot
{
    internal class CityClass
    {
        public string[] Ru { get; set; }
        public string[] Eng { get; set; }

        public CityClass(string[] ru, string[] eng)
        {
            Ru = ru;
            Eng = eng;
        }

        public string AllShow()
        {
            string result = "";

            for(int i = 0; i < Ru.GetLength(0); i++)
            {
                result += i + 1 + ". " + Ru[i] + "\n";
            }
            return result;
        }
    }
}
