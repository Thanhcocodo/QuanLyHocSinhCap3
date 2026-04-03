namespace QuanLyHocSinh;

using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using System.Windows;
using QuanLyHocSinh.Models;

public partial class TeacherDetailWindow : Window
{
    private readonly GiaoVien? _existing;
    public bool Saved { get; private set; }

    public TeacherDetailWindow(GiaoVien? existing = null)
    {
        _existing = existing;
        InitializeComponent();

        if (existing is not null)
        {
            TitleText.Text = "Chỉnh sửa Giáo viên";
            MaGiaoVienBox.Text = existing.MaGiaoVien;
            HoTenBox.Text = existing.HoTen;
            NgaySinhPicker.SelectedDate = existing.NgaySinh?.ToDateTime(TimeOnly.MinValue);
            GioiTinhBox.Text = existing.GioiTinh;
            DiaChiBox.Text = existing.DiaChi;
            SoDienThoaiBox.Text = existing.SoDienThoai;
            EmailBox.Text = existing.Email;
            ChuyenMonBox.Text = existing.ChuyenMon;
            TrinhDoBox.Text = existing.TrinhDo;
            NgayVaoTruongPicker.SelectedDate = existing.NgayVaoTruong?.ToDateTime(TimeOnly.MinValue);
            TrangThaiBox.Text = existing.TrangThai;
        }
        else
        {
            MaGiaoVienBox.Text = "Tự động sinh";
        }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = string.Empty;

        // ── Validate bắt buộc ──
        if (string.IsNullOrWhiteSpace(HoTenBox.Text))
        { ErrorText.Text = "Họ tên không được trống."; return; }

        if (NgaySinhPicker.SelectedDate is null)
        { ErrorText.Text = "Ngày sinh không được trống."; return; }

        if (NgaySinhPicker.SelectedDate.Value.Date >= DateTime.Today)
        { ErrorText.Text = "Ngày sinh phải nhỏ hơn ngày hôm nay."; return; }

        if (string.IsNullOrWhiteSpace(DiaChiBox.Text))
        { ErrorText.Text = "Địa chỉ không được trống."; return; }

        var sdt = SoDienThoaiBox.Text.Trim();
        if (!string.IsNullOrEmpty(sdt) && !IsValidPhone(sdt))
        { ErrorText.Text = "Số điện thoại không hợp lệ (10 số, bắt đầu bằng 0)."; return; }

        var email = EmailBox.Text.Trim();
        if (!string.IsNullOrEmpty(email) && !IsValidEmail(email))
        { ErrorText.Text = "Địa chỉ email không hợp lệ."; return; }

        await using var db = new Models.QuanLyHocSinhContext();

        if (_existing is null)
        {
            int year = DateTime.Now.Year;
            int count = await db.GiaoViens.CountAsync(g => g.MaGiaoVien.StartsWith($"GV{year}")) + 1;
            string maGV = $"GV{year}{count:D3}";

            var gv = new GiaoVien
            {
                MaGiaoVien = maGV,
                HoTen = HoTenBox.Text.Trim(),
                NgaySinh = DateOnly.FromDateTime(NgaySinhPicker.SelectedDate!.Value),
                GioiTinh = (GioiTinhBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "Nam",
                DiaChi = DiaChiBox.Text.Trim(),
                SoDienThoai = sdt.NullIfEmpty(),
                Email = email.NullIfEmpty(),
                ChuyenMon = ChuyenMonBox.Text.Trim().NullIfEmpty(),
                TrinhDo = TrinhDoBox.Text.Trim().NullIfEmpty(),
                NgayVaoTruong = NgayVaoTruongPicker.SelectedDate.HasValue
                    ? DateOnly.FromDateTime(NgayVaoTruongPicker.SelectedDate.Value) : null,
                TrangThai = (TrangThaiBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "Đang giảng dạy"
            };
            db.GiaoViens.Add(gv);
            await db.SaveChangesAsync();

            db.TaiKhoans.Add(new TaiKhoan
            {
                Username = maGV.ToLower(),
                Password = "1",
                Role = 2,
                GiaoVienId = gv.GiaoVienId,
                IsActive = true
            });
            await db.SaveChangesAsync();

            MessageBox.Show($"Đã thêm giáo viên.\nTài khoản: {maGV.ToLower()}\nMật khẩu mặc định: 1",
                "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            var gv = await db.GiaoViens.FindAsync(_existing.GiaoVienId);
            if (gv is null) { ErrorText.Text = "Không tìm thấy giáo viên."; return; }

            gv.HoTen = HoTenBox.Text.Trim();
            gv.NgaySinh = DateOnly.FromDateTime(NgaySinhPicker.SelectedDate!.Value);
            gv.GioiTinh = (GioiTinhBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? gv.GioiTinh;
            gv.DiaChi = DiaChiBox.Text.Trim();
            gv.SoDienThoai = sdt.NullIfEmpty();
            gv.Email = email.NullIfEmpty();
            gv.ChuyenMon = ChuyenMonBox.Text.Trim().NullIfEmpty();
            gv.TrinhDo = TrinhDoBox.Text.Trim().NullIfEmpty();
            gv.NgayVaoTruong = NgayVaoTruongPicker.SelectedDate.HasValue
                ? DateOnly.FromDateTime(NgayVaoTruongPicker.SelectedDate.Value) : null;
            gv.TrangThai = (TrangThaiBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? gv.TrangThai;

            await db.SaveChangesAsync();
            MessageBox.Show("Đã cập nhật giáo viên.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        Saved = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

    private static bool IsValidPhone(string s)
        => Regex.IsMatch(s, @"^0[0-9]{9}$");

    private static bool IsValidEmail(string s)
        => Regex.IsMatch(s, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
}

internal static class StringExtensions
{
    public static string? NullIfEmpty(this string? s) => string.IsNullOrWhiteSpace(s) ? null : s;
}
