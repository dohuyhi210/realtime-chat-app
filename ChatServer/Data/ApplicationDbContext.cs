// ==============================================
// FILE: Data/ApplicationDbContext.cs
// Mô tả: DbContext để kết nối với SQL Server
// ==============================================
using Microsoft.EntityFrameworkCore;
using ChatServer.Models;

namespace ChatServer.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets - đại diện cho các bảng
        public DbSet<User> Users { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<GroupMember> GroupMembers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ===== CONFIG CHO BẢNG USERS =====
            modelBuilder.Entity<User>(entity =>
            {
                // Index cho Username (tăng tốc login)
                entity.HasIndex(e => e.Username)
                    .IsUnique()
                    .HasDatabaseName("IX_Users_Username");

                // Index cho LastSeen (tăng tốc query online/offline)
                entity.HasIndex(e => e.LastSeen)
                    .HasDatabaseName("IX_Users_LastSeen");

                // Relationship: User có nhiều Messages gửi đi
                entity.HasMany(u => u.SentMessages)
                    .WithOne(m => m.Sender)
                    .HasForeignKey(m => m.SenderId)
                    .OnDelete(DeleteBehavior.NoAction);  // Không xóa cascade

                // Relationship: User có nhiều Messages nhận được
                entity.HasMany(u => u.ReceivedMessages)
                    .WithOne(m => m.Receiver)
                    .HasForeignKey(m => m.ReceiverId)
                    .OnDelete(DeleteBehavior.NoAction);  // Không xóa cascade

                // Relationship: User có nhiều Groups đã tạo
                entity.HasMany(u => u.CreatedGroups)
                    .WithOne(g => g.Creator)
                    .HasForeignKey(g => g.CreatedBy)
                    .OnDelete(DeleteBehavior.NoAction);  // Không xóa cascade
            });

            // ===== CONFIG CHO BẢNG MESSAGES =====
            modelBuilder.Entity<Message>(entity =>
            {
                // Index cho chat cá nhân (SenderId, ReceiverId, Timestamp)
                entity.HasIndex(e => new { e.SenderId, e.ReceiverId, e.Timestamp })
                    .HasDatabaseName("IX_Messages_Private");

                // Index cho chat nhóm (GroupId, Timestamp)
                entity.HasIndex(e => new { e.GroupId, e.Timestamp })
                    .HasDatabaseName("IX_Messages_Group");

                // Index cho tin nhắn chưa đọc (ReceiverId, IsRead)
                entity.HasIndex(e => new { e.ReceiverId, e.IsRead })
                    .HasDatabaseName("IX_Messages_Unread");

                // Relationship với Group (cascade delete)
                entity.HasOne(m => m.Group)
                    .WithMany(g => g.Messages)
                    .HasForeignKey(m => m.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);  // Xóa nhóm → xóa tin nhắn
            });

            // ===== CONFIG CHO BẢNG GROUPS =====
            modelBuilder.Entity<Group>(entity =>
            {
                // Index cho CreatedBy
                entity.HasIndex(e => e.CreatedBy)
                    .HasDatabaseName("IX_Groups_CreatedBy");
            });

            // ===== CONFIG CHO BẢNG GROUPMEMBERS =====
            modelBuilder.Entity<GroupMember>(entity =>
            {
                // Composite Primary Key (GroupId, UserId)
                entity.HasKey(gm => new { gm.GroupId, gm.UserId });

                // Index cho UserId (lấy danh sách nhóm của user)
                entity.HasIndex(e => e.UserId)
                    .HasDatabaseName("IX_GroupMembers_UserId");

                // Relationship với Group (cascade delete)
                entity.HasOne(gm => gm.Group)
                    .WithMany(g => g.Members)
                    .HasForeignKey(gm => gm.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);  // Xóa nhóm → xóa thành viên

                // Relationship với User (no action)
                entity.HasOne(gm => gm.User)
                    .WithMany(u => u.GroupMemberships)
                    .HasForeignKey(gm => gm.UserId)
                    .OnDelete(DeleteBehavior.NoAction);  // Không xóa cascade
            });
        }
    }
}