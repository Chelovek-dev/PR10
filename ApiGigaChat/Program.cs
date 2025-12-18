using ApiGigaChat.Models;
using ApiGigaChat.Models.Response;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ApiGigaChat
{
    internal class Program
    {
        static string ClientId = "019b2840-ca25-7a46-92f5-37031757eeba";
        static string AuthorizationKey = "MDE5YjI4NDAtY2EyNS03YTQ2LTkyZjUtMzcwMzE3NTdlZWJhOmUwMzNjNDI4LTAwM2UtNGRiMC04YjY3LTY1YTIxYmY3MzE4Nw==";

        static string YandexFolderId = "ыы";
        static string YandexApiKey = "ыы-ыы";

        static List<dynamic> chatHistory = new List<dynamic>();
        static string currentModel = "GigaChat"; 

        public static async Task<string> GetToken(string rqUID, string bearer)
        {
            string ReturnToken = null;
            string Url = "https://ngw.devices.sberbank.ru:9443/api/v2/oauth";

            using (HttpClientHandler Handler = new HttpClientHandler())
            {
                Handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

                using (HttpClient Clien = new HttpClient(Handler))
                {
                    HttpRequestMessage Request = new HttpRequestMessage(HttpMethod.Post, Url);

                    Request.Headers.Add("Accept", "application/json");
                    Request.Headers.Add("RqUID", rqUID);
                    Request.Headers.Add("Authorization", $"Bearer {bearer}");

                    var Data = new List<KeyValuePair<string, string>> {
                        new KeyValuePair<string, string>("scope", "GIGACHAT_API_PERS")
                    };

                    Request.Content = new FormUrlEncodedContent(Data);

                    HttpResponseMessage Response = await Clien.SendAsync(Request);

                    if (Response.IsSuccessStatusCode)
                    {
                        string ResponseContent = await Response.Content.ReadAsStringAsync();
                        ResponseToken Token = JsonConvert.DeserializeObject<ResponseToken>(ResponseContent);
                        ReturnToken = Token.access_token;
                    }
                    else
                    {
                        Console.WriteLine($"Ошибка при получении токена: {Response.StatusCode}");
                    }
                }
            }

            return ReturnToken;
        }

        static async Task Main(string[] args)
        {
            string Token = await GetToken(ClientId, AuthorizationKey);

            if (Token == null)
            {
                Console.WriteLine("Не удалось получить токен GigaChat");
                return;
            }

            Console.WriteLine("Доступные команды:");
            Console.WriteLine("- 'модель' - выбрать модель (GigaChat/YandexGPT)");
            Console.WriteLine("- 'очистить' - очистить историю диалога");

            Console.WriteLine($"Текущая модель: {currentModel}");

            while (true)
            {
                Console.Write("Сообщение: ");
                string Message = Console.ReadLine();

                
                if (Message.ToLower() == "очистить")
                {
                    chatHistory.Clear();
                    Console.WriteLine("История очищена");
                    continue;
                }
                else if (Message.ToLower() == "модель")
                {
                    Console.Write("Выберите модель (1 - GigaChat, 2 - YandexGPT): ");
                    string choice = Console.ReadLine();
                    if (choice == "1")
                    {
                        currentModel = "GigaChat";
                        Console.WriteLine("Выбрана модель: GigaChat");
                    }
                    else if (choice == "2")
                    {
                        currentModel = "YandexGPT";
                        Console.WriteLine("Выбрана модель: YandexGPT");
                    }
                    else
                    {
                        Console.WriteLine("Неверный выбор, оставляем текущую модель");
                    }
                    continue;
                }

                ResponseMessage Answer = null;

                if (currentModel == "GigaChat")
                {
                    Answer = await GetAnswer(Token, Message);
                }
                else if (currentModel == "YandexGPT")
                {
                    Answer = await GetAnswerFromYandexGPT(Message);
                }

                if (Answer != null && Answer.choices != null && Answer.choices.Count > 0)
                {
                    string responseText = Answer.choices[0].message.content;
                    Console.WriteLine("Ответ: " + responseText);

                    chatHistory.Add(new { role = "user", content = Message });
                    chatHistory.Add(new { role = "assistant", content = responseText });
                }
                else
                {
                    Console.WriteLine("Не удалось получить ответ");
                }
            }
        }

        public static async Task<ResponseMessage> GetAnswer(string token, string message)
        {
            ResponseMessage responseMessage = null;
            string Url = "https://gigachat.devices.sberbank.ru/api/v1/chat/completions";

            using (HttpClientHandler Handler = new HttpClientHandler())
            {
                Handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

                using (HttpClient Client = new HttpClient(Handler))
                {
                    HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, Url);

                    httpRequestMessage.Headers.Add("Accept", "application/json");
                    httpRequestMessage.Headers.Add("Authorization", $"Bearer {token}");

                    var messagesArray = new List<object>();

                    foreach (var msg in chatHistory)
                    {
                        messagesArray.Add(msg);
                    }

                    messagesArray.Add(new { role = "user", content = message });

                    var DataRequest = new
                    {
                        model = "GigaChat",
                        stream = false,
                        repetition_penalty = 1,
                        messages = messagesArray.ToArray()
                    };

                    string JsonContent = JsonConvert.SerializeObject(DataRequest);
                    httpRequestMessage.Content = new StringContent(JsonContent, Encoding.UTF8, "application/json");

                    HttpResponseMessage Response = await Client.SendAsync(httpRequestMessage);

                    if (Response.IsSuccessStatusCode)
                    {
                        string ResponseContent = await Response.Content.ReadAsStringAsync();
                        responseMessage = JsonConvert.DeserializeObject<ResponseMessage>(ResponseContent);
                    }
                    else
                    {
                        Console.WriteLine($"Ошибка при получении ответа: {Response.StatusCode}");
                    }
                }
            }

            return responseMessage;
        }

        public static async Task<ResponseMessage> GetAnswerFromYandexGPT(string message)
        {
            ResponseMessage responseMessage = new ResponseMessage();
            string Url = "https://llm.api.cloud.yandex.net/foundationModels/v1/completion";

            using (HttpClient Client = new HttpClient())
            {
                HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, Url);

                httpRequestMessage.Headers.Add("Authorization", $"Api-Key {YandexApiKey}");
                httpRequestMessage.Headers.Add("x-folder-id", YandexFolderId);

                var messagesArray = new List<object>();

                foreach (var msg in chatHistory)
                {
                    messagesArray.Add(new
                    {
                        role = msg.role,
                        text = msg.content
                    });
                }

                messagesArray.Add(new
                {
                    role = "user",
                    text = message
                });

                var requestBody = new
                {
                    modelUri = $"gpt://{YandexFolderId}/yandexgpt/latest",
                    completionOptions = new
                    {
                        stream = false,
                        temperature = 0.6,
                        maxTokens = "2000"
                    },
                    messages = messagesArray
                };

                string JsonContent = JsonConvert.SerializeObject(requestBody);
                httpRequestMessage.Content = new StringContent(JsonContent, Encoding.UTF8, "application/json");

                HttpResponseMessage Response = await Client.SendAsync(httpRequestMessage);

                if (Response.IsSuccessStatusCode)
                {
                    string ResponseContent = await Response.Content.ReadAsStringAsync();

                    try
                    {
                        var yandexResponse = JsonConvert.DeserializeObject<YandexGPTResponse>(ResponseContent);

                        if (yandexResponse != null && yandexResponse.result != null &&
                            yandexResponse.result.alternatives != null && yandexResponse.result.alternatives.Count > 0)
                        {
                            responseMessage.choices = new List<Choice>
                            {
                                new Choice
                                {
                                    message = new Request.Message
                                    {
                                        role = "assistant",
                                        content = yandexResponse.result.alternatives[0].message.text
                                    },
                                    index = 0,
                                    finish_reason = "stop"
                                }
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка парсинга ответа YandexGPT: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"Ошибка YandexGPT: {Response.StatusCode}");
                    Console.WriteLine(await Response.Content.ReadAsStringAsync());
                }
            }

            return responseMessage;
        }
    }
}