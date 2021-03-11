using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace httpsTest
{
    static class Program
    {
        static async Task Main(string[] args)
        {
            // Get sites list
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            var sites = config.GetSection("sites").Get<List<string>>();

            if (sites is null || sites.Count() == 0)
            {
                Console.WriteLine("Provide a list of websites in the appconfig.json file.");
                return;
            }

            // Create HttpClient
            var handler = new HttpClientHandler() { AllowAutoRedirect = false };
            var client = new HttpClient(handler);

            // Output results
            Console.WriteLine("# Website protocol headers");
            Console.WriteLine();

            Console.WriteLine("| URL                    | HTTP Status Code  | HSTS Value |");
            Console.WriteLine("| ---------------------- | ----------------- | ---------: |");

            foreach (string site in sites)
            {
                try
                {
                    var httpStatusCode = await client.GetHttpStatusCodeAsync(site);
                    var hstsValue = await client.GetHstsValueAsync(site);

                    Console.WriteLine($"| {site.PadRight(22)} | {httpStatusCode.PadRight(17)} | {hstsValue.PadLeft(10)} |");
                }
                catch (HttpRequestException)
                {
                    Console.WriteLine($"| {site.PadRight(22)} | {"Connection error".PadRight(17)} | {"N/A".PadLeft(10)} |");
                }
            }
        }

        private static async Task<string> GetHttpStatusCodeAsync(this HttpClient client, string site)
        {
            var response = await client.GetHttpHeadAsync($"http://{site}");

            if ((int)response.StatusCode == 301)
            {
                if (response.Headers.Location.ToString() == $"https://{site}/")
                {
                    return $"Valid: {response.StatusCode}";
                }

                return $"Invalid redirect: {response.Headers.Location}";
            }

            return $"Invalid response: {response.StatusCode}";
        }

        private static async Task<string> GetHstsValueAsync(this HttpClient client, string site)
        {
            var headers = (await client.GetHttpHeadAsync($"https://{site}")).Headers;

            IEnumerable<string> values;

            if (headers.TryGetValues("Strict-Transport-Security", out values))
            {
                return values.First().Replace("max-age=", string.Empty);
            }

            return "None";
        }

        private static async Task<HttpResponseMessage> GetHttpHeadAsync(this HttpClient client, string url)
        {
            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            if (url.IndexOf(':') < 0)
            {
                throw new ArgumentException("URL must have protocol specified.", nameof(url));
            }

            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                throw new ArgumentException("URL is not well formed.", nameof(url));
            }

            return await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));
        }
    }
}
