using System.Text.Json.Serialization;

namespace Project01.Models;

public class CacheEntry
{
    [JsonIgnore]
    public DateTime CreatedAt { get; set; }
}