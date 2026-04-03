using System;
using System.Collections.Generic;

namespace QuanLyHocSinh.Models;

public partial class GiaoVien
{
    public int GiaoVienId { get; set; }

    public string MaGiaoVien { get; set; } = null!;

    public string HoTen { get; set; } = null!;

    public DateOnly? NgaySinh { get; set; }

    public string? GioiTinh { get; set; }

    public string? DiaChi { get; set; }

    public string? SoDienThoai { get; set; }

    public string? Email { get; set; }

    public string? ChuyenMon { get; set; }

    public string? TrinhDo { get; set; }

    public DateOnly? NgayVaoTruong { get; set; }

    public string TrangThai { get; set; } = null!;

    public virtual ICollection<PhanCong> PhanCongs { get; set; } = new List<PhanCong>();

    public virtual ICollection<TaiKhoan> TaiKhoans { get; set; } = new List<TaiKhoan>();
}
