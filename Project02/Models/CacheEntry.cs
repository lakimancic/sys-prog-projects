using System.Text.Json.Serialization;

namespace Project02.Models;

public class CacheEntry
{
    [JsonIgnore]
    public DateTime CreatedAt { get; set; }
}