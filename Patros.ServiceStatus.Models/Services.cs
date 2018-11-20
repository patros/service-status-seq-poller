using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;

namespace Patros.ServiceStatus.Models
{
    public class Services
    {
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public Dictionary<string, Status> Statuses { get; set; } = new Dictionary<string, Status>();
    }
}