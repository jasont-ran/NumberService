using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NumberService.Models;

public class NumberBatchResult
{
    [JsonProperty("id")]
    public string Key { get; set; }

    public long StartNumber { get; set; }
    public long EndNumber { get; set; }

    public string ClientId { get; set; }

    [JsonProperty("_etag")]
    public string ETag { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public double? RequestCharge { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public CosmosDiagnostics CosmosDiagnostics { get; set; }
}
