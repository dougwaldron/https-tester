using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Spectre.Console;

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
                AnsiConsole.MarkupLine("[red]Provide a list of websites in the appconfig.json file.[/]");
                return;
            }

            // Create HttpClient
            var handler = new HttpClientHandler() { AllowAutoRedirect = false };
            var client = new HttpClient(handler);

            // Output results to:
            // - StringBuilder for file
            // - Spectre Console for console

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("# Testing Website Protocol Headers");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Testing Website Protocol Headers[/]");

            sb.AppendLine();
            AnsiConsole.WriteLine();

            sb.AppendLine("Valid Status Code is \"Moved\".");
            AnsiConsole.WriteLine("Valid Status Code is \"Moved\".");

            sb.AppendLine();
            AnsiConsole.WriteLine();

            sb.AppendLine("| URL Tested             | HTTP Status Code  | HSTS Value |");
            sb.AppendLine("| ---------------------- | ----------------- | ---------- |");

            var table = new Table().Border(TableBorder.MinimalHeavyHead).Collapse();
            table.AddColumn("URL Tested");
            table.AddColumn("HTTP Status Code");
            table.AddColumn("HSTS Value");

            await AnsiConsole.Live(table).StartAsync(async c =>
            {
                // Test each site
                foreach (string site in sites)
                {
                    try
                    {
                        var httpStatusCode = await client.GetHttpStatusCodeAsync(site);
                        var hstsValue = await client.GetHstsValueAsync(site);

                        sb.AppendLine($"| {site.PadRight(22)} | {httpStatusCode.Item1.PadRight(17)} | {hstsValue.PadLeft(10)} |");
                        table.AddRow(site, httpStatusCode.Item2, hstsValue);
                    }
                    catch (HttpRequestException)
                    {
                        sb.AppendLine($"| {site.PadRight(22)} | {"Connection error".PadRight(17)} | {"N/A".PadLeft(10)} |");
                        table.AddRow(site, "[red]Connection error[/]", "N/A");
                    }

                    c.Refresh();
                }
            });

            // Write results to file
            AnsiConsole.Markup(Emoji.Known.FloppyDisk + " Writing results to \"results.md\" ...");
            using var fs = new FileStream("results.md", FileMode.Create);
            using var sw = new StreamWriter(fs);
            sw.Write(sb.ToString());
            sw.Close();

            AnsiConsole.WriteLine(" Finished.");
        }

        // First string is for Markdown file; second for Spectre Console.
        private static async Task<(string, string)> GetHttpStatusCodeAsync(this HttpClient client, string site)
        {
            var response = await client.GetHttpHeadAsync($"http://{site}");

            if ((int)response.StatusCode == 301)
            {
                if (response.Headers.Location.ToString() == $"https://{site}/")
                {
                    return (
                        $"Valid response: \"{response.StatusCode}\"",
                        $"Valid response: \"{response.StatusCode}\"");
                }

                return (
                    $"Invalid redirect: \"{response.Headers.Location}\"",
                    $"[red]Invalid redirect: \"{response.Headers.Location}\"[/]");
            }

            return (
                $"Invalid response: \"{response.StatusCode}\"",
                $"[red]Invalid response: \"{response.StatusCode}\"[/]");
        }

        private static async Task<string> GetHstsValueAsync(this HttpClient client, string site)
        {
            var headers = (await client.GetHttpHeadAsync($"https://{site}")).Headers;

            IEnumerable<string> values;

            if (headers.TryGetValues("Strict-Transport-Security", out values))
            {
                return values.First().Replace("max-age=", string.Empty);
            }

            return "[red]None[/]";
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
