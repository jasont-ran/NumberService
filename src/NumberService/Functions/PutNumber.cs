using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NumberService.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace NumberService.Functions;

public class PutNumber
{
    private static readonly string _clientId = Guid.NewGuid().ToString("N");
    private readonly TelemetryClient _telemetry;
    private readonly Container _container;

    public PutNumber(TelemetryClient telemetry, CosmosClient cosmos)
    {
        _telemetry = telemetry;
        _container = cosmos.GetContainer(
            Environment.GetEnvironmentVariable("CosmosDbDatabaseId"),
            Environment.GetEnvironmentVariable("CosmosDbContainerId"));
    }

    [FunctionName("PutNumber")]
    public async Task<IActionResult> Run(
        ILogger log,
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "numbers/{key:alpha}")] HttpRequest req,
        string key)
    {
        var startTime = DateTime.UtcNow;
        var timer = System.Diagnostics.Stopwatch.StartNew();
        Response<NumberResult> response = null;

        try
        {
            response = await _container.Scripts.ExecuteStoredProcedureAsync<NumberResult>(
                "incrementNumber",
                new PartitionKey(key),
                new[] { key, _clientId });
        }
        finally
        {
            timer.Stop();
            _telemetry.TrackCosmosDependency(
                response,
                $"incrementNumber $key={key}, $_clientId={_clientId}",
                startTime,
                timer.Elapsed);
        }

        var number = response.Resource;
        number.RequestCharge = response.RequestCharge;

        // if query string contains ?diagnostics, return CosmosDiagnostics
        if (req.Query.ContainsKey("diagnostics"))
        {
            try
            {
                number.CosmosDiagnostics = JsonConvert.DeserializeObject<Models.CosmosDiagnostics>(response.Diagnostics.ToString());
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Could not deserialize Diagnostics");
            }
        }

        // As long as sproc is written correctly, this case should never be true.
        if (number.ClientId != _clientId) throw new InvalidOperationException($"Response ClientId \"{number.ClientId}\" does not match ClientId \"{_clientId}\".");

        log.LogInformation($"Number {number.Number} issued to clientId {number.ClientId} with ETag {number.ETag} from key {number.Key}");

        _telemetry.TrackEvent(
            "PutNumber",
            properties: new Dictionary<string, string>
                {
                    { "Number", number.Number.ToString() },
                    { "ClientId", _clientId },
                    { "Key", number.Key },
                    { "ETag", number.ETag }
                },
            metrics: new Dictionary<string, double>
                {
                    { "CosmosRequestCharge", response.RequestCharge }
                });

        return new OkObjectResult(number);
    }



    [FunctionName("PutNumberBatch")]
    public async Task<IActionResult> RunBatch(ILogger log, [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "numbersBatch/{key:alpha}")] HttpRequest req, string key)
    {
        int batchSize;
        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var postRequest = JsonConvert.DeserializeObject<PostRequest>(requestBody);
            batchSize = postRequest.BatchSize;
            if(batchSize <= 0)
            {
                throw new Exception("No batch size");
            }
        }
        catch
        {
            throw new Exception("Error Getting batch size");
        }

        var startTime = DateTime.UtcNow;
        var timer = System.Diagnostics.Stopwatch.StartNew();
        Response<NumberBatchResult> response = null;

        try
        {
            response = await _container.Scripts.ExecuteStoredProcedureAsync<NumberBatchResult>(
                "incrementNumberBatch",
                new PartitionKey(key),
                new object[] { key, _clientId, batchSize });
        }
        finally
        {
            timer.Stop();
            _telemetry.TrackCosmosDependency(
                response,
                $"incrementNumberBatch $key={key}, $_clientId={_clientId}, $batchSize={batchSize}",
                startTime,
                timer.Elapsed);
        }

        var numbers = response.Resource;
        numbers.RequestCharge = response.RequestCharge;

        //if query string contains ?diagnostics, return CosmosDiagnostics
        if (req.Query.ContainsKey("diagnostics"))
        {
            try
            {
                numbers.CosmosDiagnostics = JsonConvert.DeserializeObject<Models.CosmosDiagnostics>(response.Diagnostics.ToString());
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Could not deserialize Diagnostics");
            }
        }

        // As long as sproc is written correctly, this case should never be true.
        if (numbers.ClientId != _clientId) throw new InvalidOperationException($"Response ClientId \"{numbers.ClientId}\" does not match ClientId \"{_clientId}\".");

        log.LogInformation($"Number {numbers.StartNumber} to {numbers.EndNumber} issued to clientId {numbers.ClientId} with ETag {numbers.ETag} from key {numbers.Key}");

        _telemetry.TrackEvent(
            "PutNumber",
            properties: new Dictionary<string, string>
                {
                    { "Numbers", numbers.StartNumber.ToString() + " " + numbers.EndNumber.ToString() },
                    { "ClientId", _clientId },
                    { "Key", numbers.Key },
                    { "ETag", numbers.ETag }
                },
            metrics: new Dictionary<string, double>
                {
                    { "CosmosRequestCharge", response.RequestCharge }
                });

        return new OkObjectResult(numbers);
    }
}
