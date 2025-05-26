using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;

[Index(nameof(GraphId))]
public record Task
{
    [Key]
    public long                        Id                 { get; init; }
    public required string             GraphId            { get; set; }
    public required string             Title              { get; set; }
    public required string             Status             { get; set; } // notStarted, inProgress, completed, waitingOnOthers, deferred
    public required string             Importance         { get; set; } // low, normal, high
    public DateTimeOffset?             CreatedAt          { get; set; }
    public required DateTimeOffset?    DueDateAt          { get; set; }
    public required DateTimeOffset?    CompletedAt        { get; set; }
    public required string             BodyContent        { get; set; }
    public required string             BodyContentType    { get; set; } // text, html
    public bool?                       HasAttachments     { get; set; }
}