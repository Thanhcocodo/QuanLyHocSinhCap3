using System;
using System.Collections.Generic;

namespace QuanLyHocSinh.Models;

public partial class HocSinh
{
    public int HocSinhId { get; set; }

    public string MaHocSinh { get; set; } = null!;

    public string HoTen { get; set; } = null!;

    public DateOnly NgaySinh { get; set; }

    public string GioiTinh { get; set; } = null!;

    public string? DiaChi { get; set; }

    public string? SoDienThoai { get; set; }

    public string? Email { get; set; }

    public int LopId { get; set; }

    public DateOnly NgayNhapHoc { get; set; }

    public string? HoTenCha { get; set; }

    public string? HoTenMe { get; set; }

    public string? SdtchaMe { get; set; }

    public string TrangThai { get; set; } = null!;

    public DateOnly? NgayTotNghiep { get; set; }

    public virtual ICollection<Diem> Diems { get; set; } = new List<Diem>();

    public virtual ICollection<LichSuLop> LichSuLops { get; set; } = new List<LichSuLop>();

    public virtual Lop Lop { get; set; } = null!;

    public virtual ICollection<TaiKhoan> TaiKhoans { get; set; } = new List<TaiKhoan>();
}
