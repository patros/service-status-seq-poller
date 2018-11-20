using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;

namespace Patros.ServiceStatus.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Status
    {
        Online,
        Recovered,
        Warning,
        Failing,
        Offline
    }
}
