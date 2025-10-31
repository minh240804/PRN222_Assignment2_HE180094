using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Assignment2.DataAccess.Models
{

    public partial class FunewsManagementContext : DbContext
    {
        public FunewsManagementContext()
        {
        }

        public FunewsManagementContext(DbContextOptions<FunewsManagementContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Category> Categories { get; set; }

        public virtual DbSet<NewsArticle> NewsArticles { get; set; }

        public virtual DbSet<SystemAccount> SystemAccounts { get; set; }

        public virtual DbSet<Tag> Tags { get; set; }
        
        public virtual DbSet<Comment> Comments { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var builder = new ConfigurationBuilder()
                                  .SetBasePath(Directory.GetCurrentDirectory())
                                  .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            IConfigurationRoot configuration = builder.Build();
            optionsBuilder.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>(entity =>
            {
                entity.ToTable("Category");

                entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
                entity.Property(e => e.CategoryDesciption).HasMaxLength(250);
                entity.Property(e => e.CategoryName).HasMaxLength(100);
                entity.Property(e => e.ParentCategoryId).HasColumnName("ParentCategoryID");

                entity.HasOne(d => d.ParentCategory).WithMany(p => p.InverseParentCategory)
                    .HasForeignKey(d => d.ParentCategoryId)
                    .HasConstraintName("FK_Category_Category");
            });

            modelBuilder.Entity<NewsArticle>(entity =>
            {
                entity.ToTable("NewsArticle");

                entity.Property(e => e.NewsArticleId)
                    .HasMaxLength(20)
                    .HasColumnName("NewsArticleID");
                entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
                entity.Property(e => e.CreatedById).HasColumnName("CreatedByID");
                entity.Property(e => e.CreatedDate).HasColumnType("datetime");
                entity.Property(e => e.Headline).HasMaxLength(150);
                entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
                entity.Property(e => e.NewsContent).HasMaxLength(4000);
                entity.Property(e => e.NewsSource).HasMaxLength(400);
                entity.Property(e => e.NewsTitle).HasMaxLength(400);
                entity.Property(e => e.UpdatedById).HasColumnName("UpdatedByID");

                entity.HasOne(d => d.Category).WithMany(p => p.NewsArticles)
                    .HasForeignKey(d => d.CategoryId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_NewsArticle_Category");

                entity.HasOne(d => d.CreatedBy).WithMany(p => p.NewsArticles)
                    .HasForeignKey(d => d.CreatedById)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_NewsArticle_SystemAccount");

                entity.HasMany(d => d.Tags).WithMany(p => p.NewsArticles)
                    .UsingEntity<Dictionary<string, object>>(
                        "NewsTag",
                        r => r.HasOne<Tag>().WithMany()
                            .HasForeignKey("TagId")
                            .OnDelete(DeleteBehavior.ClientSetNull)
                            .HasConstraintName("FK_NewsTag_Tag"),
                        l => l.HasOne<NewsArticle>().WithMany()
                            .HasForeignKey("NewsArticleId")
                            .OnDelete(DeleteBehavior.ClientSetNull)
                            .HasConstraintName("FK_NewsTag_NewsArticle"),
                        j =>
                        {
                            j.HasKey("NewsArticleId", "TagId");
                            j.ToTable("NewsTag");
                            j.IndexerProperty<string>("NewsArticleId")
                                .HasMaxLength(20)
                                .HasColumnName("NewsArticleID");
                            j.IndexerProperty<int>("TagId").HasColumnName("TagID");
                        });
            });

            modelBuilder.Entity<SystemAccount>(entity =>
            {
                entity.HasKey(e => e.AccountId);

                entity.ToTable("SystemAccount");

                entity.Property(e => e.AccountId)
                    .ValueGeneratedNever()
                    .HasColumnName("AccountID");
                entity.Property(e => e.AccountEmail).HasMaxLength(70);
                entity.Property(e => e.AccountName).HasMaxLength(100);
                entity.Property(e => e.AccountPassword).HasMaxLength(70);
            });

            modelBuilder.Entity<Tag>(entity =>
            {
                entity.HasKey(e => e.TagId).HasName("PK_HashTag");

                entity.ToTable("Tag");

                entity.Property(e => e.TagId)
                    .ValueGeneratedNever()
                    .HasColumnName("TagID");
                entity.Property(e => e.Note).HasMaxLength(400);
                entity.Property(e => e.TagName).HasMaxLength(50);
            });

            modelBuilder.Entity<Comment>(entity =>
            {
                entity.HasKey(e => e.CommentId);

                entity.ToTable("Comment");

                entity.Property(e => e.CommentId).HasColumnName("CommentId");
                entity.Property(e => e.ArticleId)
                    .HasMaxLength(20)
                    .HasColumnName("ArticleId");
                entity.Property(e => e.AccountId).HasColumnName("AccountId");
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");
                entity.Property(e => e.IsDeleted)
                    .HasDefaultValue(false);
                entity.Property(e => e.DeletedBy).HasColumnName("DeletedBy");
                entity.Property(e => e.DeletedAt)
                    .HasColumnType("datetime")
                    .HasColumnName("DeletedAt");

                entity.HasOne(d => d.Article)
                    .WithMany(p => p.Comments)
                    .HasForeignKey(d => d.ArticleId)
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("FK_Comment_NewsArticle");

                entity.HasOne(d => d.Account)
                    .WithMany(p => p.Comments)
                    .HasForeignKey(d => d.AccountId)
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("FK_Comment_SystemAccount");
                
                entity.HasOne(d => d.DeletedByAccount)
                    .WithMany()
                    .HasForeignKey(d => d.DeletedBy)
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("FK_Comment_SystemAccount_DeletedBy");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}