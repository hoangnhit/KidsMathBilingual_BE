using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Swd392.Api.Infrastructure.Database.Entities;

namespace Swd392.Api.Infrastructure.Database;

public partial class KidsMathDbContext : DbContext
{
    public KidsMathDbContext(DbContextOptions<KidsMathDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<Admin> Admins { get; set; }

    public virtual DbSet<Badge> Badges { get; set; }

    public virtual DbSet<Chapter> Chapters { get; set; }

    public virtual DbSet<Course> Courses { get; set; }

    public virtual DbSet<Parent> Parents { get; set; }

    public virtual DbSet<Progress> Progresses { get; set; }

    public virtual DbSet<Question> Questions { get; set; }

    public virtual DbSet<Quiz> Quizzes { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<StudentBadge> StudentBadges { get; set; }

    public virtual DbSet<Teacher> Teachers { get; set; }

    public virtual DbSet<Topic> Topics { get; set; }

    public virtual DbSet<Transaction> Transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.account_id);

            entity.ToTable("Account");

            entity.HasIndex(e => e.email, "UQ__Account__AB6E6164BD439D49").IsUnique();

            entity.HasIndex(e => e.username, "UQ__Account__F3DBC5723931F424").IsUnique();

            entity.Property(e => e.account_id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.created_at).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.email).HasMaxLength(100);
            entity.Property(e => e.fullname).HasMaxLength(100);
            entity.Property(e => e.password_hash).HasMaxLength(255);
            entity.Property(e => e.role).HasMaxLength(20);
            entity.Property(e => e.updated_at).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.username).HasMaxLength(50);
        });

        modelBuilder.Entity<Admin>(entity =>
        {
            entity.HasKey(e => e.admin_id);

            entity.ToTable("Admin");

            entity.HasIndex(e => e.email, "UQ__Admin__AB6E6164E68D08B2").IsUnique();

            entity.HasIndex(e => e.username, "UQ__Admin__F3DBC572D42F80FC").IsUnique();

            entity.Property(e => e.admin_id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.created_at).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.email).HasMaxLength(100);
            entity.Property(e => e.password_hash).HasMaxLength(255);
            entity.Property(e => e.role)
                .HasMaxLength(20)
                .HasDefaultValue("admin");
            entity.Property(e => e.updated_at).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.username).HasMaxLength(50);
        });

        modelBuilder.Entity<Badge>(entity =>
        {
            entity.HasKey(e => e.badge_id).HasName("PK__Badge__E798965625B664A0");

            entity.ToTable("Badge");

            entity.HasIndex(e => e.name, "UQ__Badge__72E12F1B0FA74D64").IsUnique();

            entity.Property(e => e.created_at).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.criteria).HasMaxLength(255);
            entity.Property(e => e.description).HasMaxLength(255);
            entity.Property(e => e.name).HasMaxLength(100);
            entity.Property(e => e.status)
                .HasMaxLength(20)
                .HasDefaultValue("active");
            entity.Property(e => e.updated_at).HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<Chapter>(entity =>
        {
            entity.HasKey(e => e.chapter_id).HasName("PK__Chapter__745EFE87FB396E84");

            entity.ToTable("Chapter");

            entity.HasIndex(e => e.topic_id, "IX_Chapter_Topic");

            entity.Property(e => e.created_at).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.status)
                .HasMaxLength(20)
                .HasDefaultValue("active");
            entity.Property(e => e.title_en).HasMaxLength(100);
            entity.Property(e => e.title_local).HasMaxLength(100);
            entity.Property(e => e.updated_at).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.topic).WithMany(p => p.Chapters)
                .HasForeignKey(d => d.topic_id)
                .HasConstraintName("FK_Chapter_Topic");
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.course_id).HasName("PK__Course__8F1EF7AEF40A40AA");

            entity.ToTable("Course");

            entity.HasIndex(e => e.creator_id, "IX_Course_Creator");

            entity.Property(e => e.created_at).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.price).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.status)
                .HasMaxLength(20)
                .HasDefaultValue("active");
            entity.Property(e => e.title_en).HasMaxLength(100);
            entity.Property(e => e.title_local).HasMaxLength(100);
            entity.Property(e => e.updated_at).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.creator).WithMany(p => p.Courses)
                .HasForeignKey(d => d.creator_id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Course_Admin");
        });

        modelBuilder.Entity<Parent>(entity =>
        {
            entity.HasKey(e => e.parent_id);

            entity.ToTable("Parent");

            entity.HasIndex(e => e.email, "UQ__Parent__AB6E6164583135B0").IsUnique();

            entity.HasIndex(e => e.username, "UQ__Parent__F3DBC572454A5CCC").IsUnique();

            entity.Property(e => e.parent_id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.created_at).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.email).HasMaxLength(100);
            entity.Property(e => e.full_name).HasMaxLength(100);
            entity.Property(e => e.password_hash).HasMaxLength(255);
            entity.Property(e => e.phone).HasMaxLength(20);
            entity.Property(e => e.role)
                .HasMaxLength(20)
                .HasDefaultValue("parent");
            entity.Property(e => e.updated_at).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.username).HasMaxLength(50);
        });

        modelBuilder.Entity<Progress>(entity =>
        {
            entity.HasKey(e => e.progress_id).HasName("PK__Progress__49B3D8C1A7891EAD");

            entity.ToTable("Progress");

            entity.HasIndex(e => e.course_id, "IX_Prog_Course");

            entity.HasIndex(e => e.student_id, "IX_Prog_Student");

            entity.HasIndex(e => e.topic_id, "IX_Prog_Topic");

            entity.Property(e => e.created_at).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.score).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.status)
                .HasMaxLength(20)
                .HasDefaultValue("in_progress");
            entity.Property(e => e.updated_at).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.course).WithMany(p => p.Progresses)
                .HasForeignKey(d => d.course_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Prog_Course");

            entity.HasOne(d => d.student).WithMany(p => p.Progresses)
                .HasForeignKey(d => d.student_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Prog_Student");

            entity.HasOne(d => d.topic).WithMany(p => p.Progresses)
                .HasForeignKey(d => d.topic_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Prog_Topic");
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(e => e.question_id).HasName("PK__Question__2EC21549440830DE");

            entity.ToTable("Question");

            entity.HasIndex(e => e.quiz_id, "IX_Ques_Quiz");

            entity.Property(e => e.content_en).HasMaxLength(500);
            entity.Property(e => e.content_local).HasMaxLength(500);
            entity.Property(e => e.correct_key).HasMaxLength(10);
            entity.Property(e => e.created_at).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.difficulty).HasMaxLength(20);
            entity.Property(e => e.updated_at).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.quiz).WithMany(p => p.Questions)
                .HasForeignKey(d => d.quiz_id)
                .HasConstraintName("FK_Question_Quiz");
        });

        modelBuilder.Entity<Quiz>(entity =>
        {
            entity.HasKey(e => e.quiz_id).HasName("PK__Quiz__2D7053ECB33D28AB");

            entity.ToTable("Quiz");

            entity.HasIndex(e => e.course_id, "IX_Quiz_Course");

            entity.HasIndex(e => e.topic_id, "IX_Quiz_Topic");

            entity.Property(e => e.created_at).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.passing_score)
                .HasDefaultValue(700m)
                .HasColumnType("decimal(5, 2)");
            entity.Property(e => e.status)
                .HasMaxLength(20)
                .HasDefaultValue("active");
            entity.Property(e => e.title_en).HasMaxLength(100);
            entity.Property(e => e.title_local).HasMaxLength(100);
            entity.Property(e => e.updated_at).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.course).WithMany(p => p.Quizzes)
                .HasForeignKey(d => d.course_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Quiz_Course");

            entity.HasOne(d => d.topic).WithMany(p => p.Quizzes)
                .HasForeignKey(d => d.topic_id)
                .HasConstraintName("FK_Quiz_Topic");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.student_id);

            entity.ToTable("Student");

            entity.HasIndex(e => e.parent_id, "IX_Student_Parent");

            entity.HasIndex(e => e.username, "UQ__Student__F3DBC5725F4A3AED").IsUnique();

            entity.Property(e => e.student_id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.created_at).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.full_name).HasMaxLength(100);
            entity.Property(e => e.password_hash).HasMaxLength(255);
            entity.Property(e => e.role)
                .HasMaxLength(20)
                .HasDefaultValue("student");
            entity.Property(e => e.updated_at).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.username).HasMaxLength(50);

            entity.HasOne(d => d.parent).WithMany(p => p.Students)
                .HasForeignKey(d => d.parent_id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Student_Parent");
        });

        modelBuilder.Entity<StudentBadge>(entity =>
        {
            entity.HasKey(e => e.student_badge_id).HasName("PK__StudentB__0EABB86638CCBCF1");

            entity.ToTable("StudentBadge");

            entity.HasIndex(e => e.badge_id, "IX_SB_Badge");

            entity.HasIndex(e => e.student_id, "IX_SB_Student");

            entity.Property(e => e.created_at).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.earned_at).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.source).HasMaxLength(100);
            entity.Property(e => e.updated_at).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.badge).WithMany(p => p.StudentBadges)
                .HasForeignKey(d => d.badge_id)
                .HasConstraintName("FK_SB_Badge");

            entity.HasOne(d => d.student).WithMany(p => p.StudentBadges)
                .HasForeignKey(d => d.student_id)
                .HasConstraintName("FK_SB_Student");
        });

        modelBuilder.Entity<Teacher>(entity =>
        {
            entity.HasKey(e => e.teacher_id);

            entity.ToTable("Teacher");

            entity.HasIndex(e => e.email, "UQ__Teacher__AB6E616451D1ADF5").IsUnique();

            entity.HasIndex(e => e.username, "UQ__Teacher__F3DBC572DDB42DB2").IsUnique();

            entity.Property(e => e.teacher_id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.created_at).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.email).HasMaxLength(100);
            entity.Property(e => e.full_name).HasMaxLength(100);
            entity.Property(e => e.password_hash).HasMaxLength(255);
            entity.Property(e => e.phone).HasMaxLength(20);
            entity.Property(e => e.role)
                .HasMaxLength(20)
                .HasDefaultValue("teacher");
            entity.Property(e => e.updated_at).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.username).HasMaxLength(50);
        });

        modelBuilder.Entity<Topic>(entity =>
        {
            entity.HasKey(e => e.topic_id).HasName("PK__Topic__D5DAA3E9168C9861");

            entity.ToTable("Topic");

            entity.HasIndex(e => e.course_id, "IX_Topic_Course");

            entity.Property(e => e.created_at).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.is_bilingual).HasDefaultValue(true);
            entity.Property(e => e.status)
                .HasMaxLength(20)
                .HasDefaultValue("active");
            entity.Property(e => e.title_en).HasMaxLength(100);
            entity.Property(e => e.title_local).HasMaxLength(100);
            entity.Property(e => e.updated_at).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.course).WithMany(p => p.Topics)
                .HasForeignKey(d => d.course_id)
                .HasConstraintName("FK_Topic_Course");
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.transaction_id).HasName("PK__Transact__85C600AFAB1465B1");

            entity.HasIndex(e => e.course_id, "IX_Tran_Course");

            entity.HasIndex(e => e.parent_id, "IX_Tran_Parent");

            entity.HasIndex(e => e.student_id, "IX_Tran_Student");

            entity.Property(e => e.amount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.created_at).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.currency)
                .HasMaxLength(3)
                .HasDefaultValue("USD")
                .IsFixedLength();
            entity.Property(e => e.external_ref).HasMaxLength(100);
            entity.Property(e => e.method).HasMaxLength(20);
            entity.Property(e => e.status)
                .HasMaxLength(20)
                .HasDefaultValue("pending");
            entity.Property(e => e.updated_at).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.course).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.course_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tran_Course");

            entity.HasOne(d => d.parent).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.parent_id)
                .HasConstraintName("FK_Tran_Parent");

            entity.HasOne(d => d.student).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.student_id)
                .HasConstraintName("FK_Tran_Student");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
