using DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DETECTIA.Connector.ExchangeOnline.Migration;

public class AppDatabaseContext(DbContextOptions<AppDatabaseContext> options) : DbContext(options)
{
    // Users
    public DbSet<User> Users { get; init; }  
    public DbSet<UserGroup> UserGroups { get; init; }
    
    // Mailbox
    public DbSet<UserMailFolder> UserMailFolders { get; init; } 
    public DbSet<UserMailboxSettings> UserMailboxSettings { get; init; }
    public DbSet<UserMessage> UserMessages { get; init; }
    public DbSet<MessageAttachment> MessageAttachments { get; init; }
    public DbSet<MessageMatch> MessageMatches { get; init; }
    
    
    // Tasks
    public DbSet<TaskMatch> TaskMatches { get; init; }
    
    // Events
    public DbSet<CalendarEvent> Events { get; init; }
    public DbSet<EventAttachment> EventAttachments { get; init; }
    public DbSet<EventParticipant> EventParticipants { get; init; }
    public DbSet<EventMatch> EventMatches { get; init; }
    
    // Other
    public DbSet<ExchangeSyncState> SyncStates { get; init; }

    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("DETECTIA");

        modelBuilder.Entity<Match>()
            .ToTable("Match")
            .HasDiscriminator<string>("Discriminator")
            .HasValue<MessageMatch>("MessageMatch")
            .HasValue<EventMatch>("EventMatch")
            .HasValue<TaskMatch>("TaskMatch");
        
        modelBuilder.Entity<MessageMatch>()
            .Property(mm => mm.AttachmentId)
            .HasColumnName("MessageAttachmentId");

        modelBuilder.Entity<EventMatch>()
            .Property(em => em.AttachmentId)
            .HasColumnName("EventAttachmentId");
        
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
            
            b.HasOne(m => m.Folder)
             .WithMany(f => f.Messages)
             .HasForeignKey(m => m.FolderId)
             .OnDelete(DeleteBehavior.Restrict); // Prevents cascading
        });
           
        modelBuilder.Entity<MessageAttachment>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).ValueGeneratedOnAdd();
        });
        
        modelBuilder.Entity<UserMailFolder>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).ValueGeneratedOnAdd();
            
            b.HasOne(f => f.User)
                .WithMany(u => u.MailboxFolders)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Restrict);
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
        
        modelBuilder.Entity<UserGroupMembership>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).ValueGeneratedOnAdd();
        });
    }
    
    public override int SaveChanges()
    {
       // UpdateLastModified();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
       // UpdateLastModified();
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