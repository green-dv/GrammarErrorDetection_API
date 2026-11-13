using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using GrammarErrorDetection.Models.Grammar.Validation;

namespace GrammarErrorDetection.Models.Grammar.Gamification;

public sealed class ChallengeList
{
    [NotEmptyGuid]
    public Guid? Id { get; set; }

    [NotEmptyGuid]
    public Guid? CreatedByUserId { get; set; }

    [StringLength(150)]
    public string? Title { get; set; }

    public string? Description { get; set; }

    public string? Instructions { get; set; }

    [StringLength(20)]
    public string? Difficulty { get; set; }

    [Range(0, int.MaxValue)]
    public int? Points { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    [StringLength(20)]
    public string? Status { get; set; }

    public bool? Active { get; set; }

    public DateOnly? CreatedAt { get; set; }
}
