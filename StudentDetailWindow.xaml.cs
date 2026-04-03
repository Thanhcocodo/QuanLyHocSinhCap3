namespace QuanLyHocSinh;

using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using System.Windows;
using QuanLyHocSinh.Models;

public partial class StudentDetailWindow : Window
{
    private readonly HocSinh? _existing;
    private readonly int? _forcedLopId;
    public bool Saved { get; private set; }

    private sealed record LopDisplay(int LopId, string Display);

    public StudentDetailWindow(HocSinh? existing = null, int? forcedLopId = null)
    {
        _existing = existing;
        _forcedLopId = forcedLopId;
        InitializeComponent();
        Loaded += async (_, _) => await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        await using var db = new Models.QuanLyHocSinhContext();

        IQueryable<Lop> query = db.Lops.AsNoTracking().Where(l => l.IsActive);
        if (_forcedLopId.HasValue)
            query = query.Where(l => l.LopId == _forcedLopId.Value);

        var lops = await query
            .OrderBy(l => l.Khoi).ThenBy(l => l.TenLop)
            .Select(l => new LopDisplay(l.LopId, $"{l.TenLop} (Khối {l.Khoi} - {l.NamHoc})"))
            .ToListAsync();

        LopCombo.ItemsSource = lops;

        if (_existing is not null)
        {
            TitleText.Text = "Chỉnh sửa Học sinh";
            MaHocSinhBox.Text = _existing.MaHocSinh;
            HoTenBox.Text = _existing.HoTen;
            NgaySinhPicker.SelectedDate = _existing.NgaySinh.ToDateTime(TimeOnly.MinValue);
            GioiTinhBox.Text = _existing.GioiTinh;
            LopCombo.SelectedValue = _existing.LopId;
            NgayNhapHocPicker.SelectedDate = _existing.NgayNhapHoc.ToDateTime(TimeOnly.MinValue);
            DiaChiBox.Text = _existing.DiaChi;
            SoDienThoaiBox.Text = _existing.SoDienThoai;
            EmailBox.Text = _existing.Email;
            HoTenChaBox.Text = _existing.HoTenCha;
            HoTenMeBox.Text = _existing.HoTenMe;
            SdtChaMeBox.Text = _existing.SdtchaMe;
            TrangThaiBox.Text = _existing.TrangThai;
        }
        else
        {
            MaHocSinhBox.Text = "Tự động sinh";
            NgayNhapHocPicker.SelectedDate = DateTime.Today;
            if (_forcedLopId.HasValue && lops.Count == 1)
                LopCombo.SelectedIndex = 0;
        }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = string.Empty;

        // ── Validate ──
        if (string.IsNullOrWhiteSpace(HoTenBox.Text))
        { ErrorText.Text = "Họ tên không được trống."; return; }

        if (NgaySinhPicker.SelectedDate is null)
        { ErrorText.Text = "Ngày sinh không được trống."; return; }

        if (NgaySinhPicker.SelectedDate.Value.Date >= DateTime.Today)
        { ErrorText.Text = "Ngày sinh phải nhỏ hơn ngày hôm nay."; return; }

        if (NgayNhapHocPicker.SelectedDate is null)
        { ErrorText.Text = "Ngày nhập học không được trống."; return; }

        if (LopCombo.SelectedValue is null)
        { ErrorText.Text = "Vui lòng chọn lớp."; return; }

        var hoTenCha = HoTenChaBox.Text.Trim();
        var hoTenMe  = HoTenMeBox.Text.Trim();
        if (string.IsNullOrEmpty(hoTenCha) && string.IsNullOrEmpty(hoTenMe))
        { ErrorText.Text = "Phải nhập ít nhất họ tên Cha hoặc Mẹ."; return; }

        var sdtChaMe = SdtChaMeBox.Text.Trim();
        if (string.IsNullOrEmpty(sdtChaMe))
        { ErrorText.Text = "SĐT cha/mẹ không được trống."; return; }
        if (!IsValidPhone(sdtChaMe))
        { ErrorText.Text = "SĐT cha/mẹ không hợp lệ (10 số, bắt đầu bằng 0)."; return; }

        var sdt = SoDienThoaiBox.Text.Trim();
        if (!string.IsNullOrEmpty(sdt) && !IsValidPhone(sdt))
        { ErrorText.Text = "SĐT học sinh không hợp lệ (10 số, bắt đầu bằng 0)."; return; }

        var email = EmailBox.Text.Trim();
        if (!string.IsNullOrEmpty(email) && !IsValidEmail(email))
        { ErrorText.Text = "Địa chỉ email không hợp lệ."; return; }

        int lopId = (int)LopCombo.SelectedValue;

        await using var db = new Models.QuanLyHocSinhContext();

        if (_existing is null)
        {
            int year = NgayNhapHocPicker.SelectedDate!.Value.Year;
            int count = await db.HocSinhs.CountAsync(h => h.MaHocSinh.StartsWith($"HS{year}")) + 1;
            string maHS = $"HS{year}{count:D4}";

            var hs = new HocSinh
            {
                MaHocSinh   = maHS,
                HoTen       = HoTenBox.Text.Trim(),
                NgaySinh    = DateOnly.FromDateTime(NgaySinhPicker.SelectedDate!.Value),
                GioiTinh    = (GioiTinhBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "Nam",
                LopId       = lopId,
                NgayNhapHoc = DateOnly.FromDateTime(NgayNhapHocPicker.SelectedDate!.Value),
                DiaChi      = DiaChiBox.Text.Trim().NullIfEmpty(),
                SoDienThoai = sdt.NullIfEmpty(),
                Email       = email.NullIfEmpty(),
                HoTenCha    = hoTenCha.NullIfEmpty(),
                HoTenMe     = hoTenMe.NullIfEmpty(),
                SdtchaMe    = sdtChaMe,
                TrangThai   = "Đang học"
            };
            db.HocSinhs.Add(hs);
            await db.SaveChangesAsync();

            db.TaiKhoans.Add(new TaiKhoan
            {
                Username   = maHS.ToLower(),
                Password   = "1",
                Role       = 0,
                HocSinhId  = hs.HocSinhId,
                IsActive   = true
            });
            await db.SaveChangesAsync();

            MessageBox.Show($"Đã thêm học sinh.\nTài khoản: {maHS.ToLower()}\nMật khẩu mặc định: 1",
                "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            var hs = await db.HocSinhs.FindAsync(_existing.HocSinhId);
            if (hs is null) { ErrorText.Text = "Không tìm thấy học sinh."; return; }

            hs.HoTen       = HoTenBox.Text.Trim();
            hs.NgaySinh    = DateOnly.FromDateTime(NgaySinhPicker.SelectedDate!.Value);
            hs.GioiTinh    = (GioiTinhBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? hs.GioiTinh;
            hs.LopId       = lopId;
            hs.NgayNhapHoc = DateOnly.FromDateTime(NgayNhapHocPicker.SelectedDate!.Value);
            hs.DiaChi      = DiaChiBox.Text.Trim().NullIfEmpty();
            hs.SoDienThoai = sdt.NullIfEmpty();
            hs.Email       = email.NullIfEmpty();
            hs.HoTenCha    = hoTenCha.NullIfEmpty();
            hs.HoTenMe     = hoTenMe.NullIfEmpty();
            hs.SdtchaMe    = sdtChaMe;
            hs.TrangThai   = (TrangThaiBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? hs.TrangThai;

            await db.SaveChangesAsync();
            MessageBox.Show("Đã cập nhật học sinh.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
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
