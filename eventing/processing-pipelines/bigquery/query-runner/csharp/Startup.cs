// Copyright 2020 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using System.IO;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using CloudNative.CloudEvents;
using Common;
using Google.Cloud.BigQuery.V2;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace QueryRunner
{
    public class Startup
    {
        private const string CloudEventType = "dev.knative.samples.querycompleted";
        private const string CloudEventSource = "knative/eventing/samples/queryrunner";

        private const string DatasetId = "covid19_jhu_csse";
        private string _tableId;

        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            logger.LogInformation("Service is starting...");

            app.UseRouting();

            var configReader = new ConfigReader(logger);
            var eventReaderType = configReader.ReadEventReaderType();
            var projectId = configReader.ReadProjectId();
            IEventWriter eventWriter = configReader.ReadEventWriter(CloudEventSource, CloudEventType);

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapPost("/", async context =>
                {
                    var client = await BigQueryClient.CreateAsync(projectId);

                    var country = await ReadCountry(eventReaderType, context, logger);

                    _tableId = country.Replace(" ", "").ToLowerInvariant();

                    var results = await RunQuery(client, country, logger);
                    logger.LogInformation("Executed query");

                    var replyData = JsonConvert.SerializeObject(new {datasetId = DatasetId, tableId = _tableId, country = country});
                    await eventWriter.Write(replyData, context);
                });
            });
        }

        private async Task<string> ReadCountry(EventReaderType eventReaderType, HttpContext context, ILogger logger)
        {
            if (eventReaderType == EventReaderType.HttpPost)
            {
                using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8))
                {
                    return await reader.ReadToEndAsync();
                }
            }

            var eventReader = new CloudEventReader(logger);
            var cloudEvent = await eventReader.Read(context);
            return (string)cloudEvent.Data;
        }

        private async Task<BigQueryTable> GetOrCreateTable(BigQueryClient client)
        {
            var dataset = await client.GetOrCreateDatasetAsync(DatasetId);
            try
            {
                await client.DeleteTableAsync(DatasetId, _tableId); // Start fresh each time
            }
            catch (Exception)
            {
                // Ignore. The table probably did not exist.
            }
            var table = await dataset.CreateTableAsync(_tableId, new TableSchemaBuilder
            {
                { "date", BigQueryDbType.Date },
                { "num_reports", BigQueryDbType.Int64 },
            }.Build());

            return table;
        }

        private async Task<BigQueryResults> RunQuery(BigQueryClient client, string country, ILogger<Startup> logger)
        {
            var sql = $@"SELECT date, SUM(confirmed) num_reports
                FROM `bigquery-public-data.covid19_jhu_csse.summary`
                WHERE country_region = '{country}'
                GROUP BY date
                ORDER BY date ASC";

            logger.LogInformation($"Executing query: \n{sql}");

             var table = await GetOrCreateTable(client);
             return await client.ExecuteQueryAsync(sql, null, new QueryOptions {
                 DestinationTable = table.Reference
             });

        }
    }
}
