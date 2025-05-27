using DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DETECTIA.Connector.ExchangeOnline.Migration;

public class AppDatabaseContext(DbContextOptions<AppDatabaseContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; init; }  
    public DbSet<UserGroup> UserGroups { get; init; }
    
    public DbSet<UserMailFolder> UserMailFolders { get; init; } 
    public DbSet<UserMailboxSettings> UserMailboxSettings { get; init; }
    public DbSet<UserMessage> UserMessages { get; init; }
    public DbSet<UserMessageAttachment> MessageAttachments { get; init; }
    public DbSet<MailMatch> MailMatches { get; init; }
    public DbSet<TaskMatch> TaskMatches { get; init; }
    public DbSet<ExchangeSyncState> SyncStates { get; init; }
    
    public DbSet<CalendarEvent> Events { get; init; }
    public DbSet<EventAttachment> EventAttachments { get; init; }
    public DbSet<EventParticipant> EventParticipants { get; init; }
    public DbSet<EventMatch> EventMatches { get; init; }

    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.HasDefaultSchema("DETECTIA");
        modelBuilder.Entity<UserMailboxSettings>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).ValueGeneratedOnAdd();
            b.HasOne<User>()
                .WithOne(u => u.UserMailboxSettings!)
                .HasForeignKey<UserMailboxSettings>(e => e.UserId);
        });
        
        modelBuilder.Entity<ExchangeSyncState>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).ValueGeneratedOnAdd();
        });
        modelBuilder.Entity<User>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).ValueGeneratedOnAdd();
        });
        modelBuilder.Entity<UserMessage>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).ValueGeneratedOnAdd();
        });
        modelBuilder.Entity<UserMessageAttachment>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).ValueGeneratedOnAdd();
        });
        
        modelBuilder.Entity<UserMailFolder>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).ValueGeneratedOnAdd();
        });
        
        modelBuilder.Entity<UserGroup>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).ValueGeneratedOnAdd();
        });
        
        modelBuilder.Entity<Match>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).ValueGeneratedOnAdd();
        });
        
        // 1) Event → Organizer (one-to-many)
        modelBuilder.Entity<CalendarEvent>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).ValueGeneratedOnAdd();

            b.HasOne(e => e.Organizer)
                .WithMany(u => u.OrganizedEvents)
                .HasForeignKey(e => e.OrganizerId)
                // don’t cascade-delete organizer → events
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 2) EventParticipant → Event (many-to-one)
        modelBuilder.Entity<EventParticipant>(b =>
        {
            b.HasKey(ep => ep.Id);

            b.HasOne(ep => ep.Event)
                .WithMany(e => e.Attendees)
                .HasForeignKey(ep => ep.EventId)
                // it’s fine to delete an event and cascade remove its participants
                .OnDelete(DeleteBehavior.Cascade);

            // 3) EventParticipant → User (many-to-one)
            b.HasOne(ep => ep.User)
                .WithMany(u => u.InvitedEvents)
                .HasForeignKey(ep => ep.UserId)
                // prevent deleting a user from cascading into EventParticipant
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
    
    public override int SaveChanges()
    {
        UpdateLastModified();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateLastModified();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateLastModified()
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is IEntityTracker trackableEntity)
            {
                trackableEntity.LastSyncUtc = DateTimeOffset.UtcNow;
            }
        }
    }
}