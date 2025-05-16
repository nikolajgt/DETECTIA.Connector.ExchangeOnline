namespace DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;

public record UserMailboxSettings
{
    public ulong                 Id                                    { get; init; }
    public string?               ArchiveFolder                         { get; init; }
    public bool?                 AutomaticRepliesEnabled               { get; init; }
    public string?               AutomaticRepliesInternalMessage       { get; init; }
    public string?               AutomaticRepliesExternalMessage       { get; init; }
    public string?               DateFormat                            { get; init; }
    public string?               TimeFormat                            { get; init; }
    public string?               TimeZone                              { get; init; }
    public IList<string>?        WorkingDays                           { get; init; }
    public TimeSpan?             WorkingHoursStartTime                 { get; init; }
    public TimeSpan?             WorkingHoursEndTime                   { get; init; }
    public string?               DelegateMeetingMessageDeliveryOptions { get; init; }
}