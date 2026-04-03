using System;
using System.Collections.Generic;

namespace QuanLyHocSinh.Models;

public partial class PhanCong
{
    public int PhanCongId { get; set; }

    public int LopId { get; set; }

    public int MonHocId { get; set; }

    public int GiaoVienId { get; set; }

    public string NamHoc { get; set; } = null!;

    public byte HocKy { get; set; }

    public bool IsChuNhiem { get; set; }

    public virtual GiaoVien GiaoVien { get; set; } = null!;

    public virtual Lop Lop { get; set; } = null!;

    public virtual MonHoc MonHoc { get; set; } = null!;
}
