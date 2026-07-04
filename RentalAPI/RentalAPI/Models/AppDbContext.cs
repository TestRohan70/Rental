using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace RentalAPI.Models;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Resident> Residents { get; set; }

    public virtual DbSet<VisitorRequest> VisitorRequests { get; set; }

    public virtual DbSet<SysmUser> SysmUsers { get; set; }

    //public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Server=ENV-BOM-480\\SQLEXPRESS;Database=Rental;Trusted_Connection=True;TrustServerCertificate=True;");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Notifica__3214EC07A3F9A14C");

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.Resident).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.ResidentId)
                .HasConstraintName("FK__Notificat__Resid__52593CB8");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Notificat__UserI__5165187F");
        });

        modelBuilder.Entity<Resident>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Resident__3214EC07B67162DF");

            entity.ToTable("Resident");

            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.OwnershipType).HasMaxLength(100);
            entity.Property(e => e.Role)
                .HasColumnName("Role")
                .HasMaxLength(100);
            entity.Property(e => e.Password).HasMaxLength(400);
            entity.Property(e => e.Society).HasMaxLength(200);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");
            entity.Property(e => e.Wing).HasMaxLength(50);
        });

        modelBuilder.Entity<VisitorRequest>(entity =>
        {
            entity.ToTable("VisitorRequest");

            entity.Property(e => e.VisitorName).HasMaxLength(200);
            entity.Property(e => e.VisitorPhone).HasMaxLength(20);
            entity.Property(e => e.Purpose).HasMaxLength(500);
            entity.Property(e => e.Wing).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.RespondedDate).HasColumnType("datetime");
            entity.Property(e => e.AcknowledgedDate).HasColumnType("datetime");
            entity.Property(e => e.VisitorPhotoUrl).HasMaxLength(500);

            entity.HasOne(d => d.Resident)
                .WithMany()
                .HasForeignKey(d => d.ResidentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Security)
                .WithMany()
                .HasForeignKey(d => d.SecurityId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SysmUser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SysmUser__3214EC27718C4C71");

            entity.ToTable("SysmUser");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.Password).HasMaxLength(400);
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UserName)
                .HasMaxLength(200)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
