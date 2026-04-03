namespace QuanLyHocSinh.Services;

using Microsoft.EntityFrameworkCore;
using QuanLyHocSinh.Models;

public sealed class PromotionService
{
    public sealed record PromotionResult(
        int HocSinhId,
        string MaHocSinh,
        string HoTen,
        string LopHienTai,
        byte Khoi,
        bool DuDieuKien,
        string KetQua,  // "Lên lớp", "Ở lại", "Tốt nghiệp"
        double? TBTong,
        string GhiChu
    );

    /// <summary>
    /// Chạy kiểm tra điều kiện lên lớp cho tất cả HS của một NamHoc.
    /// Không thay đổi DB — chỉ trả về preview.
    /// </summary>
    public async Task<List<PromotionResult>> PreviewAsync(string namHoc)
    {
        await using var db = new QuanLyHocSinhContext();

        var hocSinhs = await db.HocSinhs
            .AsNoTracking()
            .Include(hs => hs.Lop)
            .Where(hs => hs.TrangThai == "Đang học" && hs.Lop.NamHoc == namHoc)
            .ToListAsync();

        var monHocDict = await db.MonHocs.AsNoTracking().ToDictionaryAsync(m => m.MonHocId, m => (m.TenMh, m.HeSo));

        var diemByHocSinh = await db.Diems.AsNoTracking()
            .Where(d => d.NamHoc == namHoc)
            .ToListAsync();

        var diemLookup = diemByHocSinh
            .GroupBy(d => d.HocSinhId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var results = new List<PromotionResult>();

        foreach (var hs in hocSinhs)
        {
            var diems = diemLookup.TryGetValue(hs.HocSinhId, out var list) ? list : new List<Diem>();
            var diemWithHeSo = diems.Select(d => (d, monHocDict.TryGetValue(d.MonHocId, out var m) ? m.HeSo : (byte)1));
            var (du, tbTong, ghiChu) = GradeCalcService.DanhGiaLenLopTongKet(diemWithHeSo);

            string ketQua;
            if (!du)
                ketQua = "Ở lại";
            else if (hs.Lop.Khoi == 12)
                ketQua = "Tốt nghiệp";
            else
                ketQua = "Lên lớp";

            results.Add(new PromotionResult(hs.HocSinhId, hs.MaHocSinh, hs.HoTen, hs.Lop.TenLop, hs.Lop.Khoi, du, ketQua,
                tbTong, ghiChu));
        }

        return results;
    }

    private static string ComputeTenLopForKhoi(string tenLopCu, byte khoiMoi)
    {
        if (string.IsNullOrWhiteSpace(tenLopCu)) return tenLopCu;

        int i = 0;
        while (i < tenLopCu.Length && char.IsDigit(tenLopCu[i])) i++;
        if (i == 0) return tenLopCu;

        return khoiMoi + tenLopCu[i..];
    }

    /// <summary>
    /// Thực thi lên lớp. Tạo lớp mới nếu cần. Cập nhật DB.
    /// </summary>
    public async Task<(int lenLop, int oLai, int totNghiep, string error)> ApplyAsync(string namHoc)
    {
        await using var db = new QuanLyHocSinhContext();

        await using var tx = await db.Database.BeginTransactionAsync();

        var namParts = namHoc.Split('-');
        if (namParts.Length != 2 || !int.TryParse(namParts[0], out int yearStart))
            return (0, 0, 0, "Định dạng NamHoc không hợp lệ (phải là YYYY-YYYY).");

        string namHocMoi = $"{yearStart + 1}-{yearStart + 2}";

        var hocSinhs = await db.HocSinhs
            .Include(hs => hs.Lop)
            .Where(hs => hs.TrangThai == "Đang học" && hs.Lop.NamHoc == namHoc)
            .ToListAsync();

        var monHocDict = await db.MonHocs.AsNoTracking().ToDictionaryAsync(m => m.MonHocId, m => (m.TenMh, m.HeSo));

        var diemByHocSinh = await db.Diems.AsNoTracking()
            .Where(d => d.NamHoc == namHoc)
            .ToListAsync();

        var diemLookup = diemByHocSinh
            .GroupBy(d => d.HocSinhId)
            .ToDictionary(g => g.Key, g => g.ToList());

        int cntLenLop = 0, cntOLai = 0, cntTotNghiep = 0;
        var lopCache = new Dictionary<string, Lop>(); // "TenLop|Khoi|NamHoc" → Lop

        foreach (var hs in hocSinhs)
        {
            var diems = diemLookup.TryGetValue(hs.HocSinhId, out var list) ? list : new List<Diem>();
            var diemWithHeSo = diems.Select(d => (d, monHocDict.TryGetValue(d.MonHocId, out var h2) ? h2.HeSo : (byte)1));
            var (du, _, _) = GradeCalcService.DanhGiaLenLopTongKet(diemWithHeSo);

            if (!du)
            {
                // Ở lại nhưng sang năm học mới
                cntOLai++;

                byte khoiMoi = hs.Lop.Khoi;
                string tenLopMoi = ComputeTenLopForKhoi(hs.Lop.TenLop, khoiMoi);
                string cacheKey = $"{tenLopMoi}|{khoiMoi}|{namHocMoi}";

                if (!lopCache.TryGetValue(cacheKey, out var lopMoi))
                {
                    lopMoi = await db.Lops.FirstOrDefaultAsync(l =>
                        l.TenLop == tenLopMoi && l.Khoi == khoiMoi && l.NamHoc == namHocMoi);

                    if (lopMoi is null)
                    {
                        lopMoi = new Lop
                        {
                            TenLop = tenLopMoi,
                            Khoi = khoiMoi,
                            NamHoc = namHocMoi,
                            IsActive = true
                        };
                        db.Lops.Add(lopMoi);
                    }
                    lopCache[cacheKey] = lopMoi;
                }

                db.LichSuLops.Add(new LichSuLop
                {
                    HocSinhId = hs.HocSinhId,
                    LopId = hs.LopId,
                    NamHoc = namHoc,
                    GhiChu = "Ở lại",
                    NgayChuyen = DateTime.Now
                });

                hs.Lop = lopMoi;
                continue;
            }

            if (hs.Lop.Khoi == 12)
            {
                // Tốt nghiệp
                hs.TrangThai = "Đã tốt nghiệp";
                hs.NgayTotNghiep = DateOnly.FromDateTime(DateTime.Now);
                cntTotNghiep++;

                db.LichSuLops.Add(new LichSuLop
                {
                    HocSinhId = hs.HocSinhId,
                    LopId = hs.LopId,
                    NamHoc = namHoc,
                    GhiChu = "Tốt nghiệp",
                    NgayChuyen = DateTime.Now
                });
            }
            else
            {
                // Lên lớp: 10→11, 11→12
                byte khoiMoi = (byte)(hs.Lop.Khoi + 1);
                string tenLop = ComputeTenLopForKhoi(hs.Lop.TenLop, khoiMoi);

                string cacheKey = $"{tenLop}|{khoiMoi}|{namHocMoi}";
                if (!lopCache.TryGetValue(cacheKey, out var lopMoi))
                {
                    lopMoi = await db.Lops.FirstOrDefaultAsync(l =>
                        l.TenLop == tenLop && l.Khoi == khoiMoi && l.NamHoc == namHocMoi);

                    if (lopMoi is null)
                    {
                        lopMoi = new Lop
                        {
                            TenLop = tenLop,
                            Khoi = khoiMoi,
                            NamHoc = namHocMoi,
                            IsActive = true
                        };
                        db.Lops.Add(lopMoi);
                    }
                    lopCache[cacheKey] = lopMoi;
                }

                db.LichSuLops.Add(new LichSuLop
                {
                    HocSinhId = hs.HocSinhId,
                    LopId = hs.LopId,
                    NamHoc = namHoc,
                    GhiChu = $"Lên lớp {khoiMoi}",
                    NgayChuyen = DateTime.Now
                });

                hs.Lop = lopMoi;
                cntLenLop++;
            }
        }

        try
        {
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return (cntLenLop, cntOLai, cntTotNghiep, string.Empty);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return (0, 0, 0, $"Lỗi áp dụng lên lớp: {ex.Message}");
        }
    }
}
