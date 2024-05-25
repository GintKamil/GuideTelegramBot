using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBot
{
    class UserClass
    {
        public long ChatId { get; set; }
        public string[] CityAndEvent = new string[2] { "", "" };
        public int CityIndex = 0;
        public int EventIndex = 1;
        public HttpClient Client = new();
        public int flagCity = 0, flagEvent = 0;
        public int MinCountOutput = 0, MaxCountOutput = 5;
        public Root result;
    }
}
