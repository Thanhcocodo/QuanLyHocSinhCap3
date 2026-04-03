using System;
using System.Collections.Generic;

namespace QuanLyHocSinh.Models;

public partial class TaiKhoan
{
    public int TaiKhoanId { get; set; }

    public string Username { get; set; } = null!;

    public string Password { get; set; } = null!;

    public byte Role { get; set; }

    public int? HocSinhId { get; set; }

    public int? GiaoVienId { get; set; }

    public bool IsActive { get; set; }

    public virtual GiaoVien? GiaoVien { get; set; }

    public virtual HocSinh? HocSinh { get; set; }
}
