using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace SysProg3
{
    public class WebServer
    {
        private string baseUrl;
        private int port;
        private GitHubSearchService service;

        public WebServer(string baseUrl, int port, int cacheCapacity)
        {
            this.baseUrl = baseUrl;
            this.port = port;
            service = new GitHubSearchService(cacheCapacity);
        }

        public async Task Launch()
        {
            string address = $"{baseUrl}:{port}/";
            using (var listener = new HttpListener())
            {
                listener.Prefixes.Add(address);
                listener.Start();
                Console.WriteLine($"Listening on {address}...");
                while (listener.IsListening)
                {
                    var context = await listener.GetContextAsync();
                    Task.Run(() => HandleRequest(context));
                }
            }
        }
        private async void HandleRequest(HttpListenerContext context)
        {
            if (context.Request.HttpMethod == "GET")
            {
                var language = context.Request.RawUrl;
                if (language == "/")
                {
                    var buffer = Encoding.UTF8.GetBytes("Language parameter is required.");
                    SendResponse(context, buffer, "text/html", HttpStatusCode.BadRequest);
                }
                else
                {
                    try
                    {
                        language = language.TrimStart('/');
                        var repositoryStream = service.GetRepositories(language);
                        var tcs = new TaskCompletionSource<List<RepositoryInfo>>();

                        var subscription = repositoryStream.Subscribe(
                             repos => tcs.TrySetResult(repos),
                             error => tcs.TrySetException(error)
                            );
                        var result = await tcs.Task;

                        var json = JsonConvert.SerializeObject(result, Formatting.Indented);
                        var buffer = Encoding.UTF8.GetBytes(json);
                        SendResponse(context, buffer, "text/html");
                    }
                    catch (Exception ex)
                    {
                        if (context.Response.OutputStream.CanWrite)
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                            context.Response.OutputStream.Close();
                        }
                        Console.WriteLine(ex.Message);
                    }
                }
            }
            else
            {
                return;
            }
        }
        private async void SendResponse(HttpListenerContext context, byte[] responseBody, string contentType, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            string logString = string.Format(
                "REQUEST:\n{0} {1} HTTP/{2}\nHost: {3}\nUser-agent: {4}\n-------------------\nRESPONSE:\nStatus: {5}\nDate: {6}\nContent-Type: {7}\nContent-Length: {8}\n",
                context.Request.HttpMethod,
                context.Request.RawUrl,
                context.Request.ProtocolVersion,
                context.Request.UserHostName,
                context.Request.UserAgent,
                statusCode,
                DateTime.Now,
                contentType,
                responseBody.Length
            );
            context.Response.ContentType = contentType;
            context.Response.StatusCode = (int)statusCode;
            context.Response.ContentLength64 = responseBody.Length;

            using (Stream outputStream = context.Response.OutputStream)
            {
                await outputStream.WriteAsync(responseBody, 0, responseBody.Length);
            }
            Console.WriteLine(logString);
        }
    }
}
