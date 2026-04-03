namespace QuanLyHocSinh;

using Microsoft.EntityFrameworkCore;
using System.Windows;
using QuanLyHocSinh.Models;

public partial class AssignmentEditWindow : Window
{
    public bool Saved { get; private set; }

    private sealed record LopItem(int LopId, string Display);
    private sealed record MonHocItem(int MonHocId, string Display);
    private sealed record GiaoVienItem(int GiaoVienId, string Display);

    public AssignmentEditWindow()
    {
        InitializeComponent();
        NamHocBox.Text = $"{DateTime.Now.Year}-{DateTime.Now.Year + 1}";
        Loaded += async (_, _) => await LoadComboBoxesAsync();
    }

    private async Task LoadComboBoxesAsync()
    {
        await using var db = new Models.QuanLyHocSinhContext();

        LopCombo.ItemsSource = await db.Lops.AsNoTracking()
            .Where(l => l.IsActive)
            .OrderBy(l => l.Khoi).ThenBy(l => l.TenLop)
            .Select(l => new LopItem(l.LopId, $"{l.TenLop} (Khối {l.Khoi} - {l.NamHoc})"))
            .ToListAsync();

        MonHocCombo.ItemsSource = await db.MonHocs.AsNoTracking()
            .OrderBy(m => m.TenMh)
            .Select(m => new MonHocItem(m.MonHocId, $"{m.TenMh} (Hệ số {m.HeSo})"))
            .ToListAsync();

        GiaoVienCombo.ItemsSource = await db.GiaoViens.AsNoTracking()
            .Where(g => g.TrangThai == "Đang giảng dạy")
            .OrderBy(g => g.HoTen)
            .Select(g => new GiaoVienItem(g.GiaoVienId, $"{g.HoTen} - {g.ChuyenMon}"))
            .ToListAsync();
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = string.Empty;

        if (LopCombo.SelectedValue is null) { ErrorText.Text = "Vui lòng chọn lớp."; return; }
        if (MonHocCombo.SelectedValue is null) { ErrorText.Text = "Vui lòng chọn môn học."; return; }
        if (GiaoVienCombo.SelectedValue is null) { ErrorText.Text = "Vui lòng chọn giáo viên."; return; }
        if (string.IsNullOrWhiteSpace(NamHocBox.Text)) { ErrorText.Text = "Nhập năm học."; return; }

        int lopId = (int)LopCombo.SelectedValue;
        int monHocId = (int)MonHocCombo.SelectedValue;
        int giaoVienId = (int)GiaoVienCombo.SelectedValue;
        string namHoc = NamHocBox.Text.Trim();
        byte hocKy = (byte)(HocKyCombo.SelectedIndex + 1);

        try
        {
            await using var db = new Models.QuanLyHocSinhContext();

            bool exists = await db.PhanCongs.AnyAsync(p =>
                p.LopId == lopId && p.MonHocId == monHocId &&
                p.NamHoc == namHoc && p.HocKy == hocKy);

            if (exists)
            {
                ErrorText.Text = "Phân công này đã tồn tại (lớp + môn + năm + kỳ đã có).";
                return;
            }

            db.PhanCongs.Add(new PhanCong
            {
                LopId = lopId,
                MonHocId = monHocId,
                GiaoVienId = giaoVienId,
                NamHoc = namHoc,
                HocKy = hocKy,
                IsChuNhiem = ChuNhiemCheck.IsChecked == true
            });
            await db.SaveChangesAsync();
            MessageBox.Show("Đã thêm phân công.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            Saved = true;
            Close();
        }
        catch (Exception ex) { ErrorText.Text = $"Lỗi: {ex.Message}"; }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
}
