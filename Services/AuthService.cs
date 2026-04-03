namespace QuanLyHocSinh.Services;

using Microsoft.EntityFrameworkCore;
using QuanLyHocSinh.Models;

public sealed class AuthService
{
    public async Task<(TaiKhoan? user, bool isDisabled)> LoginAsync(string username, string password)
    {
        await using var db = new QuanLyHocSinhContext();

        var user = await db.TaiKhoans
            .Include(x => x.HocSinh)
            .Include(x => x.GiaoVien)
            .SingleOrDefaultAsync(x => x.Username == username);

        if (user is null)
            return (null, false);

        if (!string.Equals(password, user.Password))
            return (null, false);

        if (!user.IsActive)
            return (null, true);

        return (user, false);
    }
}
