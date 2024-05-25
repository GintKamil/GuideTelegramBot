using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;
using Newtonsoft.Json;

namespace TelegramBot
{
    class EventKindsClass
    {
        [JsonProperty("id")]
        public int Id;

        [JsonProperty("slug")]
        public string Eng;

        [JsonProperty("name")]
        public string Ru;

        public async void Show(ITelegramBotClient botClient, Chat chat, int i)
        {
            await botClient.SendTextMessageAsync(chat.Id, $"{i}. {Ru}");
        }

        public string ShowS()
        {
            return Ru;
        }
    }
}

// gint1k