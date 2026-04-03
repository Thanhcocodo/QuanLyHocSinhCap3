namespace QuanLyHocSinh.Services;

using Microsoft.EntityFrameworkCore;
using QuanLyHocSinh.Models;

public sealed class StatisticsService
{
    public sealed record KhoiCount(byte Khoi, string TenKhoi, int TongDangHoc);
    public sealed record KhoaCount(int NamNhapHoc, string TenKhoa, int TongDangHoc);
    public sealed record LopCount(string TenLop, byte Khoi, int TongDangHoc);

    public async Task<List<KhoiCount>> GetTongDangHocTheoKhoiAsync()
    {
        await using var db = new QuanLyHocSinhContext();
        return await db.HocSinhs
            .AsNoTracking()
            .Where(hs => hs.TrangThai == "Đang học")
            .GroupBy(hs => hs.Lop.Khoi)
            .Select(g => new KhoiCount(g.Key, $"Khối {g.Key}", g.Count()))
            .OrderBy(x => x.Khoi)
            .ToListAsync();
    }

    public async Task<List<KhoaCount>> GetTongDangHocTheoKhoaAsync()
    {
        await using var db = new QuanLyHocSinhContext();
        return await db.HocSinhs
            .AsNoTracking()
            .Where(hs => hs.TrangThai == "Đang học")
            .GroupBy(hs => hs.NgayNhapHoc.Year)
            .Select(g => new KhoaCount(g.Key, $"Khóa {g.Key}", g.Count()))
            .OrderBy(x => x.NamNhapHoc)
            .ToListAsync();
    }

    public async Task<List<LopCount>> GetTongDangHocTheoLopAsync()
    {
        await using var db = new QuanLyHocSinhContext();
        return await db.HocSinhs
            .AsNoTracking()
            .Where(hs => hs.TrangThai == "Đang học")
            .GroupBy(hs => new { hs.Lop.TenLop, hs.Lop.Khoi })
            .Select(g => new LopCount(g.Key.TenLop, g.Key.Khoi, g.Count()))
            .OrderBy(x => x.Khoi)
            .ThenBy(x => x.TenLop)
            .ToListAsync();
    }

    public async Task<int> GetTongHocSinhAsync()
    {
        await using var db = new QuanLyHocSinhContext();
        return await db.HocSinhs.AsNoTracking().CountAsync(hs => hs.TrangThai == "Đang học");
    }

    public async Task<int> GetTongGiaoVienAsync()
    {
        await using var db = new QuanLyHocSinhContext();
        return await db.GiaoViens.AsNoTracking().CountAsync(gv => gv.TrangThai == "Đang giảng dạy");
    }
}
