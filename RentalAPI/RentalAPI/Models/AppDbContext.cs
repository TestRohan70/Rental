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

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SysmUser> SysmUsers { get; set; }

    public virtual DbSet<Resident> Residents { get; set; }

    public virtual DbSet<SocietyUserMapping> SocietyUserMappings { get; set; }

    public virtual DbSet<ResidentFlatMapping> ResidentFlatMappings { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<VisitorRequest> VisitorRequests { get; set; }

    public virtual DbSet<VisitorStatus> VisitorStatuses { get; set; }

    public virtual DbSet<VisitorVisit> VisitorVisits { get; set; }

    public virtual DbSet<SocietyMaster> SocietyMasters { get; set; }

    public virtual DbSet<WingMaster> WingMasters { get; set; }

    public virtual DbSet<FloorMaster> FloorMasters { get; set; }

    public virtual DbSet<FlatMaster> FlatMasters { get; set; }

    public virtual DbSet<FlatCategoryMaster> FlatCategoryMasters { get; set; }

    public virtual DbSet<PmWingFloorConfig> PmWingFloorConfigs { get; set; }

    public virtual DbSet<PmSocietyWingFlatConfig> PmSocietyWingFlatConfigs { get; set; }
public virtual DbSet<PmAccount> PmAccounts { get; set; }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Server=(localdb)\\MyLocalDB;Database=Premisus_DB;Trusted_Connection=True;TrustServerCertificate=True;");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Role");
            entity.HasKey(e => e.Id).HasName("PK_RoleMaster");
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasIndex(e => e.Code, "UX_RoleMaster_Code").IsUnique();
        });

        modelBuilder.Entity<SysmUser>(entity =>
        {
            entity.ToTable("SysmUser");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.Password).HasMaxLength(400);
            entity.Property(e => e.UserName).HasMaxLength(200);
            entity.Property(e => e.Role).HasMaxLength(50);

            entity.HasOne(d => d.RoleNavigation)
                .WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_SysmUser_Role");
        });

        modelBuilder.Entity<Resident>(entity =>
        {
            entity.ToTable("Resident");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("Pending");
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");

            entity.HasOne(d => d.User)
                .WithOne(p => p.Resident)
                .HasForeignKey<Resident>(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Resident_SysmUser");

            entity.HasOne(d => d.ApprovedByUser)
                .WithMany()
                .HasForeignKey(d => d.ApprovedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SocietyUserMapping>(entity =>
        {
            entity.ToTable("SocietyUserMapping");
            entity.HasKey(e => e.Id).HasName("PK_SocietyUserMapping");
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.SocietyId).HasColumnName("SocietyID");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasIndex(e => new { e.SocietyId, e.UserId }, "UQ_SocietyUserMapping_Society_User").IsUnique();

            entity.HasOne(d => d.Society)
                .WithMany()
                .HasForeignKey(d => d.SocietyId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_SocietyUserMapping_Society");

            entity.HasOne(d => d.User)
                .WithMany(p => p.SocietyUserMappings)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_SocietyUserMapping_User");
        });

        modelBuilder.Entity<ResidentFlatMapping>(entity =>
        {
            entity.ToTable("ResidentFlatMapping");
            entity.HasKey(e => e.Id).HasName("PK_ResidentFlatMapping");
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.ResidentId).HasColumnName("ResidentID");
            entity.Property(e => e.SocietyWingFlatConfigId).HasColumnName("SocietyWingFlatConfigID");
            entity.Property(e => e.OwnershipType).HasMaxLength(50);

            entity.HasOne(d => d.Resident)
                .WithMany(p => p.FlatMappings)
                .HasForeignKey(d => d.ResidentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ResidentFlatMapping_Resident");

            entity.HasOne(d => d.SocietyWingFlatConfig)
                .WithMany()
                .HasForeignKey(d => d.SocietyWingFlatConfigId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ResidentFlatMapping_SocietyWingFlatConfig");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.Resident)
                .WithMany(p => p.Notifications)
                .HasForeignKey(d => d.ResidentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.User)
                .WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VisitorRequest>(entity =>
        {
            entity.ToTable("VisitorRequest");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.VisitorName).HasMaxLength(200);
            entity.Property(e => e.VisitorPhone).HasMaxLength(20);
            entity.Property(e => e.Purpose).HasMaxLength(500);
            entity.Property(e => e.Wing).HasMaxLength(50);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.RespondedDate).HasColumnType("datetime");
            entity.Property(e => e.AcknowledgedDate).HasColumnType("datetime");
            entity.Property(e => e.VisitorPhotoUrl).HasMaxLength(500);
            entity.Property(e => e.VisitType).HasMaxLength(50);
            entity.Property(e => e.ExpectedArrivalDateTime).HasColumnType("datetime");
            entity.Property(e => e.OTPHash).HasMaxLength(500);
            entity.Property(e => e.OTPExpiresAt).HasColumnType("datetime");
            entity.Property(e => e.OTPVerifiedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Resident)
                .WithMany()
                .HasForeignKey(d => d.ResidentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_VisitorRequest_Resident");

            entity.HasOne(d => d.SecurityUser)
                .WithMany()
                .HasForeignKey(d => d.SecurityUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_VisitorRequest_Security");

            entity.HasOne(d => d.Society)
                .WithMany()
                .HasForeignKey(d => d.SocietyId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_VisitorRequest_Society");

            entity.HasOne(d => d.SocietyWingFlatConfig)
                .WithMany()
                .HasForeignKey(d => d.SocietyWingFlatConfigId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_VisitorRequest_SocietyWingFlatConfig");

            entity.HasOne(d => d.Status)
                .WithMany()
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_VisitorRequest_Status");
        });

        modelBuilder.Entity<VisitorStatus>(entity =>
        {
            entity.ToTable("VisitorStatus");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<VisitorVisit>(entity =>
        {
            entity.ToTable("VisitorVisit");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CheckInDateTime).HasColumnType("datetime");
            entity.Property(e => e.CheckOutDateTime).HasColumnType("datetime");
            entity.Property(e => e.Gate).HasMaxLength(100);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");

            entity.HasOne(d => d.VisitorRequest)
                .WithMany(p => p.VisitorVisits)
                .HasForeignKey(d => d.VisitorRequestId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_VisitorVisit_VisitorRequest");

            entity.HasOne(d => d.CheckInSecurityUser)
                .WithMany()
                .HasForeignKey(d => d.CheckInSecurityUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_VisitorVisit_CheckInSecurity");

            entity.HasOne(d => d.CheckOutSecurityUser)
                .WithMany()
                .HasForeignKey(d => d.CheckOutSecurityUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_VisitorVisit_CheckOutSecurity");

            entity.HasOne(d => d.Status)
                .WithMany()
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_VisitorVisit_Status");
        });

        modelBuilder.Entity<SocietyMaster>(entity =>
        {
            entity.ToTable("society");
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Code).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Location).HasMaxLength(100);
        });

        modelBuilder.Entity<WingMaster>(entity =>
        {
            entity.ToTable("wings");
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Code).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<FloorMaster>(entity =>
        {
            entity.ToTable("floors");
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Code).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<FlatMaster>(entity =>
        {
            entity.ToTable("Flat");
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Code).HasColumnName("CODE").HasMaxLength(20);
            entity.Property(e => e.TypeId).HasColumnName("TypeID");
            entity.HasOne(d => d.Type)
                .WithMany()
                .HasForeignKey(d => d.TypeId)
                .HasConstraintName("FK_FLAT_FLACategory_TypeID");
        });

        modelBuilder.Entity<FlatCategoryMaster>(entity =>
        {
            entity.ToTable("FlatCategory");
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Type).HasMaxLength(20);
        });

        modelBuilder.Entity<PmWingFloorConfig>(entity =>
        {
            entity.ToTable("pmWingFloorConfig");
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.WingId).HasColumnName("WingID");
            entity.Property(e => e.FloorId).HasColumnName("FloorID");

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
            entity.ToTable("pmSocietyWingFlatConfig");
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.SocietyId).HasColumnName("SocietyID");
            entity.Property(e => e.WingId).HasColumnName("WingID");
            entity.Property(e => e.FloorId).HasColumnName("FloorID");
            entity.Property(e => e.FlatId).HasColumnName("FlatID");

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


        modelBuilder.Entity<PmAccount>(entity =>
        {
            entity.ToTable("PmAccount");

            entity.HasKey(x => x.ID);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
