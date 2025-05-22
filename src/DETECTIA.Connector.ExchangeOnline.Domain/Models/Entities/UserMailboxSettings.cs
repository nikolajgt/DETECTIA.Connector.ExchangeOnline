using System.ComponentModel.DataAnnotations;

namespace DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;

public record UserMailboxSettings
{
    [Key]
    public ulong                 Id                                    { get; init; }
    public string?               ArchiveFolder                         { get; set; }
    public bool?                 AutomaticRepliesEnabled               { get; set; }
    public string?               AutomaticRepliesInternalMessage       { get; set; }
    public string?               AutomaticRepliesExternalMessage       { get; set; }
    public string?               DateFormat                            { get; set; }
    public string?               TimeFormat                            { get; set; }
    public string?               TimeZone                              { get; set; }
    public IList<string>?        WorkingDays                           { get; set; }
    public TimeSpan?             WorkingHoursStartTime                 { get; set; }
    public TimeSpan?             WorkingHoursEndTime                   { get; set; }
    public string?               DelegateMeetingMessageDeliveryOptions { get; set; }
    public long                UserId                          { get; init; }
}