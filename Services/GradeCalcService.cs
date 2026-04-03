namespace QuanLyHocSinh.Services;

using System.Linq;
using QuanLyHocSinh.Models;

public static class GradeCalcService
{
    /// <summary>
    /// Tính điểm trung bình môn theo học kỳ.
    /// Công thức: (Miệng×1 + 15phút×1 + 1Tiết×2 + GiữaKỳ×2 + CuốiKỳ×4) / (10 × HeSo)
    /// Phân mẫu cố định = 10 * HeSo (tổng trọng số tối đa khi đủ tất cả điểm).
    /// Chỉ tính được khi đã nhập đủ cả 5 loại điểm.
    /// </summary>
    public static double? TinhTBMonHocKy(Diem d, byte heSo = 1)
    {
        // Bắt buộc phải có điểm cuối kỳ mới tính TB
        if (!d.DiemCuoiKy.HasValue) return null;

        double tu = 0;
        tu += (d.DiemMieng  ?? 0) * 1;
        tu += (d.Diem15Phut ?? 0) * 1;
        tu += (d.Diem1Tiet  ?? 0) * 2;
        tu += (d.DiemGiuaKy ?? 0) * 2;
        tu += d.DiemCuoiKy.Value * 4;

        double mau = 10.0 * heSo;
        return Math.Round(tu / mau, 2);
    }

    /// <summary>
    /// Kiểm tra học sinh có đủ điều kiện lên lớp không.
    /// Điều kiện: Không có môn nào TB &lt; 3.5 VÀ TB tất cả môn &gt;= 5.0
    /// </summary>
    public static bool KiemTraDuDieuKienLenLop(IEnumerable<(Diem diem, byte heSo)> diems)
    {
        var list = diems.ToList();
        if (!list.Any()) return false;

        double tongTB = 0;
        int count = 0;

        foreach (var (d, heSo) in list)
        {
            var tb = TinhTBMonHocKy(d, heSo);
            if (tb is null) continue;     // bỏ qua môn chưa đủ điểm (chưa nhập điểm cuối kỳ) => vẫn có thể lên lớp nếu các môn khác đủ điều kiện
            //if (tb is null) return false;   // chặt: thiếu điểm (cuối kỳ) ở bất kỳ môn nào => không đủ điều kiện
            if (tb < 3.5) return false;     // trượt ngay nếu có môn < 3.5
            tongTB += tb.Value;
            count++;
        }

        if (count == 0) return false; // không có môn nào có điểm cuối kỳ => không đủ điều kiện
        return (tongTB / count) >= 5.0;
    }

    public static bool KiemTraDuDieuKienLenLopCaNam(IEnumerable<(Diem diem, byte heSo)> diems)
    {
        var list = diems.ToList();
        if (!list.Any()) return false;

        double tongTB = 0;
        int count = 0;

        foreach (var monGroup in list.GroupBy(x => x.diem.MonHocId))
        {
            var hk1 = monGroup.FirstOrDefault(x => x.diem.HocKy == 1);
            var hk2 = monGroup.FirstOrDefault(x => x.diem.HocKy == 2);

            if (hk1.diem is null || hk2.diem is null) return false;

            var tb1 = TinhTBMonHocKy(hk1.diem, hk1.heSo);
            var tb2 = TinhTBMonHocKy(hk2.diem, hk2.heSo);
            if (tb1 is null || tb2 is null) return false;

            var caNam = Math.Round((tb1.Value + tb2.Value * 2) / 3.0, 2);
            if (caNam < 3.5) return false;

            tongTB += caNam;
            count++;
        }

        if (count == 0) return false;
        return (tongTB / count) >= 5.0;
    }

    public static (bool du, double? tbTong, string ghiChu) DanhGiaLenLopTongKet(IEnumerable<(Diem diem, byte heSo)> diems)
    {
        var list = diems.ToList();
        if (!list.Any()) return (false, null, "Chưa có điểm.");

        double weightedSum = 0;
        int weightTotal = 0;

        foreach (var (d, heSo) in list)
        {
            var tb = TinhTBMonHocKy(d, heSo);
            if (tb is null) continue;

            int w = d.HocKy == 2 ? 2 : 1;
            weightedSum += tb.Value * w;
            weightTotal += w;
        }

        if (weightTotal == 0)
            return (false, null, "Chưa có điểm cuối kỳ để tính TB.");

        double avg = Math.Round(weightedSum / weightTotal, 2);
        if (avg < 5.0)
            return (false, avg, "TB tổng < 5.0.");

        return (true, avg, string.Empty);
    }
}
