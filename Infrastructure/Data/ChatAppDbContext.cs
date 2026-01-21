using Core.Entities;
using Core.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{
    public class ChatAppDbContext : IdentityDbContext<User, IdentityRole<int>, int>
    {
        public ChatAppDbContext(DbContextOptions<ChatAppDbContext> options) : base(options)
        {
        }

        public DbSet<Message> Messages { get; set; } = null!;
        public DbSet<Conversations> Conversations { get; set; } = null!;
        public DbSet<ConversationMembers> ConversationMembers { get; set; } = null!;
        public DbSet<MessageReaction> MessageReactions { get; set; } = null!;
        public DbSet<Attachment> Attachments { get; set; } = null!;
        public DbSet<UserContact> UserContacts { get; set; } = null!;
        public DbSet<BlockedUser> BlockedUsers { get; set; } = null!;
        public DbSet<Report> Reports { get; set; } = null!;
        public DbSet<Call> Calls { get; set; } = null!;
        public DbSet<MessageDeletedForUser> MessageDeletedForUsers { get; set; } = null!;
        public DbSet<MessageReadStatus> MessageReadStatuses { get; set; } = null!;
        public DbSet<PushSubscription> PushSubscriptions { get; set; } = null!;
        public DbSet<Poll> Polls { get; set; } = null!;
        public DbSet<PollOption> PollOptions { get; set; } = null!;
        public DbSet<PollVote> PollVotes { get; set; } = null!;
        public DbSet<MessageMention> MessageMentions { get; set; } = null!;


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Conversation Configuration
            modelBuilder.Entity<Conversations>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ConversationType).IsRequired().HasMaxLength(20);
                entity.Property(e => e.GroupName).HasMaxLength(200);
                entity.HasOne(e => e.Creator).WithMany(u => u.CreatedConversations)
                    .HasForeignKey(e => e.CreatedBy).OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => e.CreatedAt);
            });

            // ConversationMember Configuration
            modelBuilder.Entity<ConversationMembers>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Role).HasMaxLength(20).HasDefaultValue(ConversationRole.Member);
                entity.HasOne(e => e.Conversation).WithMany(c => c.Members)
                    .HasForeignKey(e => e.ConversationId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.User).WithMany(u => u.ConversationMembers)
                    .HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(new[] { "ConversationId", "UserId" }).IsUnique();
            });

            // Message Configuration
            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Content);
                entity.Property(e => e.MessageType).HasMaxLength(20).HasDefaultValue(MessageType.Text);
                entity.HasOne(e => e.Conversation).WithMany(c => c.Messages)
                    .HasForeignKey(e => e.ConversationId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Sender).WithMany(u => u.SentMessages)
                    .HasForeignKey(e => e.SenderId).OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => e.ConversationId);
                entity.HasIndex(e => e.CreatedAt);

                // Reply Configuration
                entity.HasOne(e => e.ParentMessage)
                    .WithMany(m => m.Replies)
                    .HasForeignKey(e => e.ParentMessageId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // MessageReaction Configuration
            modelBuilder.Entity<MessageReaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EmojiType).IsRequired().HasMaxLength(50);
                entity.HasOne(e => e.Message).WithMany(m => m.Reactions)
                    .HasForeignKey(e => e.MessageId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.User).WithMany(u => u.MessageReactions)
                    .HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(new[] { "MessageId", "UserId" }).IsUnique();
            });

            // Attachment Configuration
            modelBuilder.Entity<Attachment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FileUrl).IsRequired();
                entity.Property(e => e.FileType).HasMaxLength(50);
                entity.HasOne(e => e.Message).WithMany(m => m.Attachments)
                    .HasForeignKey(e => e.MessageId).OnDelete(DeleteBehavior.Cascade);
            });

            // MessageDeletedForUser Configuration
            modelBuilder.Entity<MessageDeletedForUser>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Message).WithMany(m => m.DeletedForUsers)
                    .HasForeignKey(e => e.MessageId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.User).WithMany()
                    .HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(new[] { "MessageId", "UserId" }).IsUnique();
            });

            // UserContact Configuration
            modelBuilder.Entity<UserContact>()
                 .HasOne(uc => uc.Sender)
                 .WithMany(u => u.UserContacts)
                 .HasForeignKey(uc => uc.SenderId)
                 .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserContact>()
                .HasOne(uc => uc.Receiver)
                .WithMany(u => u.ContactedByUsers)
                .HasForeignKey(uc => uc.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            // Create unique constraint on UserContact
            modelBuilder.Entity<UserContact>()
                .HasIndex(uc => new { uc.SenderId, uc.ReceiverId })
                .IsUnique();

            // BlockedUser Configuration
            modelBuilder.Entity<BlockedUser>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Blocker)
                    .WithMany(u => u.BlockedUsers)
                    .HasForeignKey(e => e.BlockerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.BlockedUserProfile)
                    .WithMany(u => u.BlockedByUsers)
                    .HasForeignKey(e => e.BlockedUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Ensure unique block relationship
                entity.HasIndex(new[] { "BlockerId", "BlockedUserId" }).IsUnique();
            });

            // Configure Report
            modelBuilder.Entity<Report>()
                .HasOne(r => r.ReportedUser)
                .WithMany()
                .HasForeignKey(r => r.ReportedUserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Report>()
                .HasOne(r => r.Reporter)
                .WithMany()
                .HasForeignKey(r => r.ReporterId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Report>()
                .HasOne(r => r.Conversation)
                .WithMany()
                .HasForeignKey(r => r.ConversationId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure Call
            modelBuilder.Entity<Call>()
                .HasOne(c => c.Initiator)
                .WithMany()
                .HasForeignKey(c => c.InitiatorId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Call>()
                .HasOne(c => c.Receiver)
                .WithMany()
                .HasForeignKey(c => c.ReceiverId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Call>()
                .HasOne(c => c.Conversation)
                .WithMany()
                .HasForeignKey(c => c.ConversationId)
                .OnDelete(DeleteBehavior.NoAction);

            // MessageReadStatus Configuration
            modelBuilder.Entity<MessageReadStatus>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Message)
                    .WithMany()
                    .HasForeignKey(e => e.MessageId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Poll Configuration
            modelBuilder.Entity<Poll>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Question).IsRequired();
                entity.HasOne(e => e.Creator).WithMany()
                    .HasForeignKey(e => e.CreatorId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Conversation).WithMany()
                    .HasForeignKey(e => e.ConversationId).OnDelete(DeleteBehavior.Cascade);
            });

            // PollOption Configuration
            modelBuilder.Entity<PollOption>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Text).IsRequired();
                entity.HasOne(e => e.Poll).WithMany(p => p.Options)
                    .HasForeignKey(e => e.PollId).OnDelete(DeleteBehavior.Cascade);
            });

            // PollVote Configuration
            modelBuilder.Entity<PollVote>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.PollOption).WithMany(o => o.Votes)
                    .HasForeignKey(e => e.PollOptionId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.User).WithMany()
                    .HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
                // Ensure a user can only vote once per option (or poll? logic might handle multiple votes if allowed, but uniqueness per option/user is good)
                entity.HasIndex(new[] { "PollOptionId", "UserId" }).IsUnique();
            });

            // MessageMention Configuration
            modelBuilder.Entity<MessageMention>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Message).WithMany(m => m.Mentions)
                    .HasForeignKey(e => e.MessageId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.User).WithMany()
                    .HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(new[] { "MessageId", "UserId" }).IsUnique();
            });
        }
    }

}

