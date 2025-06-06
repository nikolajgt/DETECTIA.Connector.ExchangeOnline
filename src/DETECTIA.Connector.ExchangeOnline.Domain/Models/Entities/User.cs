﻿using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;

[Index(nameof(GraphId))]
[Index(nameof(Mail))]
public record User
{
    // 1. Identity
    [Key]
    public long Id                                           { get; init; }  
    public required string GraphId                           { get; set; }

    public bool AccountEnabled                               { get; set; }
    public string? DisplayName                               { get; init; }
    public string? GivenName                                 { get; init; }
    public string? Surname                                   { get; init; }
                                                                    
    // 2. Mail / login                                              
    public string? Mail                                      { get; init; }
    public string? UserPrincipalName                         { get; init; }
    public string? MailNickname                              { get; init; }
                                                                    
    // 3. Organization                                              
    public string? JobTitle                                  { get; init; }
    public string? Department                                { get; init; }
    public string? OfficeLocation                            { get; init; }
                                                                    
    // 4. Contact                                                   
    public string? MobilePhone                               { get; init; }
    public required IList<string> BusinessPhones             { get; init; }
    public required IList<string> OtherMails                 { get; init; }
                                                                    
    // 5. Directory sync & license                                  
    public string? OnPremisesImmutableId                     { get; init; }
    public string? UsageLocation                             { get; init; }
    public string? PreferredLanguage                         { get; init; }
    public string? UserType                                  { get; init; }  
                                                                    
    public DateTimeOffset? CreatedAt                         { get; init; }
    public DateTimeOffset? LastPasswordChangeAt              { get; set; }
    
    public UserMailboxSettings? UserMailboxSettings          { get; init; }
    public List<UserMailFolder>? MailboxFolders              { get; init; }
    public List<UserGroupMembership>? GroupMemberships                 { get; init; }
    public List<EventParticipant>? InvitedEvents             { get; init; }
    public List<CalendarEvent>? OrganizedEvents                      { get; init; }
    public string? FoldersDeltaLink                          { get; set; }
    public string? EventsDeltaLink                           { get; set; }
}