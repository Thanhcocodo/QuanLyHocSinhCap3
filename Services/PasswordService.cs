namespace QuanLyHocSinh.Services;

using Microsoft.EntityFrameworkCore;
using QuanLyHocSinh.Models;

public sealed class PasswordService
{
    public async Task<(bool ok, string message)> ChangePasswordAsync(int taiKhoanId, string oldPassword, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            return (false, "Mật khẩu mới phải có ít nhất 6 ký tự.");

        await using var db = new QuanLyHocSinhContext();

        var user = await db.TaiKhoans.SingleOrDefaultAsync(x => x.TaiKhoanId == taiKhoanId && x.IsActive);
        if (user is null)
            return (false, "Tài khoản không tồn tại.");

        if (!string.Equals(oldPassword, user.Password))
            return (false, "Mật khẩu cũ không đúng.");

        user.Password = newPassword;
        await db.SaveChangesAsync();
        return (true, "Đổi mật khẩu thành công.");
    }

    /// <summary>
    /// Reset mật khẩu không cần mật khẩu cũ (dùng cho admin và GVCN).
    /// </summary>
    public async Task<(bool ok, string message)> ResetPasswordAsync(int taiKhoanId, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            return (false, "Mật khẩu mới phải có ít nhất 6 ký tự.");

        await using var db = new QuanLyHocSinhContext();

        var user = await db.TaiKhoans.SingleOrDefaultAsync(x => x.TaiKhoanId == taiKhoanId && x.IsActive);
        if (user is null)
            return (false, "Tài khoản không tồn tại.");

        user.Password = newPassword;
        await db.SaveChangesAsync();
        return (true, "Reset mật khẩu thành công.");
    }
}
