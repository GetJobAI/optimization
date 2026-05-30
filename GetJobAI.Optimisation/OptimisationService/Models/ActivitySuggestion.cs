using System.Text.Json.Serialization;

namespace GetJobAI.Optimisation.OptimisationService.Models;

public class ActivitySuggestion
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("entry_id")]
    public Guid EntryId { get; set; }

    [JsonPropertyName("include")]
    public bool Include { get; set; } = true;

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("highlights_rewritten")]
    public List<string> HighlightsRewritten { get; set; } = [];

    [JsonPropertyName("accepted")]
    public bool? Accepted { get; set; }

    [JsonPropertyName("rejection_hint")]
    public string? RejectionHint { get; set; }

    [JsonPropertyName("rewrite_count")]
    public int? RewriteCount { get; set; } = 0;
}
