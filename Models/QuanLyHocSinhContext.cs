using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace QuanLyHocSinh.Models;

public partial class QuanLyHocSinhContext : DbContext
{
    public QuanLyHocSinhContext()
    {
    }

    public QuanLyHocSinhContext(DbContextOptions<QuanLyHocSinhContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Diem> Diems { get; set; }

    public virtual DbSet<GiaoVien> GiaoViens { get; set; }

    public virtual DbSet<HocSinh> HocSinhs { get; set; }

    public virtual DbSet<LichSuLop> LichSuLops { get; set; }

    public virtual DbSet<Lop> Lops { get; set; }

    public virtual DbSet<MonHoc> MonHocs { get; set; }

    public virtual DbSet<PhanCong> PhanCongs { get; set; }

    public virtual DbSet<TaiKhoan> TaiKhoans { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var ConnectionString = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build().GetConnectionString("DefaultConnection");
            optionsBuilder.UseSqlServer(ConnectionString);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Diem>(entity =>
        {
            entity.HasKey(e => e.DiemId).HasName("PK__Diem__A533761F87B53A92");

            entity.ToTable("Diem");

            entity.HasIndex(e => e.HocSinhId, "IX_Diem_HocSinhId");

            entity.HasIndex(e => e.MonHocId, "IX_Diem_MonHocId");

            entity.HasIndex(e => new { e.HocSinhId, e.MonHocId, e.HocKy, e.NamHoc }, "UQ_Diem").IsUnique();

            entity.Property(e => e.NamHoc).HasMaxLength(20);

            entity.HasOne(d => d.HocSinh).WithMany(p => p.Diems)
                .HasForeignKey(d => d.HocSinhId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Diem_HocSinh");

            entity.HasOne(d => d.MonHoc).WithMany(p => p.Diems)
                .HasForeignKey(d => d.MonHocId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Diem_MonHoc");
        });

        modelBuilder.Entity<GiaoVien>(entity =>
        {
            entity.HasKey(e => e.GiaoVienId).HasName("PK__GiaoVien__7D9E87D805B96833");

            entity.ToTable("GiaoVien");

            entity.HasIndex(e => e.MaGiaoVien, "UQ_GiaoVien_MaGiaoVien").IsUnique();

            entity.Property(e => e.ChuyenMon).HasMaxLength(50);
            entity.Property(e => e.DiaChi).HasMaxLength(200);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.GioiTinh).HasMaxLength(10);
            entity.Property(e => e.HoTen).HasMaxLength(100);
            entity.Property(e => e.MaGiaoVien).HasMaxLength(20);
            entity.Property(e => e.SoDienThoai).HasMaxLength(15);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(50)
                .HasDefaultValue("Đang giảng dạy");
            entity.Property(e => e.TrinhDo).HasMaxLength(50);
        });

        modelBuilder.Entity<HocSinh>(entity =>
        {
            entity.HasKey(e => e.HocSinhId).HasName("PK__HocSinh__CD0A979F0716D895");

            entity.ToTable("HocSinh");

            entity.HasIndex(e => e.LopId, "IX_HocSinh_LopId");

            entity.HasIndex(e => e.MaHocSinh, "IX_HocSinh_MaHocSinh");

            entity.HasIndex(e => e.MaHocSinh, "UQ_HocSinh_MaHocSinh").IsUnique();

            entity.Property(e => e.DiaChi).HasMaxLength(200);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.GioiTinh).HasMaxLength(10);
            entity.Property(e => e.HoTen).HasMaxLength(100);
            entity.Property(e => e.HoTenCha).HasMaxLength(100);
            entity.Property(e => e.HoTenMe).HasMaxLength(100);
            entity.Property(e => e.MaHocSinh).HasMaxLength(20);
            entity.Property(e => e.SdtchaMe)
                .HasMaxLength(15)
                .HasColumnName("SDTChaMe");
            entity.Property(e => e.SoDienThoai).HasMaxLength(15);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(50)
                .HasDefaultValue("Đang học");

            entity.HasOne(d => d.Lop).WithMany(p => p.HocSinhs)
                .HasForeignKey(d => d.LopId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HocSinh_Lop");
        });

        modelBuilder.Entity<LichSuLop>(entity =>
        {
            entity.HasKey(e => e.LichSuLopId).HasName("PK__LichSuLo__3DA4C6133EB19114");

            entity.ToTable("LichSuLop");

            entity.HasIndex(e => e.HocSinhId, "IX_LichSuLop_HocSinhId");

            entity.Property(e => e.GhiChu).HasMaxLength(200);
            entity.Property(e => e.NamHoc).HasMaxLength(20);
            entity.Property(e => e.NgayChuyen)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.HocSinh).WithMany(p => p.LichSuLops)
                .HasForeignKey(d => d.HocSinhId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LichSuLop_HocSinh");

            entity.HasOne(d => d.Lop).WithMany(p => p.LichSuLops)
                .HasForeignKey(d => d.LopId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LichSuLop_Lop");
        });

        modelBuilder.Entity<Lop>(entity =>
        {
            entity.HasKey(e => e.LopId).HasName("PK__Lop__40585D2B3B2E3FAC");

            entity.ToTable("Lop");

            entity.HasIndex(e => new { e.TenLop, e.Khoi, e.NamHoc }, "UQ_Lop").IsUnique();

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.NamHoc).HasMaxLength(20);
            entity.Property(e => e.TenLop).HasMaxLength(50);
        });

        modelBuilder.Entity<MonHoc>(entity =>
        {
            entity.HasKey(e => e.MonHocId).HasName("PK__MonHoc__32C3DE7D78949A62");

            entity.ToTable("MonHoc");

            entity.HasIndex(e => e.MaMh, "UQ_MonHoc_MaMH").IsUnique();

            entity.Property(e => e.HeSo).HasDefaultValue((byte)1);
            entity.Property(e => e.MaMh)
                .HasMaxLength(20)
                .HasColumnName("MaMH");
            entity.Property(e => e.TenMh)
                .HasMaxLength(100)
                .HasColumnName("TenMH");
        });

        modelBuilder.Entity<PhanCong>(entity =>
        {
            entity.HasKey(e => e.PhanCongId).HasName("PK__PhanCong__7EF840BD978344A6");

            entity.ToTable("PhanCong");

            entity.HasIndex(e => e.GiaoVienId, "IX_PhanCong_GiaoVienId");

            entity.HasIndex(e => e.LopId, "IX_PhanCong_LopId");

            entity.HasIndex(e => new { e.LopId, e.MonHocId, e.NamHoc, e.HocKy }, "UQ_PhanCong").IsUnique();

            entity.Property(e => e.NamHoc).HasMaxLength(20);

            entity.HasOne(d => d.GiaoVien).WithMany(p => p.PhanCongs)
                .HasForeignKey(d => d.GiaoVienId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PhanCong_GiaoVien");

            entity.HasOne(d => d.Lop).WithMany(p => p.PhanCongs)
                .HasForeignKey(d => d.LopId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PhanCong_Lop");

            entity.HasOne(d => d.MonHoc).WithMany(p => p.PhanCongs)
                .HasForeignKey(d => d.MonHocId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PhanCong_MonHoc");
        });

        modelBuilder.Entity<TaiKhoan>(entity =>
        {
            entity.HasKey(e => e.TaiKhoanId).HasName("PK__TaiKhoan__9A124B45C3A8C1C3");

            entity.ToTable("TaiKhoan");

            entity.HasIndex(e => e.Username, "IX_TaiKhoan_Username");

            entity.HasIndex(e => e.Username, "UQ_TaiKhoan_Username").IsUnique();

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Password).HasMaxLength(100);
            entity.Property(e => e.Username).HasMaxLength(50);

            entity.HasOne(d => d.GiaoVien).WithMany(p => p.TaiKhoans)
                .HasForeignKey(d => d.GiaoVienId)
                .HasConstraintName("FK_TaiKhoan_GiaoVien");

            entity.HasOne(d => d.HocSinh).WithMany(p => p.TaiKhoans)
                .HasForeignKey(d => d.HocSinhId)
                .HasConstraintName("FK_TaiKhoan_HocSinh");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
