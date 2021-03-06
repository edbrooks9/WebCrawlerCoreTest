﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace WebCrawlerCore
{
    public class LinkChecker
    {
        private static readonly ILogger<LinkChecker> Logger = Logs.Factory.CreateLogger<LinkChecker>();

        public static IEnumerable<string> GetLinks(string link, string page)
        {
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(page);
            var originalLinks = htmlDocument.DocumentNode.SelectNodes("//a[@href]")
                .Select(a => a.GetAttributeValue("href", string.Empty))
                .ToList();

            using (Logger.BeginScope($"Getting links from {link}"))
            {
               originalLinks.ForEach(l => Logger.LogTrace(100, "Original link: {link}", l));
            }
            
            var links = originalLinks
                .Where(l => !string.IsNullOrEmpty(l))
                .Where(l => l.StartsWith("http"));
            return links;
        }

        public static IEnumerable<LinkCheckResult> CheckLinks(IEnumerable<string> links)
        {
            var all = Task.WhenAll(links.Select(CheckLink));
            return all.Result;
        }

        private static async Task<LinkCheckResult> CheckLink(string link)
        {
            var result = new LinkCheckResult();
            result.Link = link;

            using var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Head, link);
            try
            {
                var response = await client.SendAsync(request);
                result.Problem = response.IsSuccessStatusCode
                    ? null
                    : response.StatusCode.ToString();
                return result;
            }
            catch (HttpRequestException exception)
            {
                Logger.LogTrace(0, exception, "Failed to retrieve {link}", link);
                result.Problem = exception.Message;
                return result;
            }
        }
    }

    public class LinkCheckResult
    {
        public int Id { get; set; }
        public bool Exists => String.IsNullOrWhiteSpace(Problem);
        public bool IsMissing => !Exists;
        public string Problem { get; set; }
        public string Link { get; set; }
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    }
}
