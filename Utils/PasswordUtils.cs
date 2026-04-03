namespace QuanLyHocSinh.Utils;

public static class PasswordUtils
{
    public static bool VerifyPassword(string inputPassword, string storedPassword)
    {
        return string.Equals(inputPassword, storedPassword);
    }
}
