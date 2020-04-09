using System;
using System.IO;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Fitness.v1;
using Google.Apis.Fitness.v1.Data;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;
using System.Threading;
using Google.Apis.Util.Store;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Weighter.Google;
using Weighter;
using System.Linq;
using System.Globalization;

namespace weighter
{
    class Program
    {
        static void Main(string[] args)
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            var serviceProvider = services.BuildServiceProvider();
            var app = serviceProvider.GetService<Application>();
            Task.Run(() => app.Run()).Wait();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services
                .AddLogging(opt =>
                {
                    opt.AddConsole();
                })
                .AddTransient<Application>()
                .AddSingleton<IConfig>(new Config())
                .AddSingleton<GoogleAuth>()
                .AddTransient<GoogleSheets>()
                .AddTransient<GoogleFitness>();
        }

        class Application
        {
            private readonly ILogger<Application> logger;
            private readonly IConfig config;
            private readonly GoogleFitness googleFitness;
            private readonly GoogleSheets googleSheets;

            public Application(ILogger<Application> logger, IConfig config, GoogleFitness googleFitness, GoogleSheets googleSheets)
            {
                logger.LogInformation("Starting weighter...");
                this.logger = logger;
                this.config = config;
                this.googleFitness = googleFitness;
                this.googleSheets = googleSheets;
            }
            public async Task Run()
            {
                // Possibly need to bust the cache on fitness API? Look into this later
                Random r = new Random();
                var fuzz = r.Next(600);
                DateTime from = DateTime.Now.AddDays(-7).AddSeconds(fuzz);
                DateTime to = DateTime.Now;
                var weightResult = await googleFitness.GetWeightsBetween(from, to);

                var last = weightResult.Last();
                var lastDate = new DateTimeOffset(last.DateTime).ToLocalTime();
                logger.LogInformation($"Fetched {weightResult.Count()} weights on Google Fit, last one {last.Weight} on {lastDate}");
                var weightToPost = GetWeightToPost(weightResult);
                logger.LogInformation($"Posting {weightToPost} weight");

                var dateColumnValues = await googleSheets.GetValuesFromColumn(
                    config.WeighInSpreadsheetId,
                    config.WeighInSheetName, 
                    config.WeighInSheetDateColumn
                );

                var rowToPost = GetRowToPost(dateColumnValues);

                await googleSheets.PostValueToCell(
                    weightToPost, 
                    config.WeighInSpreadsheetId,
                    config.WeighInSheetName,
                    config.WeighInSheetEntryColumn,
                    rowToPost    
                );
            }

            private int GetRowToPost(IList<string> dateColumnValues)
            {
                var now = DateTime.Now;
                var dates = dateColumnValues.Select(DateStringToDateTime).ToList();

                var firstFutureDate = dates.First(date => date.Date >= now.Date);
                int dateIndex = dates.IndexOf(firstFutureDate) + 1;
                logger.LogInformation($"Posting to date {firstFutureDate} on row {dateIndex}");
                return dateIndex;
            }

            private DateTime DateStringToDateTime(string dateString)
            {
                DateTime result = default(DateTime);
                var success = DateTime.TryParseExact(dateString, "dd-MMM", CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
                return result;
            }

            private static double GetWeightToPost(IEnumerable<WeightValue> weightResult)
            {
                // TODO maybe a weighted average for the week or something?
                return weightResult.Select(w => w.Weight).Average();
            }
        }
    }
}
