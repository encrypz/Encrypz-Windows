using Encrypz.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Encrypz.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<EncryptedFile> EncryptedFiles { get; set; } = null!;
        public DbSet<Folder> Folders { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(256);
            });

            // EncryptedFile configuration
            modelBuilder.Entity<EncryptedFile>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.EncryptedFileName)
                      .HasMaxLength(512)
                      .IsRequired();

                entity.Property(e => e.GoogleDriveFileId)
                      .HasMaxLength(256)
                      .IsRequired();

                entity.Property(e => e.InitializationVector)
                      .HasMaxLength(16)
                      .IsRequired();

                entity.Property(e => e.AuthenticationTag)
                      .HasMaxLength(16)
                      .IsRequired();

                entity.Property(e => e.EncryptedThumbnail)
                      .HasMaxLength(2000);
                      
                entity.Property(e => e.ThumbnailIv)
                      .HasMaxLength(16);
                      
                entity.Property(e => e.ThumbnailAuthTag)
                      .HasMaxLength(16);
            });

            // Folder configuration
            modelBuilder.Entity<Folder>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.EncryptedFolderName)
                      .HasMaxLength(512)
                      .IsRequired();

                entity.Property(e => e.InitializationVector)
                      .HasMaxLength(16)
                      .IsRequired();

                entity.Property(e => e.AuthenticationTag)
                      .HasMaxLength(16)
                      .IsRequired();
            });

            // Relationships
            modelBuilder.Entity<EncryptedFile>()
                .HasOne(f => f.User)
                .WithMany(u => u.EncryptedFiles)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Folder>()
                .HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Folder>()
                .HasOne(f => f.ParentFolder)
                .WithMany(f => f.SubFolders)
                .HasForeignKey(f => f.ParentFolderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EncryptedFile>()
                .HasOne(f => f.Folder)
                .WithMany(f => f.Files)
                .HasForeignKey(f => f.FolderId)
                .OnDelete(DeleteBehavior.SetNull);

            // Fix for TiDB: Remove ascii_general_ci collation from Guid columns
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(Guid) || property.ClrType == typeof(Guid?))
                    {
                        property.SetCollation("utf8mb4_general_ci");
                        property.SetCharSet("utf8mb4");
                    }
                }
            }
        }
    }
}
