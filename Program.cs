using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBot
{
    class Program
    {
        static ITelegramBotClient bot = new TelegramBotClient("6699353245:AAGS-6TdqHA30EJjPMpgCz69oCy4KLN4Lzk"); // токен для telegram bot

        static CityClass CityArr = new CityClass (
            new string[] { "Москва", "Санкт-Петербург", "Казань", "Екатеринбург", "Нижний Новгород", "Выборг", "Самара", "Краснодар", "Сочи", "Уфа", "Красноярск" }, 
            new string[] { "msk", "spb", "kzn", "ekb", "nnv", "vbg", "smr", "krd", "sochi", "ufa", "krasnoyarsk" } 
        ); // Массив со всеми городами


        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            Newtonsoft.Json.JsonConvert.SerializeObject(update); // обработка полученного обновления из json 
      
            try
            {
                var message = update.Message;
                Console.WriteLine($"Пришло сообщение от пользователя {message.Chat.Username}\n" +
                    $"Сообщение: {message.Text}");
                switch (update.Type)
                {
                    case UpdateType.Message:

                        var user = UserClassStore.UserClasses.GetOrAdd(message.Chat.Id, new UserClass { ChatId = message.Chat.Id }); // создание нового пользователя или использование уже зарегистрированного пользователя 

                        await ProcessMessageAsync(botClient, message, user, cancellationToken); // функция с обработкой поведения
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Возникла ошибка {ex}");
                await HandleErrorAsync(botClient, ex, cancellationToken);
            }
        }

        public static async Task ProcessMessageAsync(ITelegramBotClient botClient, Message message, UserClass user, CancellationToken cancellationToken)
        {
            var chat = message.Chat;

            switch (message.Type)
            {
                case MessageType.Text:
                    {

                        if (Array.IndexOf(CityArr.Ru, message.Text) != -1 && user.flagCity == 1) // случай, если пользователь правильно выбрал город
                        {
                            user.CityAndEvent[user.CityIndex] = CityVoid(message);
                            user.flagCity = 2;
                        }
                        else if (Array.IndexOf(CityArr.Ru, message.Text) == -1 && user.flagCity == 1) user.flagCity = 3; // случай, если пользователь неправильно выбрал город

                        if (user.flagEvent == 1 || user.flagEvent == 3)
                        {
                            var infoEvent = await user.Client.GetStringAsync("https://kudago.com/public-api/v1.4/event-categories/?lang=ru"); 
                            var EventArr = JsonConvert.DeserializeObject<EventKindsClass[]>(infoEvent); // получаем все возможные ивенты из API

                            string value = SearchValue(EventArr, message); // проверка написанного ивента со списком
                            if (value != "No") // случай, если такое событие существует в списке
                            {
                                user.CityAndEvent[user.EventIndex] = value;
                                user.flagEvent = 2;
                            }
                            else // случай, если такого события не существует или неправильно написано
                            {
                                user.flagEvent = 3;
                            }
                        }


                        if (message.Text == "/start") // обработка /start
                        {
                            StartSearchMessage(botClient, chat);
                        }

                        if (message.Text == "Поиск") // обработка "Поиск"
                        {
                            SearchSearchMessage(botClient, chat); // Функция для обработки "Поиск"

                            user.flagCity = 1;
                        }

                        if (user.flagCity == 2 || user.flagCity == 3) // обработка полученных данных по поводу города
                        {
                            var infoEvent = await user.Client.GetStringAsync("https://kudago.com/public-api/v1.4/event-categories/?lang=ru");
                            var EventArr = JsonConvert.DeserializeObject<EventKindsClass[]>(infoEvent);
                            CitySearchMessage(botClient, chat, EventArr, user); // вывод пользователю сообщения, если город введено правильно, начинаем вывод списка событий, если нет, то вывод сообщение о ошибке и просим ввести данные заново

                            if (user.flagCity == 3) // если данные не правильны
                                user.flagCity = 1;

                            else if (user.flagCity == 2) // если данные верны
                            {
                                user.flagCity = 100;
                                user.flagEvent = 1;
                            }
                        }

                        if (user.flagEvent == 2 || user.flagEvent == 3) // обработка полученных данных по поводу события
                        {
                            EventSearchMessage(botClient, chat, user); // вывод пользователю сообщения, если событие введено правильно, переходим к след. шагу, если нет, то вывод сообщение о ошибке и просим ввести данные заново

                            if (user.flagEvent == 2) // если данные введены верно
                                user.flagEvent = 100;
                        }

                        if (message.Text == "Получить данные")
                        {
                            long UnixTimeNow = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds(); // получаем текущее время и переводим в Unix формат

                            var info = await user.Client.GetStringAsync("https://kudago.com/public-api/v1.4/events/?lang=&page_size=7&actual_since=" + UnixTimeNow + "&text_format=text&location=" + user.CityAndEvent[0] + "&categories=" + user.CityAndEvent[1] + "&fields=title,site_url,dates,price,place,age_restriction");
                            user.result = JsonConvert.DeserializeObject<Root>(info); // получаем данные из API

                            user.result.Write(botClient, chat, user.MinCountOutput, user.MaxCountOutput); // выводим опреденное количество событий ( количество 5 )
                            MaxCountResult(user.result, ref user.MaxCountOutput); // проверка, можем ли мы в последующих действиях прибавить 5 к MaxCountOutput, если нет, то принимаем -1

                            await Task.Delay(3000);

                            if (user.MaxCountOutput == -1) // случай, если у нас было количество событий меньше 5
                            {
                                await botClient.SendTextMessageAsync(chat.Id, "На этом события закончились");

                                FinalSearchMessage(botClient, chat, true);
                            }

                            else
                            {
                                if (user.result.Count != 0) // случай, если события есть
                                {
                                    var replyKeyboardFurtherAndLess = new ReplyKeyboardMarkup(new KeyboardButton[] { new KeyboardButton("Дальше"), new KeyboardButton("Стоп") }) { ResizeKeyboard = true };
                                    await botClient.SendTextMessageAsync(chat.Id, "Если хотите увидеть еще несколько событий нажмите 'Дальше', если хотите остановить нажмите на 'Стоп'", replyMarkup: replyKeyboardFurtherAndLess);
                                }
                                else // случай, если событий 0
                                {
                                    FinalSearchMessage(botClient, chat, false);
                                }
                            }
                        }

                        if (message.Text == "Дальше") // обработки при нажатии "Дальше"
                        {
                            if (user.MaxCountOutput != -1) // проверка на то, что у MaxCountOutput не минусовое значение
                            {
                                // увеличиваем min и max
                                user.MinCountOutput += 5;
                                user.MaxCountOutput += 5;

                                user.result.Write(botClient, chat, user.MinCountOutput, user.MaxCountOutput); // вывод

                                // такой же участок кода, как и в обработке "Получить данные"
                                MaxCountResult(user.result, ref user.MaxCountOutput);

                                await Task.Delay(3000);

                                if (user.MaxCountOutput == -1)
                                {
                                    await botClient.SendTextMessageAsync(chat.Id, "На этом события закончились");

                                    FinalSearchMessage(botClient, chat, true);
                                }
                            }

                            else
                            {
                                await botClient.SendTextMessageAsync(chat.Id, "На этом события закончились");

                                FinalSearchMessage(botClient, chat, true);
                            }
                        }

                        if (message.Text == "Стоп") // обработка при нажатии "Стоп"
                        {
                            FinalSearchMessage(botClient, chat, true);
                        }
                        break;
                    }
                default:
                    {
                        await botClient.SendTextMessageAsync(chat.Id, "Используйте только текст!");
                        break;
                    }
            }
        }

        public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
        }

        static async Task Main(string[] args)
        {
            Console.WriteLine("Запущен бот " + bot.GetMeAsync().Result.FirstName);

            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }, // receive all update types
            };
            bot.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken
            );
            Console.ReadLine();
        }

        public static string CityVoid(Message line)
        {
            string result = "";

            switch (line.Text)
            {
                case "Москва":
                    result = "msk";
                    break;
                case "Санкт-Петербург":
                    result = "spb";
                    break;
                case "Казань":
                    result = "kzn";
                    break;
                case "Екатеринбург":
                    result = "ekb";
                    break;
                case "Нижний Новгород":
                    result = "nnv";
                    break;
                case "Выборг":
                    result = "vbg";
                    break;
                case "Самара":
                    result = "smr";
                    break;
                case "Краснодар":
                    result = "krd";
                    break;
                case "Сочи":
                    result = "sochi";
                    break;
                case "Уфа":
                    result = "ufa";
                    break;
                case "Красноярск":
                    result = "krasnoyarsk";
                    break;
                default:
                    result = "No";
                    break;
            }
            return result;
        }

        public static string SearchValue(EventKindsClass[] eventArr, Message value)
        {
            for(int i = 0; i < eventArr.GetLength(0); i++)
            {
                if (eventArr[i].Ru == value.Text)
                {
                    return eventArr[i].Eng;
                }   
            }

            return "No";
        }

        public static string AllEventShow(EventKindsClass[] arr)
        {
            string result = "";
            int i = 0;
            foreach(var a in arr)
            {
                result += ++i + ". " + a.ShowS() + "\n";
            }

            return result;
        }

        public static string AllCityShow(CityClass city)
        {
            return city.AllShow();
        }

        public static void MaxCountResult(Root result, ref int max)
        {
            if (max > result.Results.Count && result.Results.Count != 0)
                max = -1;                
        }

        public static async void StartSearchMessage(ITelegramBotClient botClient, Chat chat)
        {
            var replyKeyboardPoisk = new ReplyKeyboardMarkup(new KeyboardButton("Поиск")) { ResizeKeyboard = true };
            await botClient.SendTextMessageAsync(chat.Id, "Добро пожаловать в телеграмм бота Guide-TelegramBot. Здесь вы можете узнать о достопримечательностях и событиях города");
            await botClient.SendTextMessageAsync(chat.Id, "Напиши 'Поиск' для поиска событий!", replyMarkup: replyKeyboardPoisk);
        }

        public static async void CitySearchMessage(ITelegramBotClient botClient, Chat chat, EventKindsClass[] EventArr, UserClass user)
        {
            if (user.flagCity == 3)
            {
                var replyKeyboardCity = new ReplyKeyboardMarkup(new KeyboardButton[]
                {
                                            new KeyboardButton("Москва"),
                                            new KeyboardButton("Казань"),
                                            new KeyboardButton("Санкт-Петербург"),
                })
                { ResizeKeyboard = true };

                await botClient.SendTextMessageAsync(chat.Id, "Возникла ошибка, проверьте правильность введённых данных!");
                await botClient.SendTextMessageAsync(chat.Id, "Выберите город!", replyMarkup: replyKeyboardCity);
            }

            if (user.flagCity == 2)
            {
                await botClient.SendTextMessageAsync(chat.Id, "Хорошо, теперь выберите само мероприятие, которое хотите посетить.");
                await botClient.SendTextMessageAsync(chat.Id, $"Теперь выберим событие, которое нас интересует");
                await botClient.SendTextMessageAsync(chat.Id, AllEventShow(EventArr));

                var replyKeyboardEvent = new ReplyKeyboardMarkup(new KeyboardButton[]
                {
                                            new KeyboardButton("Концерты"),
                                            new KeyboardButton("Фестивали"),
                                            new KeyboardButton("Кинопоказы"),
                                            new KeyboardButton("Выставки"),
                                            new KeyboardButton("Экскурсии")
                })
                { ResizeKeyboard = true };

                await botClient.SendTextMessageAsync(chat.Id, "Выберите одно из событий и напиши его, либо воспользуетесь кнопками (самые популярные события)", replyMarkup: replyKeyboardEvent);
            }
        }

        public static async void EventSearchMessage(ITelegramBotClient botClient, Chat chat, UserClass user)
        {
            if (user.flagEvent == 3)
            {
                await botClient.SendTextMessageAsync(chat.Id, "Возникла ошибка, проверьте правильность введённых данных!");
                await botClient.SendTextMessageAsync(chat.Id, "Возможно вы допустили ошибку в слове (например: в случае данного события '15. Шопинг (Магазины)' вам нужно написать 'Шопинг (Магазины)'), либо такого события не существует!");

                var replyKeyboardEvent = new ReplyKeyboardMarkup(new KeyboardButton[]
                {
                                            new KeyboardButton("Концерты"),
                                            new KeyboardButton("Фестивали"),
                                            new KeyboardButton("Кинопоказы"),
                                            new KeyboardButton("Выставки"),
                                            new KeyboardButton("Экскурсии")
                })
                { ResizeKeyboard = true };

                await botClient.SendTextMessageAsync(chat.Id, "Выберите одно из событий и напиши его, либо воспользуетесь кнопками (самые популярные события)", replyMarkup: replyKeyboardEvent);
            }

            if (user.flagEvent == 2)
            {
                var replyKeyboardData = new ReplyKeyboardMarkup(new KeyboardButton[]
                {
                                            new KeyboardButton("Получить данные")
                })
                { ResizeKeyboard = true };
                await botClient.SendTextMessageAsync(chat.Id, "Информация собрана! Нажми на 'Получить данные'", replyMarkup: replyKeyboardData);
            }
        }



        public static async void FinalSearchMessage(ITelegramBotClient botClient, Chat chat, bool res)
        {
            var replyKeyboardPoisk = new ReplyKeyboardMarkup(new KeyboardButton("Поиск")) { ResizeKeyboard = true };

            if (res)
                await botClient.SendTextMessageAsync(chat.Id, "Надеюсь вам понравился список событий и вы посетите одно из них!\nЕсли захотите найти какие-либо другие событий, смело нажимайте или пишите 'Поиск'", replyMarkup: replyKeyboardPoisk);
            else
                await botClient.SendTextMessageAsync(chat.Id, "Не огорчайтесь, возможно вы бы хотели посетить что нибудь другое?\nЕсли да, то нажимайте или пишите 'Поиск'", replyMarkup: replyKeyboardPoisk);
        }

        public static async void SearchSearchMessage(ITelegramBotClient botClient, Chat chat)
        {
            var replyKeyboardCity = new ReplyKeyboardMarkup(new KeyboardButton[]
                            {
                                            new KeyboardButton("Москва"),
                                            new KeyboardButton("Казань"),
                                            new KeyboardButton("Санкт-Петербург"),
                            })
            { ResizeKeyboard = true };

            await botClient.SendTextMessageAsync(chat.Id, "Сначало выбери город!", replyMarkup: replyKeyboardCity);
            await botClient.SendTextMessageAsync(chat.Id, AllCityShow(CityArr));
            await botClient.SendTextMessageAsync(chat.Id, "Выберите город из списка и напишите его,  либо воспользуетесь кнопками (самые популярные города)");
        }
    }
}