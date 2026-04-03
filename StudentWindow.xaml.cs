namespace QuanLyHocSinh;

using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;
using QuanLyHocSinh.Services;

public partial class StudentWindow : Window
{
    private sealed record GradeRow(
        string NamHoc, byte HocKy, string TenMon,
        double? DiemMieng, double? Diem15Phut, double? Diem1Tiet,
        double? DiemGiuaKy, double? DiemCuoiKy,
        double? TBMon, bool? DatYeuCau, string KetQua);

    public StudentWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await InitAsync();
    }

    private async Task InitAsync()
    {
        var hsId = Session.CurrentUser?.HocSinhId;
        if (hsId is null) return;

        await using var db = new Models.QuanLyHocSinhContext();
        var hs = await db.HocSinhs.AsNoTracking()
            .Include(h => h.Lop)
            .FirstOrDefaultAsync(h => h.HocSinhId == hsId.Value);

        if (hs is null) return;

        HoTenText.Text = hs.HoTen;
        LopText.Text = $"Lớp: {hs.Lop.TenLop} (Khối {hs.Lop.Khoi})";
        NgaySinhText.Text = $"Ngày sinh: {hs.NgaySinh:dd/MM/yyyy}";
        TrangThaiText.Text = hs.TrangThai;
        InfoText.Text = $"Học sinh: {hs.HoTen}";

        // Load danh sách năm học
        var namHocList = await db.Diems.AsNoTracking()
            .Where(d => d.HocSinhId == hsId.Value)
            .Select(d => d.NamHoc).Distinct()
            .OrderByDescending(n => n).ToListAsync();

        NamHocCombo.Items.Add("Tất cả");
        foreach (var n in namHocList) NamHocCombo.Items.Add(n);
        NamHocCombo.SelectedIndex = 0;

        await LoadGradesAsync(hsId.Value);
    }

    private async void LoadGrades_Click(object sender, RoutedEventArgs e)
    {
        var hsId = Session.CurrentUser?.HocSinhId;
        if (hsId is null) return;
        await LoadGradesAsync(hsId.Value);
    }

    private async Task LoadGradesAsync(int hsId)
    {
        await using var db = new Models.QuanLyHocSinhContext();
        var monHocDict = await db.MonHocs.AsNoTracking().ToDictionaryAsync(m => m.MonHocId, m => (m.TenMh, m.HeSo));

        var query = db.Diems.AsNoTracking().Where(d => d.HocSinhId == hsId);

        // Filter năm học
        if (NamHocCombo.SelectedItem is string namHoc && namHoc != "Tất cả")
            query = query.Where(d => d.NamHoc == namHoc);

        // Filter học kỳ
        int hocKyIndex = HocKyCombo.SelectedIndex;
        if (hocKyIndex == 1) query = query.Where(d => d.HocKy == 1);
        else if (hocKyIndex == 2) query = query.Where(d => d.HocKy == 2);

        var diems = await query.OrderByDescending(d => d.NamHoc).ThenBy(d => d.HocKy).ToListAsync();

        var rows = new List<GradeRow>();
        foreach (var d in diems)
        {
            var (tenMon, heSo) = monHocDict.TryGetValue(d.MonHocId, out var m) ? m : ("Môn không xác định", (byte)1);
            var tb = GradeCalcService.TinhTBMonHocKy(d, heSo);
            bool? dat = tb.HasValue ? tb >= 5.0 : null;
            string ketQua = tb is null ? "Chưa đủ điểm" : (tb >= 5.0 ? "Đạt" : "Chưa đạt");

            rows.Add(new GradeRow(d.NamHoc, d.HocKy, tenMon,
                d.DiemMieng, d.Diem15Phut, d.Diem1Tiet, d.DiemGiuaKy, d.DiemCuoiKy,
                tb, dat, ketQua));
        }

        GradeGrid.ItemsSource = rows;

        // Summary
        var coDiem = rows.Where(r => r.TBMon.HasValue).ToList();
        if (coDiem.Count > 0)
        {
            double weightedSum = coDiem.Sum(r => r.TBMon!.Value * (r.HocKy == 2 ? 2 : 1));
            int weightTotal = coDiem.Sum(r => r.HocKy == 2 ? 2 : 1);
            double avgAll = weightTotal > 0 ? (weightedSum / weightTotal) : 0;
            int chuaDat = coDiem.Count(r => r.TBMon < 5.0);
            SummaryText.Text = $"Tổng số điểm: {rows.Count} môn-kỳ | " +
                $"TB tổng: {avgAll:N2} | Môn chưa đạt: {chuaDat}";
        }
        else
        {
            SummaryText.Text = "Chưa có điểm.";
        }
    }

    private void ChangePassword_Click(object sender, RoutedEventArgs e)
        => new ChangePasswordWindow { Owner = this }.ShowDialog();

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Bạn có chắc muốn đăng xuất?", "Xác nhận đăng xuất",
            MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        Session.Clear();
        new LoginWindow().Show();
        Close();
    }
}
