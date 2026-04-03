namespace QuanLyHocSinh;

using QuanLyHocSinh.Models;

public static class Session
{
    public static TaiKhoan? CurrentUser { get; set; }

    public static void Clear()
    {
        CurrentUser = null;
    }
}
