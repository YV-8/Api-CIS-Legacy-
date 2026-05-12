using CIS.DataAcces.Models;
using Microsoft.EntityFrameworkCore;

namespace CIS.DataAcces.Data;

public class CisDbContext : DbContext
{
    public CisDbContext(DbContextOptions<CisDbContext> options) : base(options)
    {
    }

    public DbSet<Topic> Topics { get; set; }
    public DbSet<Idea> Ideas { get; set; }
    public DbSet<Vote> Votes { get; set; }
    public DbSet<Comment> Comments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Topic>(entity =>
        {
            entity.ToTable("topics");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.Title)
                .HasColumnName("name")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasColumnName("description");

            entity.Property(e => e.Type)
                .HasColumnName("type")
                .HasConversion<string>()
                .IsRequired();

            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .IsRequired();

            entity.Property(e => e.AuthorId)
                .HasColumnName("user_id")
                .IsRequired();

            entity.Property(e => e.VoteType)
                .HasColumnName("vote_type")
                .IsRequired();

            entity.Property(e => e.AllowComments)
                .HasColumnName("allow_comments")
                .IsRequired();

            entity.Property(e => e.AnonymousVote)
                .HasColumnName("anonymous_vote")
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("datetime")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("datetime");

            entity.Property(e => e.DeletedAt)
                .HasColumnName("deleted_at")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<Idea>(entity =>
        {
            entity.ToTable("ideas");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.Title)
                .HasColumnName("title")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasColumnName("description")
                .HasMaxLength(2000)
                .IsRequired();

            entity.Property(e => e.TopicId)
                .HasColumnName("topic_id")
                .IsRequired();

            entity.Property(e => e.AuthorId)
                .HasColumnName("author_id")
                .IsRequired();

            entity.Property(e => e.VoteCount)
                .HasColumnName("vote_count")
                .HasDefaultValue(0);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("datetime")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("datetime");

            entity.Property(e => e.DeletedAt)
                .HasColumnName("deleted_at")
                .HasColumnType("datetime");

            entity.HasOne(e => e.Topic)
                .WithMany(t => t.Ideas)
                .HasForeignKey(e => e.TopicId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Vote>(entity =>
        {
            entity.ToTable("votes");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.IdeaId)
                .HasColumnName("idea_id")
                .IsRequired();

            entity.Property(e => e.UserId)
                .HasColumnName("user_id");

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("datetime")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Idea)
                .WithMany(i => i.Votes)
                .HasForeignKey(e => e.IdeaId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.IdeaId, e.UserId })
                .IsUnique();
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.ToTable("comments");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.Content)
                .IsRequired()
                .HasColumnType("text")
                .HasColumnName("content");

            entity.Property(e => e.IdeaId)
                .IsRequired()
                .HasColumnType("varchar(255)")
                .HasColumnName("idea_id");

            entity.Property(e => e.UserId)
                .IsRequired()
                .HasColumnType("varchar(255)")
                .HasColumnName("user_id");

            entity.Property(e => e.CreatedAt)
                .ValueGeneratedOnAdd()
                .HasColumnType("datetime")
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(e => e.Idea)
                .WithMany(i => i.Comments)
                .HasForeignKey(e => e.IdeaId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.ToTable("comments");
        });

        base.OnModelCreating(modelBuilder);
    }
}