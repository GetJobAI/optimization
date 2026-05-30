using System.Text.Json.Serialization;

namespace GetJobAI.Optimisation.OptimisationService.Models;

public class BulletSuggestion
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("original")]
    public string Original { get; set; } = string.Empty;

    [JsonPropertyName("rewritten")]
    public string Rewritten { get; set; } = string.Empty;

    [JsonPropertyName("keywords_added")]
    public List<string> KeywordsAdded { get; set; } = [];

    [JsonPropertyName("xyz_applied")]
    public bool XyzApplied { get; set; }

    [JsonPropertyName("accepted")]
    public bool? Accepted { get; set; }
}