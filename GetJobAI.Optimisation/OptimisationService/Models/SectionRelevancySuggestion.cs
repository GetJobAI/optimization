using System.Text.Json.Serialization;

namespace GetJobAI.Optimisation.OptimisationService.Models;

public class SectionRelevancySuggestion
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("entry_id")]
    public Guid EntryId { get; set; }

    [JsonPropertyName("section_type")]
    public string? SectionType { get; set; }

    [JsonPropertyName("include")]
    public bool Include { get; set; } = true;

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("accepted")]
    public bool? Accepted { get; set; }
}