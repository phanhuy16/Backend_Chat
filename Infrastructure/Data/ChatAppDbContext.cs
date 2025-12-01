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
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.MessageType).HasMaxLength(20).HasDefaultValue(MessageType.Text);
                entity.HasOne(e => e.Conversation).WithMany(c => c.Messages)
                    .HasForeignKey(e => e.ConversationId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Sender).WithMany(u => u.SentMessages)
                    .HasForeignKey(e => e.SenderId).OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => e.ConversationId);
                entity.HasIndex(e => e.CreatedAt);
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
        }
    }

}

