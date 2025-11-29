using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using HtmlAgilityPack;

namespace MyTaskBot
{
    class Program
    {
        private static ITelegramBotClient _botClient = null!;
        private static HttpListener _httpListener = null!;
        private static DateTime startTime;

        // Московский часовой пояс
        private static readonly TimeZoneInfo moscowTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");

        // ТОЛЬКО из переменных окружения
        private static readonly string BotToken = Environment.GetEnvironmentVariable("BOT_TOKEN")
            ?? throw new Exception("BOT_TOKEN environment variable is required");

        private static readonly HttpClient httpClient = new HttpClient();

        // Счетчики для последовательной отправки картинок
        private static readonly Dictionary<string, int> currentIndexes = new Dictionary<string, int>();

        // Хранилище для ожидания ответов о тексте песен
        private static readonly Dictionary<long, string> pendingLyricsRequests = new Dictionary<long, string>();

        // Хранилище для отслеживания активного меню пользователя
        private static readonly Dictionary<long, string> userActiveMenu = new Dictionary<long, string>();

        // Pinterest ссылки
        private static readonly Dictionary<string, List<string>> PinterestUrls = new Dictionary<string, List<string>>
        {
            {
                "sam", new List<string>
                {
                    "https://ru.pinterest.com/pin/1022809765384911507/",
                    "https://ru.pinterest.com/pin/1022809765384911763/",
                    "https://ru.pinterest.com/pin/1022809765384911758/",
                    "https://ru.pinterest.com/pin/1022809765384911712/",
                    "https://ru.pinterest.com/pin/1022809765384913914/",
                    "https://ru.pinterest.com/pin/1022809765384911774/",
                    "https://ru.pinterest.com/pin/1022809765384911715/",
                    "https://ru.pinterest.com/pin/1022809765384994001/",
                    "https://ru.pinterest.com/pin/1022809765384914247/",
                    "https://ru.pinterest.com/pin/1022809765384913449/",
                    "https://ru.pinterest.com/pin/1022809765384912941/",
                    "https://ru.pinterest.com/pin/1022809765384911777/",
                    "https://ru.pinterest.com/pin/1022809765384911727/",
                    "https://ru.pinterest.com/pin/1022809765384911696/",
                    "https://ru.pinterest.com/pin/1022809765384911709/",
                    "https://ru.pinterest.com/pin/1022809765384911513/",
                    "https://ru.pinterest.com/pin/1022809765384911448/",
                    "https://ru.pinterest.com/pin/1022809765384911431/",
                    "https://ru.pinterest.com/pin/1022809765384911426/",
                    "https://ru.pinterest.com/pin/1022809765384910237/",
                    "https://ru.pinterest.com/pin/1022809765384910239/"
                }
            },
            {
                "dean", new List<string>
                {
                    "https://ru.pinterest.com/pin/1022809765384916141/",
                    "https://ru.pinterest.com/pin/1022809765384913176/",
                    "https://ru.pinterest.com/pin/1022809765384913154/",
                    "https://ru.pinterest.com/pin/1022809765384913317/",
                    "https://ru.pinterest.com/pin/1022809765384913958/",
                    "https://ru.pinterest.com/pin/1022809765384913479/",
                    "https://ru.pinterest.com/pin/1022809765384913185/",
                    "https://ru.pinterest.com/pin/1022809765384911596/",
                    "https://ru.pinterest.com/pin/1022809765384911578/",
                    "https://ru.pinterest.com/pin/1022809765384913776/",
                    "https://ru.pinterest.com/pin/1022809765384992025/",
                    "https://ru.pinterest.com/pin/1022809765384992057/",
                    "https://ru.pinterest.com/pin/1022809765384992099/",
                    "https://ru.pinterest.com/pin/1022809765384992114/",
                    "https://ru.pinterest.com/pin/1022809765384992139/",
                    "https://ru.pinterest.com/pin/1022809765384992210/",
                    "https://ru.pinterest.com/pin/1022809765384994013/",
                    "https://ru.pinterest.com/pin/1022809765384994053/",
                    "https://ru.pinterest.com/pin/1022809765384912837/",
                    "https://ru.pinterest.com/pin/1022809765384911617/",
                    "https://ru.pinterest.com/pin/1022809765384992324/",
                    "https://ru.pinterest.com/pin/1022809765384992322/"
                }
            },
            {
                "cas", new List<string>
                {
                    "https://ru.pinterest.com/pin/1022809765384913656/",
                    "https://ru.pinterest.com/pin/1022809765384913627/",
                    "https://ru.pinterest.com/pin/1022809765384913189/",
                    "https://ru.pinterest.com/pin/1022809765384913141/",
                    "https://ru.pinterest.com/pin/1022809765384913008/",
                    "https://ru.pinterest.com/pin/1022809765384913878/",
                    "https://ru.pinterest.com/pin/AY6UzOXBv2FzRN-ffIFy9vYjJyWGDuL7KU3d3mosYahOqxOnGTCI7X0/",
                    "https://ru.pinterest.com/pin/1022809765384913676/",
                    "https://ru.pinterest.com/pin/1022809765384913697/",
                    "https://ru.pinterest.com/pin/1022809765384913703/",
                    "https://ru.pinterest.com/pin/1022809765384913678/",
                    "https://ru.pinterest.com/pin/1022809765384913713/",
                    "https://ru.pinterest.com/pin/1022809765384913718/",
                    "https://ru.pinterest.com/pin/1022809765384913311/",
                    "https://ru.pinterest.com/pin/1022809765384990839/",
                    "https://ru.pinterest.com/pin/1022809765384914013/",
                    "https://ru.pinterest.com/pin/1022809765384913795/",
                    "https://ru.pinterest.com/pin/1022809765384914019/",
                    "https://ru.pinterest.com/pin/1022809765384913681/",
                    "https://ru.pinterest.com/pin/1022809765384992124/",
                    "https://ru.pinterest.com/pin/1022809765384993999/",
                    "https://ru.pinterest.com/pin/1022809765384994017/",
                    "https://ru.pinterest.com/pin/1022809765384994026/",
                    "https://ru.pinterest.com/pin/1022809765384994048/",
                    "https://ru.pinterest.com/pin/1022809765384994056/"
                }
            },
            {
                "mem", new List<string>
                {
                    "https://ru.pinterest.com/pin/1022809765384913229/",
                    "https://ru.pinterest.com/pin/1022809765384916533/",
                    "https://ru.pinterest.com/pin/1022809765384916552/",
                    "https://ru.pinterest.com/pin/1022809765384913814/",
                    "https://ru.pinterest.com/pin/1022809765384913435/",
                    "https://ru.pinterest.com/pin/1022809765384913331/",
                    "https://ru.pinterest.com/pin/1022809765384913128/",
                    "https://ru.pinterest.com/pin/1022809765384913245/",
                    "https://ru.pinterest.com/pin/1022809765384913247/",
                    "https://ru.pinterest.com/pin/1022809765384913253/",
                    "https://ru.pinterest.com/pin/1022809765384913282/",
                    "https://ru.pinterest.com/pin/1022809765384914232/",
                    "https://ru.pinterest.com/pin/1022809765384914076/",
                    "https://ru.pinterest.com/pin/1022809765384913992/",
                    "https://ru.pinterest.com/pin/1022809765384913609/",
                    "https://ru.pinterest.com/pin/1022809765384913782/",
                    "https://ru.pinterest.com/pin/1022809765384914081/",
                    "https://ru.pinterest.com/pin/1022809765384913370/",
                    "https://ru.pinterest.com/pin/1022809765384913412/",
                    "https://ru.pinterest.com/pin/1022809765384913296/",
                    "https://ru.pinterest.com/pin/1022809765384992190/",
                    "https://ru.pinterest.com/pin/1022809765384992181/",
                    "https://ru.pinterest.com/pin/1022809765384916558/",
                    "https://ru.pinterest.com/pin/1022809765384916483/",
                    "https://ru.pinterest.com/pin/1022809765384916434/",
                    "https://ru.pinterest.com/pin/1022809765384916480/",
                    "https://ru.pinterest.com/pin/1022809765384916460/",
                    "https://ru.pinterest.com/pin/1022809765384914132/",
                    "https://ru.pinterest.com/pin/1022809765384914269/",
                    "https://ru.pinterest.com/pin/1022809765384913987/",
                    "https://ru.pinterest.com/pin/1022809765384913757/",
                    "https://ru.pinterest.com/pin/1022809765384913388/",
                    "https://ru.pinterest.com/pin/1022809765384913264/",
                    "https://ru.pinterest.com/pin/1022809765384913289/",
                    "https://ru.pinterest.com/pin/1022809765384913267/",
                    "https://ru.pinterest.com/pin/1022809765384913287/",
                    "https://ru.pinterest.com/pin/1022809765384913279/",
                    "https://ru.pinterest.com/pin/1022809765384913260/",
                    "https://ru.pinterest.com/pin/1022809765384913243/",
                    "https://ru.pinterest.com/pin/1022809765384913585/"
                }
            },
            {
                "supernatural", new List<string>
                {
                    "https://ru.pinterest.com/pin/1022809765384990849/",
                    "https://ru.pinterest.com/pin/1022809765384990843/",
                    "https://ru.pinterest.com/pin/1022809765384990852/",
                    "https://ru.pinterest.com/pin/1022809765384923071/",
                    "https://ru.pinterest.com/pin/1022809765384923055/",
                    "https://ru.pinterest.com/pin/1022809765384923030/",
                    "https://ru.pinterest.com/pin/1022809765384923019/",
                    "https://ru.pinterest.com/pin/1022809765384916487/",
                    "https://ru.pinterest.com/pin/1022809765384916418/",
                    "https://ru.pinterest.com/pin/1022809765384916397/",
                    "https://ru.pinterest.com/pin/1022809765384916396/",
                    "https://ru.pinterest.com/pin/1022809765384916334/",
                    "https://ru.pinterest.com/pin/AY6UzOXBv2FzRN-ffIFy9vYjJyWGDuL7KU3d3mosYahOqxOnGTCI7X0/",
                    "https://ru.pinterest.com/pin/1022809765384916206/",
                    "https://ru.pinterest.com/pin/1022809765384916203/",
                    "https://ru.pinterest.com/pin/1022809765384916152/",
                    "https://ru.pinterest.com/pin/1022809765384916398/",
                    "https://ru.pinterest.com/pin/1022809765384923051/",
                    "https://ru.pinterest.com/pin/1022809765384994007/",
                    "https://ru.pinterest.com/pin/1022809765384994003/",
                    "https://ru.pinterest.com/pin/1022809765384994020/",
                    "https://ru.pinterest.com/pin/1022809765384994022/",
                    "https://ru.pinterest.com/pin/1022809765384994024/",
                    "https://ru.pinterest.com/pin/1022809765384994035/",
                    "https://ru.pinterest.com/pin/1022809765384994009/",
                    "https://ru.pinterest.com/pin/1022809765384994057/",
                    "https://ru.pinterest.com/pin/1022809765384994061/",
                    "https://ru.pinterest.com/pin/1022809765384994075/",
                    "https://ru.pinterest.com/pin/1022809765384916348/",
                    "https://ru.pinterest.com/pin/1022809765384916372/",
                    "https://ru.pinterest.com/pin/1022809765384913844/",
                    "https://ru.pinterest.com/pin/1022809765384913832/",
                    "https://ru.pinterest.com/pin/1022809765384913816/",
                    "https://ru.pinterest.com/pin/1022809765384913740/",
                    "https://ru.pinterest.com/pin/1022809765384913817/",
                    "https://ru.pinterest.com/pin/1022809765384913095/",
                    "https://ru.pinterest.com/pin/1022809765384913699/",
                    "https://ru.pinterest.com/pin/1022809765384913902/"
                }
            }
        };

        // Цитаты из сериала
        private static readonly Dictionary<int, string> Quotes = new Dictionary<int, string>
        {
            { 1, "«Дин: Иногда ты делаешь вещи, которые имеют смысл только для тебя.»" },
            { 2, "«Сэм: Мы не можем изменить прошлое, но можем бороться за будущее.»" },
            { 3, "«Кастиэль: Я не игрушка, который вы можете включать и выключать.»" },
            { 4, "«Дин: Семья - это не только кровь. Это те, ради кого ты готов на всё.»" },
            { 5, "«Бобби: Идиоты! Я окружён идиотами!»" },
            { 6, "«Кроули: Здравствуйте, мальчики.»" },
            { 7, "«Дин: Спасибо, брат.»" },
            { 8, "«Сэм: Мы спасаем людей, охотимся на нечисть. Семейный бизнес.»" },
            { 9, "«Дин: Призраки, демоны, вампиры - это всё в порядке вещей. Но клоуны... Клоуны меня пугают.»" },
            { 10, "«Кастиэль: Я изучал человечество. Это интересный вид.»" },
            { 11, "«Дин: Пицца или смерть!»" },
            { 12, "«Сэм: Иногда правильный путь - не самый лёгкий.»" },
            { 13, "«Дин: Я не герой. Я просто делаю то, что должен.»" },
            { 14, "«Бобби: Если вы собираетесь быть идиотами, то будьте умными идиотами.»" },
            { 15, "«Кастиэль: Я не ангел. Я воин Бога.»" },
            { 16, "«Дин: Driver picks the music, shotgun shuts his cakehole.»" },
            { 17, "«Сэм: Мы всегда будем вместе, Дин. Неважно, что случится.»" },
            { 18, "«Дин: Семья - это всё.»" },
            { 19, "«Кастиэль: Я научился лгать у вас, людей.»" },
            { 20, "«Дин: Это не конец. Это никогда не конец.»" },
            { 21, "«Сэм: Мы пережили слишком много, чтобы сдаться сейчас.»" },
            { 22, "«Дин: Пиво решает все проблемы.»" },
            { 23, "«Кастиэль: Люди - самые опасные существа на Земле.»" },
            { 24, "«Дин: Я предпочитаю бургеры философии.»" },
            { 25, "«Сэм: Мы не выбираем свою судьбу, но мы выбираем, как с ней бороться.»" },
            { 26, "«Дин: Иногда монстры оказываются не такими уж и монстрами.»" },
            { 27, "«Кастиэль: Я начинаю понимать иронию.»" },
            { 28, "«Дин: Лучше умереть стоя, чем жить на коленях.»" },
            { 29, "«Сэм: Каждый заслуживает второго шанса.»" },
            { 30, "«Дин: Impala - это не просто машина. Это дом.»" },
            { 31, "«Кастиэль: Вы, люди, маленькие, но удивительные.»" },
            { 32, "«Дин: Никогда не оставляй своего брата.»" },
            { 33, "«Сэм: Сила не в том, чтобы не падать, а в том, чтобы подниматься каждый раз.»" },
            { 34, "«Дин: Иногда нужно нарушать правила, чтобы делать правильные вещи.»" },
            { 35, "«Кастиэль: Дружба - это странное человеческое понятие, но мне нравится.»" },
            { 36, "«Дин: Мы не идеальны, но мы стараемся.»" },
            { 37, "«Сэм: Настоящая сила - в прощении.»" },
            { 38, "«Дин: Мир не чёрно-белый. Он серый, как мое любимое пиво.»" },
            { 39, "«Кастиэль: Я выбрал свою сторону - сторону человечества.»" },
            { 40, "«Дин: Мы будем бороться до конца. Потому что так делают Винчестеры.»" }
        };

        // База музыки
        private static readonly Dictionary<string, string> MusicFiles = new Dictionary<string, string>
        {
            {
                "🎸 Carry On Wayward Son",
                "https://drive.google.com/uc?export=download&id=1GiPnahoB9wWB9xNC9y5bvB8_6dgc9miK"
            },
            {
                "🐅 Eye of the Tiger",
                "https://drive.google.com/uc?export=download&id=1ms2Lv91tS37PEEKbts604f7F6vOzScku"
            },
            {
                "🎶 Supernatural Theme",
                "https://drive.google.com/uc?export=download&id=1vMWhDYFEb549qA_pVG0TmTm5RHSp7i1t"
            }
        };

        // Тексты песен
        private static readonly Dictionary<string, string> SongLyrics = new Dictionary<string, string>
        {
            {
                "🎸 Carry On Wayward Son",
                @"🎵 Carry On Wayward Son - Kansas

Carry on, my wayward son
There'll be peace when you are done
Lay your weary head to rest
Don't you cry no more

Once I rose above the noise and confusion
Just to get a glimpse beyond this illusion
I was soaring ever higher
But I flew too high

Though my eyes could see, I still was a blind man
Though my mind could think, I still was a mad man
I hear the voices when I'm dreaming
I can hear them say

Carry on, my wayward son
There'll be peace when you are done
Lay your weary head to rest
Don't you cry no more

Masquerading as a man with a reason
My charade is the event of the season
And if I claim to be a wise man
Well, it surely means that I don't know

On a stormy sea of moving emotion
Tossed about, I'm like a ship on the ocean
I set a course for winds of fortune
But I hear the voices say

Carry on, my wayward son
There'll be peace when you are done
Lay your weary head to rest
Don't you cry no more

Carry on, you will always remember
Carry on, nothing equals the splendor
Now your life's no longer empty
Surely heaven waits for you

Carry on, my wayward son
There'll be peace when you are done
Lay your weary head to rest
Don't you cry no more"
            },
            {
                "🐅 Eye of the Tiger",
                @"🎵 Eye of the Tiger - Survivor

Risin' up, back on the street
Did my time, took my chances
Went the distance, now I'm back on my feet
Just a man and his will to survive

So many times, it happens too fast
You trade your passion for glory
Don't lose your grip on the dreams of the past
You must fight just to keep them alive

It's the eye of the tiger
It's the thrill of the fight
Risin' up to the challenge of our rival
And the last known survivor
Stalks his prey in the night
And he's watchin' us all with the eye of the tiger

Face to face, out in the heat
Hangin' tough, stayin' hungry
They stack the odds 'til we take to the street
For the kill with the skill to survive

It's the eye of the tiger
It's the thrill of the fight
Risin' up to the challenge of our rival
And the last known survivor
Stalks his prey in the night
And he's watchin' us all with the eye of the tiger

Risin' up, straight to the top
Had the guts, got the glory
Went the distance, now I'm not gonna stop
Just a man and his will to survive

It's the eye of the tiger
It's the thrill of the fight
Risin' up to the challenge of our rival
And the last known survivor
Stalks his prey in the night
And he's watchin' us all with the eye of the tiger

The eye of the tiger
The eye of the tiger
The eye of the tiger
The eye of the tiger"
            }
        };

        // Главная клавиатура
        private static readonly ReplyKeyboardMarkup MainKeyboard = new(new[]
        {
            new[]
            {
                new KeyboardButton("📖 Цитатник"),
                new KeyboardButton("🖼️ Картинки"),
                new KeyboardButton("🎵 Музыка")
            },
            new[]
            {
                new KeyboardButton("🕐 Время"),
                new KeyboardButton("📅 Дата"),
            },
            new[]
            {
                new KeyboardButton("ℹ️ Помощь")
            }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };

        // Клавиатура с картинками
        private static readonly ReplyKeyboardMarkup ImagesKeyboard = new(new[]
        {
            new[]
            {
                new KeyboardButton("👦 Сэм"),
                new KeyboardButton("👨 Дин"),
                new KeyboardButton("👼 Кас")
            },
            new[]
            {
                new KeyboardButton("😄 Мемы"),
                new KeyboardButton("🎬 Кадры")
            },
            new[] { new KeyboardButton("🔙 Назад") }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };

        // Клавиатура для цитатника
        private static readonly ReplyKeyboardMarkup QuotesKeyboard = new(new[]
        {
            new[] { new KeyboardButton("1-10"), new KeyboardButton("11-20") },
            new[] { new KeyboardButton("21-30"), new KeyboardButton("31-40") },
            new[] { new KeyboardButton("🔙 Назад") }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };

        // Клавиатура для музыки
        private static readonly ReplyKeyboardMarkup MusicKeyboard = new(new[]
        {
            new[]
            {
                new KeyboardButton("🎸 Carry On Wayward Son"),
                new KeyboardButton("🐅 Eye of the Tiger")
            },
            new[]
            {
                new KeyboardButton("🎶 Supernatural Theme"),
            },
            new[] { new KeyboardButton("🔙 Назад") }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };

        // Клавиатура для подтверждения текста песни
        private static readonly ReplyKeyboardMarkup LyricsConfirmationKeyboard = new(new[]
        {
            new[] { new KeyboardButton("✅ Да"), new KeyboardButton("❌ Нет") },
            new[] { new KeyboardButton("🔙 Назад") }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };

        // Простой HTTP сервер для health checks
        private static async Task StartHttpServer()
        {
            try
            {
                _httpListener = new HttpListener();
                _httpListener.Prefixes.Add("http://*:10000/");
                _httpListener.Start();
                Console.WriteLine("🌐 HTTP Server started on port 10000");

                _ = Task.Run(async () =>
                {
                    while (_httpListener.IsListening)
                    {
                        try
                        {
                            var context = await _httpListener.GetContextAsync();
                            var response = context.Response;

                            string responseText = $"🤖 Telegram Bot is Running!\n" +
                                                $"⏰ Uptime: {DateTime.Now - startTime:dd\\.hh\\:mm\\:ss}\n" +
                                                $"💾 Memory: {GC.GetTotalMemory(false) / 1024 / 1024} MB\n" +
                                                $"✅ Status: Active";

                            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseText);

                            response.ContentLength64 = buffer.Length;
                            response.ContentType = "text/plain; charset=utf-8";

                            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                            response.Close();

                            Console.WriteLine("✅ Health check request handled");
                        }
                        catch (Exception ex)
                        {
                            if (_httpListener.IsListening)
                                Console.WriteLine($"❌ HTTP Server error: {ex.Message}");
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to start HTTP server: {ex.Message}");
            }
        }

        // Keep-alive для предотвращения сна на бесплатном тарифе
        private static async Task StartKeepAliveService()
        {
            try
            {
                var keepAliveClient = new HttpClient();
                string? serviceUrl = Environment.GetEnvironmentVariable("RENDER_SERVICE_URL");

                if (string.IsNullOrEmpty(serviceUrl))
                {
                    Console.WriteLine("ℹ️ RENDER_SERVICE_URL not set, using default health check");
                    return;
                }

                _ = Task.Run(async () =>
                {
                    while (true)
                    {
                        try
                        {
                            var response = await keepAliveClient.GetAsync(serviceUrl);
                            Console.WriteLine($"✅ Keep-alive ping sent - Status: {response.StatusCode}");
                            
                            // Ждем 14 минут между запросами (бесплатный тариф позволяет 750 часов в месяц)
                            await Task.Delay(TimeSpan.FromMinutes(14));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ Keep-alive ping failed: {ex.Message}");
                            await Task.Delay(TimeSpan.FromMinutes(1)); // Ждем меньше при ошибке
                        }
                    }
                });

                Console.WriteLine("✅ Keep-alive service started");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to start keep-alive service: {ex.Message}");
            }
        }

        static async Task Main(string[] args)
        {
            // Глобальная обработка исключений для надежности
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Console.WriteLine($"💥 Критическая ошибка: {e.ExceptionObject}");
                // Не выходим из приложения, перезапуск будет через Render
            };

            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                Console.WriteLine($"💥 Необработанная ошибка задачи: {e.Exception}");
                e.SetObserved();
            };

            Console.OutputEncoding = System.Text.Encoding.UTF8;
            startTime = DateTime.Now;

            // Перезапуск приложения при ошибках
            int restartCount = 0;
            const int maxRestarts = 10;
            
            while (restartCount < maxRestarts)
            {
                try
                {
                    Console.WriteLine($"🚀 Запуск бота на Render.com... (Попытка {restartCount + 1})");
                    Console.WriteLine($"⏰ Время запуска: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    Console.WriteLine($"🔧 .NET Version: {Environment.Version}");
                    Console.WriteLine($"💻 OS: {Environment.OSVersion}");

                    // Запускаем HTTP сервер первым
                    await StartHttpServer();
                    await Task.Delay(2000);

                    // Запускаем keep-alive service
                    await StartKeepAliveService();

                    // Проверяем наличие токена
                    if (string.IsNullOrEmpty(BotToken))
                    {
                        Console.WriteLine("❌ ОШИБКА: BOT_TOKEN не установлен!");
                        Console.WriteLine("ℹ️ Установите переменную окружения BOT_TOKEN на Render.com");
                        await Task.Delay(5000);
                        restartCount++;
                        continue;
                    }

                    Console.WriteLine("✅ BOT_TOKEN загружен из переменных окружения");

                    // Инициализируем счетчики
                    foreach (var character in PinterestUrls.Keys)
                    {
                        currentIndexes[character] = 0;
                    }

                    _botClient = new TelegramBotClient(BotToken);

                    // Настройка HttpClient для Pinterest
                    httpClient.DefaultRequestHeaders.Add("User-Agent",
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
                    httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
                    httpClient.DefaultRequestHeaders.Add("Accept-Language", "ru-RU,ru;q=0.8,en-US;q=0.5,en;q=0.3");
                    httpClient.Timeout = TimeSpan.FromSeconds(30);

                    // Проверка подключения
                    var me = await _botClient.GetMeAsync();
                    Console.WriteLine($"✅ Бот {me.FirstName} успешно запущен!");
                    Console.WriteLine($"👤 ID бота: {me.Id}");
                    Console.WriteLine($"📝 Username: @{me.Username}");

                    // Настройки получения сообщений
                    var receiverOptions = new ReceiverOptions
                    {
                        AllowedUpdates = Array.Empty<UpdateType>(),
                        ThrowPendingUpdates = true
                    };

                    // Начинаем принимать сообщения
                    _botClient.StartReceiving(
                        updateHandler: HandleUpdateAsync,
                        pollingErrorHandler: HandlePollingErrorAsync,
                        receiverOptions: receiverOptions,
                        cancellationToken: CancellationToken.None
                    );

                    Console.WriteLine("📱 Бот запущен и ожидает сообщений...");
                    Console.WriteLine($"🎯 Статус: Активен 24/7 на Render.com");

                    // Бесконечное ожидание с периодическим логированием
                    using var timer = new Timer(_ =>
                    {
                        var uptime = DateTime.Now - startTime;
                        Console.WriteLine($"🤖 Бот активен. Время работы: {uptime:dd\\.hh\\:mm\\:ss}");
                        Console.WriteLine($"💾 Память: {GC.GetTotalMemory(false) / 1024 / 1024} MB");
                    }, null, TimeSpan.Zero, TimeSpan.FromMinutes(10));

                    // Ожидаем завершения
                    await Task.Delay(Timeout.Infinite);
                }
                catch (Exception ex)
                {
                    restartCount++;
                    Console.WriteLine($"❌ Критическая ошибка при запуске (Попытка {restartCount}): {ex.Message}");
                    Console.WriteLine($"📋 StackTrace: {ex.StackTrace}");

                    // Останавливаем HTTP сервер при ошибке
                    try
                    {
                        _httpListener?.Stop();
                        _httpListener?.Close();
                        Console.WriteLine("🔴 HTTP Server stopped");
                    }
                    catch (Exception stopEx)
                    {
                        Console.WriteLine($"❌ Error stopping HTTP server: {stopEx.Message}");
                    }

                    // Очищаем ресурсы
                    try
                    {
                        _botClient?.CloseAsync();
                        httpClient?.Dispose();
                    }
                    catch { }

                    if (restartCount >= maxRestarts)
                    {
                        Console.WriteLine($"🚨 Достигнут лимит перезапусков ({maxRestarts}). Завершение работы.");
                        Environment.Exit(1);
                    }

                    Console.WriteLine($"🔄 Перезапуск через 10 секунд...");
                    await Task.Delay(10000);
                }
            }
        }

        // Обработчик входящих сообщений
        static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message is not { } message)
                return;

            if (message.Text is not { } messageText)
                return;

            var chatId = message.Chat.Id;
            var userName = message.From?.FirstName ?? "Пользователь";

            Console.WriteLine($"📩 Сообщение от {userName} ({chatId}): '{messageText}'");

            try
            {
                // Обработка числовых вводов для цитат - ТОЛЬКО если пользователь в меню цитат
                if (int.TryParse(messageText, out int quoteNumber) && quoteNumber >= 1 && quoteNumber <= 40)
                {
                    // Проверяем, находится ли пользователь в меню цитат
                    bool isInQuotesMenu = userActiveMenu.ContainsKey(chatId) &&
                                         (userActiveMenu[chatId] == "quotes" ||
                                          userActiveMenu[chatId] == "quotes_range");

                    if (isInQuotesMenu)
                    {
                        await SendQuote(botClient, chatId, quoteNumber, cancellationToken);
                        return;
                    }
                    else
                    {
                        // Если число введено не в контексте цитат, игнорируем его
                        Console.WriteLine($"ℹ️ Число {quoteNumber} проигнорировано (не в меню цитат)");
                        // Продолжаем обычную обработку сообщения
                    }
                }

                // Проверка на ответ о тексте песни
                if (pendingLyricsRequests.ContainsKey(chatId))
                {
                    await HandleLyricsResponse(botClient, chatId, messageText, cancellationToken);
                    return;
                }

                string lowerMessage = messageText.ToLower().Trim();

                var commandMap = new Dictionary<string, string>
                {
                    { "👦 сэм", "sam" },
                    { "👨 дин", "dean" },
                    { "👼 кас", "cas" },
                    { "😄 мемы", "mem" },
                    { "🎬 кадры", "supernatural" },
                    { "🕐 время", "/time" },
                    { "📅 дата", "/date" },
                    { "📖 цитатник", "/quotes" },
                    { "🎵 музыка", "/music" },
                    { "ℹ️ помощь", "/help" },
                    { "🖼️ картинки", "/images" },
                    { "🔙 назад", "/back" },
                    { "1-10", "/range1" },
                    { "11-20", "/range2" },
                    { "21-30", "/range3" },
                    { "31-40", "/range4" },
                    { "🎸 carry on wayward son", "/music_carryon" },
                    { "🐅 eye of the tiger", "/music_eye" },
                    { "🎶 supernatural theme", "/music_theme" },
                    { "✅ да", "/lyrics_yes" },
                    { "❌ нет", "/lyrics_no" }
                };

                string command = lowerMessage;

                if (commandMap.ContainsKey(messageText))
                {
                    command = commandMap[messageText];
                }
                else if (commandMap.ContainsKey(lowerMessage))
                {
                    command = commandMap[lowerMessage];
                }
                else if (!lowerMessage.StartsWith("/") && PinterestUrls.ContainsKey(lowerMessage))
                {
                    command = lowerMessage;
                }
                else if (MusicFiles.ContainsKey(messageText))
                {
                    command = commandMap[messageText];
                }

                // Обработка навигационных команд
                if (command == "/images")
                {
                    userActiveMenu[chatId] = "images";
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "🖼️ Выберите категорию картинок:",
                        replyMarkup: ImagesKeyboard,
                        cancellationToken: cancellationToken);
                    return;
                }
                else if (command == "/quotes")
                {
                    userActiveMenu[chatId] = "quotes";
                    await ShowQuotesMenu(botClient, chatId, cancellationToken);
                    return;
                }
                else if (command == "/music")
                {
                    userActiveMenu[chatId] = "music";
                    await ShowMusicMenu(botClient, chatId, cancellationToken);
                    return;
                }
                else if (command.StartsWith("/range"))
                {
                    userActiveMenu[chatId] = "quotes_range";
                    await ShowQuoteRange(botClient, chatId, command, cancellationToken);
                    return;
                }
                else if (command.StartsWith("/music_"))
                {
                    await AskForLyrics(botClient, chatId, command, cancellationToken);
                    return;
                }
                else if (command == "/back")
                {
                    userActiveMenu[chatId] = "main";
                    pendingLyricsRequests.Remove(chatId); // Очищаем ожидание ответа при возврате
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "🔙 Возвращаемся в главное меню",
                        replyMarkup: MainKeyboard,
                        cancellationToken: cancellationToken);
                    return;
                }

                if (PinterestUrls.ContainsKey(command.TrimStart('/')))
                {
                    string character = command.TrimStart('/');
                    await SendPinterestImage(botClient, chatId, character, cancellationToken);
                }
                else
                {
                    userActiveMenu[chatId] = "main";
                    string responseText = ProcessCommand(command, userName);
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: responseText,
                        replyMarkup: MainKeyboard,
                        cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при обработке сообщения: {ex.Message}");

                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "❌ Произошла ошибка при обработке вашего сообщения.",
                    replyMarkup: MainKeyboard,
                    cancellationToken: cancellationToken);
            }
        }

        // Обработчик ошибок
        static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine($"❌ Ошибка бота: {errorMessage}");
            return Task.CompletedTask;
        }

        // Обработка команд
        private static string ProcessCommand(string command, string userName)
        {
            string lowerCommand = command.ToLower().Trim();

            switch (lowerCommand)
            {
                case "/start":
                    return $"👋 Привет, {userName}!\n\n" +
                           "Я ваш супернатуральный бот! Используйте кнопки ниже для навигации.\n\n" +
                           "📋 Доступные команды:\n\n" +
                           "📖 Цитатник - цитаты из сериала\n" +
                           "🖼️ Картинки - выбрать категорию картинок\n" +
                           "🎵 Музыка - музыка из сериала\n" +
                           "🕐 Время - текущее время\n" +
                           "📅 Дата - текущая дата\n" +
                           "ℹ️ Помощь - показать справку";

                case "/help":
                    return "📋 Справка по командам:\n\n" +
                           "📖 Цитатник - выбрать цитату из сериала (1-40)\n" +
                           "🖼️ Картинки - открыть меню с картинками персонажей\n" +
                           "👦 Сэм - картинка Сэма Винчестера\n" +
                           "👨 Дин - картинка Дина Винчестера\n" +
                           "👼 Кас - картинка Кастиэля\n" +
                           "😄 Мемы - мем по сериалу\n" +
                           "🎬 Кадры - кадр из сериала (и не только)\n" +
                           "🎵 Музыка - музыка из сериала\n" +
                           "🕐 Время - узнать текущее время\n" +
                           "📅 Дата - узнать текущую дата\n\n" +
                           "🔙 Назад - вернуться в главное меню";

                case "/time":
                    var moscowTime = TimeZoneInfo.ConvertTime(DateTime.Now, moscowTimeZone);
                    return $"🕐 Текущее время: {moscowTime:HH:mm:ss}";

                case "/date":
                    var moscowDate = TimeZoneInfo.ConvertTime(DateTime.Now, moscowTimeZone);
                    return $"📅 Сегодня: {moscowDate:dd.MM.yyyy}\n" +
                           $"День недели: {GetRussianDayOfWeek(moscowDate.DayOfWeek)}";

                default:
                    return $"❌ Неизвестная команда: {command}\n\n" +
                           "Используйте кнопки ниже или /help для просмотра всех команд";
            }
        }

        // Вспомогательный метод для получения русского названия дня недели
        private static string GetRussianDayOfWeek(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday => "Понедельник",
                DayOfWeek.Tuesday => "Вторник",
                DayOfWeek.Wednesday => "Среда",
                DayOfWeek.Thursday => "Четверг",
                DayOfWeek.Friday => "Пятница",
                DayOfWeek.Saturday => "Суббота",
                DayOfWeek.Sunday => "Воскресенье",
                _ => "Неизвестный день"
            };
        }

        // Основной метод для работы с Pinterest
        private static async Task SendPinterestImage(ITelegramBotClient botClient, long chatId, string character, CancellationToken cancellationToken)
        {
            try
            {
                if (!PinterestUrls.ContainsKey(character) || !PinterestUrls[character].Any())
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "❌ Не найдены изображения для этого персонажа",
                        replyMarkup: ImagesKeyboard,
                        cancellationToken: cancellationToken);
                    return;
                }

                // Получаем текущий индекс для этого персонажа
                int currentIndex = currentIndexes[character];
                var urls = PinterestUrls[character];

                // Выбираем следующую картинку по порядку
                var pinterestUrl = urls[currentIndex];

                // Увеличиваем индекс для следующего раза
                currentIndex++;
                if (currentIndex >= urls.Count)
                {
                    currentIndex = 0;
                }
                currentIndexes[character] = currentIndex;

                Console.WriteLine($"🔗 Отправляем картинку {currentIndex + 1}/{urls.Count} для {character}: {pinterestUrl}");

                var imageUrl = await GetImageUrlFromPinterest(pinterestUrl);

                if (string.IsNullOrEmpty(imageUrl))
                {
                    throw new Exception("Не удалось найти изображение на странице Pinterest");
                }

                Console.WriteLine($"🖼️ Найдено изображение: {imageUrl}");

                var imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
                Console.WriteLine($"✅ Изображение скачано ({imageBytes.Length} байт)");

                using var stream = new MemoryStream(imageBytes);
                await botClient.SendPhotoAsync(
                    chatId: chatId,
                    photo: InputFile.FromStream(stream, $"{character}.jpg"),
                    caption: "",
                    replyMarkup: ImagesKeyboard,
                    cancellationToken: cancellationToken);

                Console.WriteLine($"✅ Изображение отправлено!");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка: {ex.Message}");

                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "❌ Не удалось загрузить изображение. Pinterest может блокировать запросы.",
                    replyMarkup: ImagesKeyboard,
                    cancellationToken: cancellationToken);
            }
        }

        // Парсинг Pinterest для получения прямой ссылки на изображение
        private static async Task<string?> GetImageUrlFromPinterest(string pinterestUrl)
        {
            try
            {
                Console.WriteLine($"🔍 Парсим Pinterest страницу...");

                var html = await httpClient.GetStringAsync(pinterestUrl);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var metaImage = doc.DocumentNode.SelectSingleNode("//meta[@property='og:image']");
                if (metaImage != null)
                {
                    var url = metaImage.GetAttributeValue("content", "");
                    if (!string.IsNullOrEmpty(url))
                    {
                        Console.WriteLine($"✅ Найдено в og:image: {url}");
                        return url;
                    }
                }

                metaImage = doc.DocumentNode.SelectSingleNode("//meta[@name='pinterest:image']");
                if (metaImage != null)
                {
                    var url = metaImage.GetAttributeValue("content", "");
                    if (!string.IsNullOrEmpty(url))
                    {
                        Console.WriteLine($"✅ Найдено в pinterest:image: {url}");
                        return url;
                    }
                }

                var scriptTags = doc.DocumentNode.SelectNodes("//script");
                if (scriptTags != null)
                {
                    foreach (var script in scriptTags)
                    {
                        var content = script.InnerHtml;
                        if (content.Contains("\"images\"") && content.Contains("pinimg.com"))
                        {
                            var patterns = new[]
                            {
                                "\"url\":\"(https://i\\.pinimg\\.com[^\"]+)\"",
                                "\"original\":{\"url\":\"(https://i\\.pinimg\\.com[^\"]+)\""
                            };

                            foreach (var pattern in patterns)
                            {
                                var match = System.Text.RegularExpressions.Regex.Match(content, pattern);
                                if (match.Success && match.Groups.Count > 1)
                                {
                                    var url = match.Groups[1].Value;
                                    Console.WriteLine($"✅ Найдено в JSON: {url}");
                                    return url;
                                }
                            }
                        }
                    }
                }

                var imgTags = doc.DocumentNode.SelectNodes("//img[@src]");
                if (imgTags != null)
                {
                    foreach (var img in imgTags)
                    {
                        var src = img.GetAttributeValue("src", "");
                        if (src.Contains("pinimg.com") &&
                            (src.Contains(".jpg") || src.Contains(".png") || src.Contains(".jpeg")))
                        {
                            if (!src.Contains("75x75_") && !src.Contains("236x") && !src.Contains("_fw_"))
                            {
                                Console.WriteLine($"✅ Найдено в img tag: {src}");
                                return src;
                            }
                        }
                    }
                }

                Console.WriteLine("❌ Не удалось найти изображение на странице");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка парсинга Pinterest: {ex.Message}");
                return null;
            }
        }

        // Методы для работы с цитатником
        private static async Task ShowQuotesMenu(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"📖 Цитатник Сверхъестественного\n\n" +
                      $"Всего цитат: {Quotes.Count}\n" +
                      $"Введите номер цитаты (от 1 до {Quotes.Count}) или выберите диапазон:",
                replyMarkup: QuotesKeyboard,
                cancellationToken: cancellationToken);
        }

        private static async Task ShowQuoteRange(ITelegramBotClient botClient, long chatId, string rangeCommand, CancellationToken cancellationToken)
        {
            int start = 1, end = 10;

            switch (rangeCommand)
            {
                case "/range1": start = 1; end = 10; break;
                case "/range2": start = 11; end = 20; break;
                case "/range3": start = 21; end = 30; break;
                case "/range4": start = 31; end = 40; break;
            }

            string rangeText = $"📖 Цитаты {start}-{end}:\n\n";

            for (int i = start; i <= end; i++)
            {
                if (Quotes.ContainsKey(i))
                {
                    rangeText += $"{i}. {Quotes[i]}\n\n";
                }
            }

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: rangeText,
                replyMarkup: QuotesKeyboard,
                cancellationToken: cancellationToken);
        }

        private static async Task SendQuote(ITelegramBotClient botClient, long chatId, int quoteNumber, CancellationToken cancellationToken)
        {
            if (Quotes.ContainsKey(quoteNumber))
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"📖 Цитата #{quoteNumber}\n\n{Quotes[quoteNumber]}",
                    replyMarkup: QuotesKeyboard,
                    cancellationToken: cancellationToken);
            }
            else
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"❌ Цитата с номером {quoteNumber} не найдена.\n" +
                          $"Введите число от 1 до {Quotes.Count}",
                    replyMarkup: QuotesKeyboard,
                    cancellationToken: cancellationToken);
            }
        }

        // Методы для работы с музыкой
        private static async Task ShowMusicMenu(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "🎵 Музыка из Сверхъестественного\n\n" +
                      "Выберите трек для прослушивания:",
                replyMarkup: MusicKeyboard,
                cancellationToken: cancellationToken);
        }

        // Метод для запроса о тексте песни
        private static async Task AskForLyrics(ITelegramBotClient botClient, long chatId, string musicCommand, CancellationToken cancellationToken)
        {
            try
            {
                var musicMap = new Dictionary<string, string>
                {
                    { "/music_carryon", "🎸 Carry On Wayward Son" },
                    { "/music_eye", "🐅 Eye of the Tiger" },
                    { "/music_theme", "🎶 Supernatural Theme" }
                };

                if (!musicMap.ContainsKey(musicCommand))
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "❌ Музыкальный трек не найден",
                        replyMarkup: MusicKeyboard,
                        cancellationToken: cancellationToken);
                    return;
                }

                var musicName = musicMap[musicCommand];

                // Для треков с текстом спрашиваем подтверждение
                if (SongLyrics.ContainsKey(musicName))
                {
                    // Сохраняем запрос в ожидании ответа
                    pendingLyricsRequests[chatId] = musicName;

                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: $"🎵 Вы выбрали: {musicName}\n\n" +
                              "Хотите получить текст песни вместе с аудио?",
                        replyMarkup: LyricsConfirmationKeyboard,
                        cancellationToken: cancellationToken);
                }
                else
                {
                    // Для треков без текста сразу отправляем музыку
                    await SendMusic(botClient, chatId, musicName, false, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при запросе текста песни: {ex.Message}");
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "❌ Произошла ошибка при обработке запроса.",
                    replyMarkup: MusicKeyboard,
                    cancellationToken: cancellationToken);
            }
        }

        // Обработка ответа о тексте песни
        private static async Task HandleLyricsResponse(ITelegramBotClient botClient, long chatId, string response, CancellationToken cancellationToken)
        {
            try
            {
                if (!pendingLyricsRequests.ContainsKey(chatId))
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "❌ Неактивный запрос. Выберите песню заново.",
                        replyMarkup: MusicKeyboard,
                        cancellationToken: cancellationToken);
                    return;
                }

                var musicName = pendingLyricsRequests[chatId];
                pendingLyricsRequests.Remove(chatId);

                bool sendLyrics = response.ToLower() switch
                {
                    "да" or "yes" or "✅ да" or "/lyrics_yes" => true,
                    "нет" or "no" or "❌ нет" or "/lyrics_no" => false,
                    _ => false
                };

                await SendMusic(botClient, chatId, musicName, sendLyrics, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при обработке ответа о тексте: {ex.Message}");
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "❌ Произошла ошибка при обработке ответа.",
                    replyMarkup: MusicKeyboard,
                    cancellationToken: cancellationToken);
            }
        }

        // Отправка музыки с опциональным текстом
        private static async Task SendMusic(ITelegramBotClient botClient, long chatId, string musicName, bool sendLyrics, CancellationToken cancellationToken)
        {
            try
            {
                if (!MusicFiles.ContainsKey(musicName))
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "❌ Музыкальный трек не найден",
                        replyMarkup: MusicKeyboard,
                        cancellationToken: cancellationToken);
                    return;
                }

                var musicUrl = MusicFiles[musicName];

                Console.WriteLine($"🎵 Отправляем музыку: {musicName} (текст: {sendLyrics})");

                // Показываем сообщение о загрузке
                var loadingMessage = await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"⏳ Загружаем {musicName}...",
                    replyMarkup: MusicKeyboard,
                    cancellationToken: cancellationToken);

                // Отправляем аудио
                await botClient.SendAudioAsync(
                    chatId: chatId,
                    audio: InputFile.FromUri(musicUrl),
                    caption: "",
                    replyMarkup: MusicKeyboard,
                    cancellationToken: cancellationToken);

                // Если нужно, отправляем текст песни
                if (sendLyrics && SongLyrics.ContainsKey(musicName))
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: SongLyrics[musicName],
                        replyMarkup: MusicKeyboard,
                        cancellationToken: cancellationToken);
                }

                // Удаляем сообщение о загрузке
                await botClient.DeleteMessageAsync(
                    chatId: chatId,
                    messageId: loadingMessage.MessageId,
                    cancellationToken: cancellationToken);

                Console.WriteLine($"✅ Музыка отправлена: {musicName}");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка отправки музыки: {ex.Message}");

                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "❌ Не удалось отправить музыку. Возможно, файл слишком большой или недоступен.",
                    replyMarkup: MusicKeyboard,
                    cancellationToken: cancellationToken);
            }
        }
    }
}
