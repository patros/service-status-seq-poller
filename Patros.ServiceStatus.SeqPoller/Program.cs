﻿using System;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Patros.AuthenticatedHttpClient;
using Patros.ServiceStatus.Models;

namespace Patros.ServiceStatus.SeqPoller
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json");

            builder.AddJsonFile("appsettings.local.json", optional: true);

            var configuration = builder.Build();

            var httpClient = QueryStringParameterAuthenticatedHttpClient.GetClient(
                new QueryStringParameterAuthenticatedHttpClientOptions
                {
                    Name = "apiKey",
                    Value = configuration["seqApiKey"]
                }
            );

            var seqRepoOptions = new SeqRepoOptions();
            configuration.Bind("seqRepoOptions", seqRepoOptions);
            var seqRepo = new SeqRepo(seqRepoOptions, httpClient);
            
            var scratchPad = new Services
            {
                LastUpdated = DateTime.UtcNow
            };

            var currentServiceStatuses = new Services();
            try
            {
                var currentJson = File.ReadAllText(configuration["serviceStatusFilePath"]);
                currentServiceStatuses = JsonConvert.DeserializeObject<Services>(currentJson);
            }
            catch (FileNotFoundException)
            {
                // nothing to see here, move along
            }

            foreach (var service in currentServiceStatuses.Statuses.Keys)
            {
                scratchPad.Statuses[service] = Status.Offline;
            }

            // note the items returned below can also be in the currentFailures
            // and other lists so the ordering of setting service states is important
            var allRecentServices = await seqRepo.GetAllServices();
            foreach (var service in allRecentServices)
            {
                scratchPad.Statuses[service] = Status.Offline;
            }

            var currentNonFailures = await seqRepo.GetServicesWithCurrentNonFailureEvents();
            foreach (var service in currentNonFailures)
            {
                scratchPad.Statuses[service] = Status.Online;
            }

            var previousFailures = await seqRepo.GetServicesWithPreviousFailures();
            foreach (var service in previousFailures)
            {
                scratchPad.Statuses[service] = Status.Recovered;
            }

            var currentWarnings = await seqRepo.GetServicesWithCurrentWarnings();
            foreach (var service in currentWarnings)
            {
                scratchPad.Statuses[service] = Status.Warning;
            }

            var currentFailures = await seqRepo.GetServicesWithCurrentFailures();
            foreach (var service in currentFailures)
            {
                scratchPad.Statuses[service] = Status.Failing;
            }

            var newJson = JsonConvert.SerializeObject(scratchPad, Formatting.Indented);
            File.WriteAllText(configuration["serviceStatusFilePath"], newJson);
        }
    }
}
