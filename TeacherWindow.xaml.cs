namespace QuanLyHocSinh;

using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using QuanLyHocSinh.Models;

public partial class TeacherWindow : Window
{
    // ID của giáo viên đang đăng nhập (lấy từ Session)
    private int? _gvId;

    // ─────────────────────────────────────────────────────────────
    //  DỮ LIỆU HIỂN THỊ
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Dòng dữ liệu hiển thị thông tin học sinh trên DataGrid
    /// </summary>
    private sealed record StudentRow(
        int HocSinhId,
        string MaHocSinh,
        string HoTen,
        DateOnly NgaySinh,
        string GioiTinh,
        string? SoDienThoai,
        string? SdtchaMe,
        string TrangThai,
        bool IsAccountActive);

    /// <summary>
    /// Dòng dữ liệu hiển thị phân công giảng dạy trên DataGrid
    /// </summary>
    private sealed record AssignRow(
        int PhanCongId,       
        string TenLop,       
        byte Khoi,            
        string TenMH,        
        string NamHoc,       
        byte HocKy,         
        bool IsChuNhiem,       
        int SoHS,               
        int LopId,        
        int MonHocId);

    /// <summary>
    /// Item hiển thị trong combobox chọn lớp
    /// </summary>
    private sealed record LopItem(
        int LopId,           
        string Display,         // Nội dung hiển thị (Ví dụ: "10A1 (K10 - 2024-2025) ⭐ GVCN")
        bool IsChuNhiem);    

    public TeacherWindow()
    {
        InitializeComponent();
        // Lấy ID giáo viên từ phiên đăng nhập
        _gvId = Session.CurrentUser?.GiaoVienId;

        // Khi cửa sổ được tải xong, hiển thị thông tin giáo viên
        Loaded += async (_, _) => await InitHeaderAsync();
    }

    /// <summary>
    /// Hiển thị tên giáo viên ở đầu cửa sổ
    /// </summary>
    private async Task InitHeaderAsync()
    {
        if (_gvId is null)
        {
            InfoText.Text = "Giáo viên:";
            return;
        }

        await using var db = new Models.QuanLyHocSinhContext();
        var gv = await db.GiaoViens
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.GiaoVienId == _gvId.Value);

        InfoText.Text = gv is null
            ? "Giáo viên:"
            : $"Giáo viên: {gv.HoTen}";
    }

    // ═══════════════════════════════════════════════════════════════
    //  TAB 1: PHÂN CÔNG GIẢNG DẠY & NHẬP ĐIỂM
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Khi tab "Phân công" được tải, tự động load danh sách phân công
    /// </summary>
    private void AssignTab_Loaded(object sender, RoutedEventArgs e) => _ = LoadAssignAsync();

    /// <summary>
    /// Nút "Tải lại" danh sách phân công
    /// </summary>
    private void LoadAssign_Click(object sender, RoutedEventArgs e) => _ = LoadAssignAsync();

    /// <summary>
    /// Load danh sách các lớp và môn học mà giáo viên được phân công
    /// </summary>
    private async Task LoadAssignAsync()
    {
        if (_gvId is null) return;
        await using var db = new Models.QuanLyHocSinhContext();

        // Lấy tất cả phân công của giáo viên này
        var pcs = await db.PhanCongs.AsNoTracking()
            .Include(p => p.Lop).Include(p => p.MonHoc)
            .Where(p => p.GiaoVienId == _gvId.Value)
            .OrderByDescending(p => p.NamHoc).ThenBy(p => p.HocKy).ThenBy(p => p.Lop.TenLop)
            .ToListAsync();

        // Chuyển đổi dữ liệu sang format hiển thị
        var rows = pcs.Select(p => new AssignRow(
            p.PhanCongId, p.Lop.TenLop, p.Lop.Khoi, p.MonHoc.TenMh,
            p.NamHoc, p.HocKy, p.IsChuNhiem,
            db.HocSinhs.Count(h => h.LopId == p.LopId && h.TrangThai == "Đang học"),
            p.LopId, p.MonHocId)).ToList();

        AssignGrid.ItemsSource = rows;
    }

    /// <summary>
    /// Xử lý khi nhấn nút "Nhập điểm" - mở cửa sổ nhập điểm cho lớp và môn đã chọn
    /// </summary>
    private void GoGradeEntry_Click(object sender, RoutedEventArgs e)
    {
        // Kiểm tra đã chọn phân công chưa
        if (AssignGrid.SelectedItem is not AssignRow row)
        {
            MessageBox.Show("Vui lòng chọn một phân công để nhập điểm.", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Mở cửa sổ nhập điểm
        new GradeEntryWindow(row.LopId, row.MonHocId, row.NamHoc, row.HocKy)
        {
            Owner = this
        }.ShowDialog();
    }

    // ═══════════════════════════════════════════════════════════════
    //  TAB 2: QUẢN LÝ HỌC SINH
    // ═══════════════════════════════════════════════════════════════

    // Lưu danh sách các lớp để hiển thị trong combobox
    private List<LopItem> _lopItems = new();

    /// <summary>
    /// Khi tab "Học sinh" được tải, load danh sách lớp vào combobox
    /// </summary>
    private void StudentsTab_Loaded(object sender, RoutedEventArgs e) => _ = LoadLopFilterAsync();

    /// <summary>
    /// Nút "Tải lại" danh sách học sinh
    /// </summary>
    private void LoadStudents_Click(object sender, RoutedEventArgs e) => _ = LoadStudentsForSelectedLopAsync();

    /// <summary>
    /// Load danh sách các lớp mà giáo viên được phân công (để lọc)
    /// </summary>
    private async Task LoadLopFilterAsync()
    {
        if (_gvId is null) return;

        await using var db = new Models.QuanLyHocSinhContext();

        // Lấy tất cả phân công của giáo viên
        var pcs = await db.PhanCongs
            .AsNoTracking()
            .Include(p => p.Lop)
            .Where(p => p.GiaoVienId == _gvId.Value)
            .ToListAsync();

        // Lọc các lớp duy nhất (không trùng lặp)
        var seen = new HashSet<int>();
        _lopItems = pcs
            .Where(p => seen.Add(p.LopId))
            .Select(p => new LopItem(
                p.LopId,
                $"{p.Lop.TenLop}  (K{p.Lop.Khoi} – {p.Lop.NamHoc}){(p.IsChuNhiem ? "  ⭐ GVCN" : "")}",
                p.IsChuNhiem))
            .OrderByDescending(x => x.IsChuNhiem)  // Lớp chủ nhiệm lên đầu
            .ThenBy(x => x.Display)
            .ToList();

        // Gán vào combobox
        LopFilterCombo.ItemsSource = _lopItems;

        if (_lopItems.Count > 0)
            LopFilterCombo.SelectedIndex = 0;  // Chọn lớp đầu tiên
        else
        {
            LopInfoText.Text = "Chưa được phân công lớp nào.";
            RoleInfoText.Text = "";
        }
    }

    /// <summary>
    /// Khi chọn lớp khác trong combobox, load lại danh sách học sinh
    /// </summary>
    private void LopFilter_Changed(object sender, SelectionChangedEventArgs e)
        => _ = LoadStudentsForSelectedLopAsync();

    /// <summary>
    /// Load danh sách học sinh của lớp đã chọn
    /// </summary>
    private async Task LoadStudentsForSelectedLopAsync()
    {
        // Kiểm tra đã chọn lớp chưa
        if (LopFilterCombo.SelectedItem is not LopItem lop)
        {
            UpdateStudentToolbar(isChuNhiem: false);
            return;
        }

        await using var db = new Models.QuanLyHocSinhContext();

        // Kiểm tra xem lớp này có phải là lớp chủ nhiệm không (từ DB)
        bool isChuNhiem = await db.PhanCongs.AnyAsync(p =>
            p.GiaoVienId == _gvId && p.LopId == lop.LopId && p.IsChuNhiem);

        // Cập nhật trạng thái các nút (chỉ GVCN mới được thêm/sửa/xóa)
        UpdateStudentToolbar(isChuNhiem);

        // Lấy danh sách học sinh
        var hs = await db.HocSinhs
            .AsNoTracking()
            .Where(h => h.LopId == lop.LopId)
            .OrderBy(h => h.HoTen)
            .ToListAsync();

        // Lấy thông tin tài khoản của từng học sinh
        var hsIds = hs.Select(x => x.HocSinhId).ToList();
        var tkByHsId = await db.TaiKhoans
            .AsNoTracking()
            .Where(t => t.HocSinhId.HasValue && hsIds.Contains(t.HocSinhId.Value))
            .ToDictionaryAsync(t => t.HocSinhId!.Value, t => t.IsActive);

        // Hiển thị danh sách lên DataGrid
        StudentsGrid.ItemsSource = hs
            .Select(x => new StudentRow(
                x.HocSinhId,
                x.MaHocSinh,
                x.HoTen,
                x.NgaySinh,
                x.GioiTinh,
                x.SoDienThoai,
                x.SdtchaMe,
                x.TrangThai,
                tkByHsId.TryGetValue(x.HocSinhId, out var active) && active))
            .ToList();

        // Cập nhật thông tin lớp
        var l = await db.Lops
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.LopId == lop.LopId);

        LopInfoText.Text = l is null
            ? ""
            : $"{l.TenLop}  (Khối {l.Khoi} – {l.NamHoc})  |  {hs.Count} học sinh";

        RoleInfoText.Text = isChuNhiem
            ? "👑 Bạn là Giáo viên Chủ nhiệm lớp này – có thể thêm/sửa/xóa học sinh"
            : "📖 Giáo viên Bộ môn – chỉ xem danh sách, chấm điểm qua tab Phân công";
    }

    /// <summary>
    /// Cập nhật trạng thái các nút (bật/tắt) dựa vào quyền của giáo viên
    /// </summary>
    private void UpdateStudentToolbar(bool isChuNhiem)
    {
        // Chỉ GVCN mới được phép thêm/sửa/xóa học sinh
        BtnAddStudent.IsEnabled = isChuNhiem;
        BtnEditStudent.IsEnabled = isChuNhiem;
        BtnDeleteStudent.IsEnabled = isChuNhiem;
        BtnToggleStudentAccount.IsEnabled = isChuNhiem;
    }

    // ─────────────────────────────────────────────────────────────
    //  CÁC CHỨC NĂNG CRUD HỌC SINH (CHỈ GVCN)
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Thêm học sinh mới
    /// </summary>
    private void AddStudent_Click(object sender, RoutedEventArgs e)
    {
        if (LopFilterCombo.SelectedItem is not LopItem lop) return;
        var win = new StudentDetailWindow(forcedLopId: lop.LopId) { Owner = this };
        win.ShowDialog();
        if (win.Saved) _ = LoadStudentsForSelectedLopAsync();  // Tải lại danh sách
    }

    /// <summary>
    /// Sửa thông tin học sinh
    /// </summary>
    private void EditStudent_Click(object sender, RoutedEventArgs e)
    {
        if (StudentsGrid.SelectedItem is not StudentRow hs)
        {
            MessageBox.Show("Vui lòng chọn một học sinh để sửa.", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (LopFilterCombo.SelectedItem is not LopItem lop) return;

        _ = EditStudentAsync(hs.HocSinhId, lop.LopId);
    }

    private async Task EditStudentAsync(int hocSinhId, int lopId)
    {
        try
        {
            await using var db = new Models.QuanLyHocSinhContext();

            // Lấy thông tin học sinh từ database
            var entity = await db.HocSinhs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.HocSinhId == hocSinhId);

            if (entity is null)
            {
                MessageBox.Show("Không tìm thấy học sinh.", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Mở cửa sổ sửa thông tin
            var win = new StudentDetailWindow(entity, forcedLopId: lopId) { Owner = this };
            win.ShowDialog();

            if (win.Saved) _ = LoadStudentsForSelectedLopAsync();  // Tải lại danh sách
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void DeleteStudent_Click(object sender, RoutedEventArgs e)
    {
        if (StudentsGrid.SelectedItem is not StudentRow hs)
        {
            MessageBox.Show("Vui lòng chọn một học sinh để xóa.", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show($"Bạn có chắc chắn muốn xóa học sinh '{hs.HoTen}'?",
            "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        try
        {
            await using var db = new Models.QuanLyHocSinhContext();

            // Xóa tài khoản liên quan (nếu có)
            var tk = await db.TaiKhoans.FirstOrDefaultAsync(t => t.HocSinhId == hs.HocSinhId);
            if (tk is not null)
                db.TaiKhoans.Remove(tk);

            // Xóa học sinh
            var entity = await db.HocSinhs.FindAsync(hs.HocSinhId);
            if (entity is not null)
                db.HocSinhs.Remove(entity);

            await db.SaveChangesAsync();

            // Tải lại danh sách
            await LoadStudentsForSelectedLopAsync();

            MessageBox.Show("Xóa học sinh thành công!", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Khóa/Mở khóa tài khoản học sinh
    /// </summary>
    private async void ToggleStudentAccount_Click(object sender, RoutedEventArgs e)
    {
        // Kiểm tra đã chọn học sinh chưa
        if (StudentsGrid.SelectedItem is not StudentRow hs)
        {
            MessageBox.Show("Vui lòng chọn một học sinh.", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            await using var db = new Models.QuanLyHocSinhContext();

            // Tìm tài khoản của học sinh
            var tk = await db.TaiKhoans.SingleOrDefaultAsync(t => t.HocSinhId == hs.HocSinhId);

            if (tk is null)
            {
                MessageBox.Show("Học sinh này chưa có tài khoản.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Xác định hành động: khóa hay mở khóa
            bool willDisable = tk.IsActive;
            string action = willDisable ? "khóa" : "mở khóa";

            // Xác nhận
            if (MessageBox.Show($"Bạn có chắc muốn {action} tài khoản của '{hs.HoTen}'?",
                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            // Thực hiện khóa/mở khóa
            tk.IsActive = !tk.IsActive;
            await db.SaveChangesAsync();

            // Thông báo kết quả
            MessageBox.Show(willDisable ? "Đã khóa tài khoản." : "Đã mở khóa tài khoản.",
                "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);

            // Tải lại danh sách
            await LoadStudentsForSelectedLopAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  CHỨC NĂNG CHUNG
    // ═══════════════════════════════════════════════════════════════

    private void ChangePassword_Click(object sender, RoutedEventArgs e)
        => new ChangePasswordWindow { Owner = this }.ShowDialog();

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        // Xác nhận đăng xuất
        if (MessageBox.Show("Bạn có chắc muốn đăng xuất?", "Xác nhận đăng xuất",
            MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        // Xóa session và quay lại màn hình đăng nhập
        Session.Clear();
        new LoginWindow().Show();
        Close();
    }
}