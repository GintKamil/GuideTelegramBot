using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;
using System.ComponentModel;

namespace TelegramBot
{
    public class Date
    {
        [JsonProperty("start")]
        public long Start { get; set; }

        [JsonProperty("end")]
        public long End { get; set; }
    }

    public class Place
    {
        [JsonProperty("id")]
        public int Id { get; set; }
    }

    public class Result
    {
        [JsonProperty("dates")]
        public List<Date> Dates { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("place")]
        public Place Place { get; set; }

        [JsonProperty("age_restriction")]
        public string AgeRestriction { get; set; }

        [JsonProperty("price")]
        public string Price { get; set; }

        [JsonProperty("site_url")]
        public string SiteUrl { get; set; }
    }

    public class Root
    {
        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("next")]
        public string Next { get; set; }

        [JsonProperty("previous")]
        public object Previous { get; set; }

        [JsonProperty("results")]
        public List<Result> Results { get; set; }

        public async void Write(ITelegramBotClient botClient, Chat chat, int Min, int Max)
        {
            if (Count == 0)
            {
                await botClient.SendTextMessageAsync(chat.Id, "К сожалению, не найдено данных событий в выбранном городе.");
            }

            for (int i = Min; i < Max; i++)
            {
                try
                {
                    await botClient.SendTextMessageAsync(chat.Id, Output(Results[i]));
                    await Task.Delay(500);
                }

                catch (ArgumentOutOfRangeException)
                {
                    break;
                }
            }
        }

        private string Output(Result item)
        {
            return $"Название - {item.Title}\nВозрастное ограничение - {item.AgeRestriction}\nЦена - {item.Price}\nДля более подробной информации - {item.SiteUrl}";
        }
    }
}
