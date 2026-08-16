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

    public virtual DbSet<SocietyAlert> SocietyAlerts { get; set; }

    public virtual DbSet<Resident> Residents { get; set; }

    public virtual DbSet<VisitorRequest> VisitorRequests { get; set; }

    public virtual DbSet<SysmUser> SysmUsers { get; set; }

    public virtual DbSet<SocietyMaster> SocietyMasters { get; set; }

    public virtual DbSet<WingMaster> WingMasters { get; set; }

    public virtual DbSet<FloorMaster> FloorMasters { get; set; }

    public virtual DbSet<FlatMaster> FlatMasters { get; set; }

    public virtual DbSet<FlatCategoryMaster> FlatCategoryMasters { get; set; }

    public virtual DbSet<PmWingFloorConfig> PmWingFloorConfigs { get; set; }

    public virtual DbSet<PmSocietyWingFlatConfig> PmSocietyWingFlatConfigs { get; set; }

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

        modelBuilder.Entity<SocietyAlert>(entity =>
        {
            entity.ToTable("SocietyAlert");

            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Message).HasMaxLength(1000);
            entity.Property(e => e.AlertType).HasMaxLength(50);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");

            entity.HasOne(d => d.CreatedBySecurity)
                .WithMany()
                .HasForeignKey(d => d.CreatedBySecurityId)
                .OnDelete(DeleteBehavior.Restrict);
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

        modelBuilder.Entity<SocietyMaster>(entity =>
        {
            entity.Property(e => e.Code).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Location).HasMaxLength(100);
        });

        modelBuilder.Entity<WingMaster>(entity =>
        {
            entity.Property(e => e.Code).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<FloorMaster>(entity =>
        {
            entity.Property(e => e.Code).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<FlatMaster>(entity =>
        {
            entity.Property(e => e.Code).HasMaxLength(20);
            entity.HasOne(d => d.Type)
                .WithMany()
                .HasForeignKey(d => d.TypeId)
                .HasConstraintName("FK_FLAT_FLACategory_TypeID");
        });

        modelBuilder.Entity<FlatCategoryMaster>(entity =>
        {
            entity.Property(e => e.Type).HasMaxLength(20);
        });

        modelBuilder.Entity<PmWingFloorConfig>(entity =>
        {
            entity.HasIndex(e => new { e.WingId, e.FloorId })
                .IsUnique()
                .HasDatabaseName("UQ_WingFloorConfig");

            entity.HasOne(d => d.Wing)
                .WithMany()
                .HasForeignKey(d => d.WingId)
                .HasConstraintName("FK_WingFloorConfig_Wing");

            entity.HasOne(d => d.Floor)
                .WithMany()
                .HasForeignKey(d => d.FloorId)
                .HasConstraintName("FK_WingFloorConfig_Floor");
        });

        modelBuilder.Entity<PmSocietyWingFlatConfig>(entity =>
        {
            entity.HasIndex(e => new { e.SocietyId, e.WingId, e.FloorId, e.FlatId })
                .IsUnique()
                .HasDatabaseName("UQ_SocietyWingFlatConfig");

            entity.HasOne(d => d.Society)
                .WithMany()
                .HasForeignKey(d => d.SocietyId)
                .HasConstraintName("FK_SocietyWingFlatConfig_Society");

            entity.HasOne(d => d.Wing)
                .WithMany()
                .HasForeignKey(d => d.WingId)
                .HasConstraintName("FK_SocietyWingFlatConfig_Wing");

            entity.HasOne(d => d.Floor)
                .WithMany()
                .HasForeignKey(d => d.FloorId)
                .HasConstraintName("FK_SocietyWingFlatConfig_Floor");

            entity.HasOne(d => d.Flat)
                .WithMany()
                .HasForeignKey(d => d.FlatId)
                .HasConstraintName("FK_SocietyWingFlatConfig_Flat");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
