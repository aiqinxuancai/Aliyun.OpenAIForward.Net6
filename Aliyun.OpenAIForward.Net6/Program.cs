using Microsoft.AspNetCore.Http.Extensions;
using System.Net.Http.Headers;
using System.Text.Json;


namespace Aliyun.OpenAIForward.Net6
{
    public class Program
    {
        // 定义路由映射配置
        private static Dictionary<string, string> RouteMapping = new()
        { 
            { "default", "https://api.openai.com/" },
            { "openai", "https://api.openai.com/" },
            { "openrouter", "https://openrouter.ai/api/" },
            { "google", "https://generativelanguage.googleapis.com/" }
        };

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            //支持自定义路由映射，从环境变量中读取json格式并添加到RouteMapping
            var routeMapping = Environment.GetEnvironmentVariable("ROUTE_MAPPING");
            if (!string.IsNullOrEmpty(routeMapping))
            {
                var routeMappingDict = JsonSerializer.Deserialize<Dictionary<string, string>>(routeMapping);
                if (routeMappingDict != null)
                {
                    foreach (var item in routeMappingDict)
                    {
                        RouteMapping[item.Key] = item.Value;
                    }
                }

            }

            foreach (var route in RouteMapping)
            {
                builder.Services.AddHttpClient(route.Key, client =>
                {
                    client.BaseAddress = new Uri(route.Value);
                    client.Timeout = TimeSpan.FromMinutes(16);
                });
            }

            var app = builder.Build();

            app.MapGet("/", () => "Hello World!");

            app.MapMethods("{**path}", new[] { "GET", "POST", "PUT", "DELETE" }, async (
                HttpContext context,
                IHttpClientFactory clientFactory) =>
            {
                try
                {
                    //Console.WriteLine("----收到请求----");
                    //Console.WriteLine(context.Request.GetDisplayUrl());
                    //Console.WriteLine(context.Request.Path.Value);

                    var (clientName, adjustedPath) = GetClientNameAndPath(context.Request.Path.Value);

                    //Console.WriteLine(adjustedPath);

                    var client = clientFactory.CreateClient(clientName);
                    var requestMessage = CreateProxyHttpRequest(context, adjustedPath);

                    var response = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
                    await CopyProxyHttpResponse(context, response);
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsJsonAsync(new { error = ex.Message });
                }
            });

            app.Run();
        }

        private static (string clientName, string adjustedPath) GetClientNameAndPath(string? path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return ("default", string.Empty);
            }

            path = path.TrimStart('/');

            foreach (var route in RouteMapping)
            {
                if (path.StartsWith(route.Key + "/", StringComparison.OrdinalIgnoreCase))
                {
                    var adjustedPath = path[(route.Key.Length + 1)..];
                    return (route.Key, adjustedPath);
                }
            }

            // 默认的OpenAI
            return ("default", path);
        }

        private static HttpRequestMessage CreateProxyHttpRequest(HttpContext context, string adjustedPath)
        {
            var requestMessage = new HttpRequestMessage();
            requestMessage.Method = new HttpMethod(context.Request.Method);

            requestMessage.RequestUri = new Uri($"{adjustedPath}", UriKind.Relative);

            if (context.Request.ContentLength > 0)
            {
                requestMessage.Content = new StreamContent(context.Request.Body);
            }

            foreach (var header in context.Request.Headers)
            {
                if (!header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) &&
                    !header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                {
                    if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                    {
                        if (requestMessage.Content != null)
                        {
                            requestMessage.Content.Headers.ContentType =
                                MediaTypeHeaderValue.Parse(header.Value.ToString());
                        }
                    }
                    else
                    {
                        requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                    }
                }
            }

            return requestMessage;
        }

        private static async Task CopyProxyHttpResponse(HttpContext context, HttpResponseMessage response)
        {
            context.Response.StatusCode = (int)response.StatusCode;

            foreach (var header in response.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            foreach (var header in response.Content.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            context.Response.Headers.Remove("transfer-encoding");

            await response.Content.CopyToAsync(context.Response.Body);
        }
    }

}
