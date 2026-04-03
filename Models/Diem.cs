using System;
using System.Collections.Generic;

namespace QuanLyHocSinh.Models;

public partial class Diem
{
    public int DiemId { get; set; }

    public int HocSinhId { get; set; }

    public int MonHocId { get; set; }

    public byte HocKy { get; set; }

    public string NamHoc { get; set; } = null!;

    public double? DiemMieng { get; set; }

    public double? Diem15Phut { get; set; }

    public double? Diem1Tiet { get; set; }

    public double? DiemGiuaKy { get; set; }

    public double? DiemCuoiKy { get; set; }

    public virtual HocSinh HocSinh { get; set; } = null!;

    public virtual MonHoc MonHoc { get; set; } = null!;
}
