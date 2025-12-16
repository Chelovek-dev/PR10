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

        static List<dynamic> chatHistory = new List<dynamic>();
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
                }
            }

            return ReturnToken;
        }

        static async Task Main(string[] args)
        {
            string Token = await GetToken(ClientId, AuthorizationKey);

            if (Token == null)
            {
                Console.WriteLine("Не удалось получить токен");
                return;
            }

            while (true)
            {
                Console.Write("Сообщение: ");
                string Message = Console.ReadLine();
                ResponseMessage Answer = await GetAnswer(Token, Message);
                Console.WriteLine("Ответ: " + Answer.choices[0].message.content);

                chatHistory.Add(new { role = "user", content = Message });
                chatHistory.Add(new { role = "assistant", content = Answer.choices[0].message.content });
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

                    var messagesList = new List<object>();
                    foreach (var msg in chatHistory)
                    {
                        messagesList.Add(msg);
                    }
                    messagesList.Add(new { role = "user", content = message });

                    var DataRequest = new
                    {
                        model = "GigaChat",
                        stream = false,
                        repetition_penalty = 1,
                        messages = messagesList
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
    }
}