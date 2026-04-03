using System;
using System.Collections.Generic;

namespace QuanLyHocSinh.Models;

public partial class Lop
{
    public int LopId { get; set; }

    public string TenLop { get; set; } = null!;

    public byte Khoi { get; set; }

    public string NamHoc { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<HocSinh> HocSinhs { get; set; } = new List<HocSinh>();

    public virtual ICollection<LichSuLop> LichSuLops { get; set; } = new List<LichSuLop>();

    public virtual ICollection<PhanCong> PhanCongs { get; set; } = new List<PhanCong>();
}
