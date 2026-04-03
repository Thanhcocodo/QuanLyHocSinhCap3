namespace QuanLyHocSinh.Services;

using Microsoft.EntityFrameworkCore;
using QuanLyHocSinh.Models;

public sealed class GradeService
{
    public async Task<(bool ok, string message)> UpsertDiemAsync(
        int giaoVienId,
        int hocSinhId,
        int monHocId,
        string namHoc,
        byte hocKy,
        double? diemMieng,
        double? diem15Phut,
        double? diem1Tiet,
        double? diemGiuaKy,
        double? diemCuoiKy)
    {
        if (string.IsNullOrWhiteSpace(namHoc))
            return (false, "NamHoc không hợp lệ.");

        await using var db = new QuanLyHocSinhContext();

        var hs = await db.HocSinhs.AsNoTracking().SingleOrDefaultAsync(x => x.HocSinhId == hocSinhId);
        if (hs is null)
            return (false, "Học sinh không tồn tại.");

        var lopId = hs.LopId;

        var isAssigned = await db.PhanCongs.AsNoTracking().AnyAsync(pc =>
            pc.GiaoVienId == giaoVienId &&
            pc.LopId == lopId &&
            pc.MonHocId == monHocId &&
            pc.NamHoc == namHoc &&
            pc.HocKy == hocKy);

        if (!isAssigned)
            return (false, "Bạn không được phân công dạy lớp/môn này trong năm học/học kỳ đã chọn.");

        var diem = await db.Diems.SingleOrDefaultAsync(d =>
            d.HocSinhId == hocSinhId &&
            d.MonHocId == monHocId &&
            d.NamHoc == namHoc &&
            d.HocKy == hocKy);

        if (diem is null)
        {
            diem = new Diem
            {
                HocSinhId = hocSinhId,
                MonHocId = monHocId,
                NamHoc = namHoc,
                HocKy = hocKy
            };
            db.Diems.Add(diem);
        }

        diem.DiemMieng = diemMieng;
        diem.Diem15Phut = diem15Phut;
        diem.Diem1Tiet = diem1Tiet;
        diem.DiemGiuaKy = diemGiuaKy;
        diem.DiemCuoiKy = diemCuoiKy;

        await db.SaveChangesAsync();
        return (true, "Lưu điểm thành công.");
    }
}
