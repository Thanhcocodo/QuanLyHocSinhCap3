using System;
using System.Collections.Generic;

namespace QuanLyHocSinh.Models;

public partial class MonHoc
{
    public int MonHocId { get; set; }

    public string MaMh { get; set; } = null!;

    public string TenMh { get; set; } = null!;

    public byte HeSo { get; set; }

    public virtual ICollection<Diem> Diems { get; set; } = new List<Diem>();

    public virtual ICollection<PhanCong> PhanCongs { get; set; } = new List<PhanCong>();
}
