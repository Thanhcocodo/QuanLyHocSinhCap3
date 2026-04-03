namespace QuanLyHocSinh;

using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using QuanLyHocSinh.Models;
using QuanLyHocSinh.Services;

public partial class GradeEntryWindow : Window
{
    private readonly int _lopId;
    private readonly int _monHocId;
    private readonly string _namHoc;
    private readonly byte _hocKy;
    private byte _heSo = 1;
    private ObservableCollection<GradeEditRow> _rows = new();

    public GradeEntryWindow(int lopId, int monHocId, string namHoc, byte hocKy)
    {
        _lopId = lopId; _monHocId = monHocId; _namHoc = namHoc; _hocKy = hocKy;
        InitializeComponent();
        Loaded += async (_, _) => await InitAsync();
    }

    private async Task InitAsync()
    {
        await using var db = new Models.QuanLyHocSinhContext();
        var lop = await db.Lops.AsNoTracking().FirstOrDefaultAsync(l => l.LopId == _lopId);
        var mon = await db.MonHocs.AsNoTracking().FirstOrDefaultAsync(m => m.MonHocId == _monHocId);
        _heSo = mon?.HeSo ?? 1;
        HeaderText.Text = $"Lớp: {lop?.TenLop} | Môn: {mon?.TenMh} | {_namHoc} – HK{_hocKy} | Hệ số: {_heSo}";
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        await using var db = new Models.QuanLyHocSinhContext();
        var hocSinhs = await db.HocSinhs.AsNoTracking()
            .Where(hs => hs.LopId == _lopId && hs.TrangThai == "Đang học")
            .OrderBy(hs => hs.HoTen).ToListAsync();

        var diemDict = await db.Diems.AsNoTracking()
            .Where(d => d.HocSinhId != 0 && d.MonHocId == _monHocId && d.NamHoc == _namHoc && d.HocKy == _hocKy)
            .Where(d => hocSinhs.Select(h => h.HocSinhId).Contains(d.HocSinhId))
            .ToDictionaryAsync(d => d.HocSinhId);

        _rows = new ObservableCollection<GradeEditRow>();
        foreach (var hs in hocSinhs)
        {
            diemDict.TryGetValue(hs.HocSinhId, out var d);
            var row = new GradeEditRow
            {
                HocSinhId = hs.HocSinhId,
                MaHocSinh = hs.MaHocSinh,
                HoTen = hs.HoTen,
                DiemMieng = d?.DiemMieng,
                Diem15Phut = d?.Diem15Phut,
                Diem1Tiet = d?.Diem1Tiet,
                DiemGiuaKy = d?.DiemGiuaKy,
                DiemCuoiKy = d?.DiemCuoiKy,
                HeSo = _heSo
            };
            _rows.Add(row);
        }
        StudentGrid.ItemsSource = _rows;
        UpdateSummary();
        StatusText.Text = $"Đã tải {_rows.Count} học sinh.";
    }

    private void UpdateSummary()
    {
        var withGrades = _rows.Where(r => r.TBMon.HasValue).ToList();
        if (withGrades.Count == 0) { SummaryText.Text = "Chưa có điểm."; return; }
        double avg = withGrades.Average(r => r.TBMon!.Value);
        int pass = withGrades.Count(r => r.TBMon >= 5.0);
        int fail = withGrades.Count(r => r.TBMon < 5.0);
        SummaryText.Text = $"Tổng: {_rows.Count} HS | Có điểm: {withGrades.Count} | " +
            $"TB lớp: {avg:N2} | Đạt: {pass} | Chưa đạt: {fail}";
    }

    private async void Load_Click(object sender, RoutedEventArgs e) => await LoadDataAsync();

    private async void SaveAll_Click(object sender, RoutedEventArgs e)
    {
        var gvId = Session.CurrentUser?.GiaoVienId;
        if (gvId is null) { MessageBox.Show("Tài khoản không liên kết giáo viên."); return; }

        var svc = new GradeService();
        int saved = 0, errors = 0;

        foreach (var row in _rows)
        {
            var (ok, _) = await svc.UpsertDiemAsync(
                gvId.Value, row.HocSinhId, _monHocId, _namHoc, _hocKy,
                row.DiemMieng, row.Diem15Phut, row.Diem1Tiet, row.DiemGiuaKy, row.DiemCuoiKy);
            if (ok) saved++;
            else errors++;
        }

        if (errors > 0)
            MessageBox.Show($"Đã lưu {saved} học sinh. Lỗi: {errors}. Kiểm tra lại phân công.");
        else
            StatusText.Text = $"✅ Đã lưu điểm {saved} học sinh.";

        await LoadDataAsync();
    }
}

public sealed class GradeEditRow : INotifyPropertyChanged
{
    public int HocSinhId { get; set; }
    public string MaHocSinh { get; set; } = "";
    public string HoTen { get; set; } = "";
    public byte HeSo { get; set; } = 1;

    private double? _diemMieng;
    public double? DiemMieng { get => _diemMieng; set { _diemMieng = value; OnChanged(); } }

    private double? _diem15Phut;
    public double? Diem15Phut { get => _diem15Phut; set { _diem15Phut = value; OnChanged(); } }

    private double? _diem1Tiet;
    public double? Diem1Tiet { get => _diem1Tiet; set { _diem1Tiet = value; OnChanged(); } }

    private double? _diemGiuaKy;
    public double? DiemGiuaKy { get => _diemGiuaKy; set { _diemGiuaKy = value; OnChanged(); } }

    private double? _diemCuoiKy;
    public double? DiemCuoiKy { get => _diemCuoiKy; set { _diemCuoiKy = value; OnChanged(); } }

    public double? TBMon
    {
        get
        {
            // Tạo Diem tạm để tính
            var d = new Models.Diem {
                DiemMieng = _diemMieng, Diem15Phut = _diem15Phut,
                Diem1Tiet = _diem1Tiet, DiemGiuaKy = _diemGiuaKy, DiemCuoiKy = _diemCuoiKy
            };
            return GradeCalcService.TinhTBMonHocKy(d, HeSo);
        }
    }

    public bool? DatYeuCau => TBMon.HasValue ? TBMon >= 5.0 : null;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnChanged([CallerMemberName] string? prop = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TBMon)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DatYeuCau)));
    }
}
