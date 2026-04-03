using System;
using System.Collections.Generic;

namespace QuanLyHocSinh.Models;

public partial class LichSuLop
{
    public int LichSuLopId { get; set; }

    public int HocSinhId { get; set; }

    public int LopId { get; set; }

    public string NamHoc { get; set; } = null!;

    public string? GhiChu { get; set; }

    public DateTime NgayChuyen { get; set; }

    public virtual HocSinh HocSinh { get; set; } = null!;

    public virtual Lop Lop { get; set; } = null!;
}
