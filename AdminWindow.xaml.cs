namespace QuanLyHocSinh;

using Microsoft.EntityFrameworkCore;
using QuanLyHocSinh.Models;
using QuanLyHocSinh.Services;
using System.Linq;
using System.Windows;

public partial class AdminWindow : Window
{
    private readonly StatisticsService _stats = new();

    // ─────────────────────────────────────────────────────────────
    //  DỮ LIỆU HIỂN THỊ
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Dòng dữ liệu hiển thị thông tin giáo viên trên DataGrid
    /// </summary>
    private sealed record TeacherRow(
        int GiaoVienId,
        string MaGiaoVien,
        string HoTen,
        string GioiTinh,
        string? ChuyenMon,
        string? SoDienThoai,
        string? Email,
        string TrangThai,
        bool IsAccountActive);

    // ─────────────────────────────────────────────────────────────
    //  KHỞI TẠO
    // ─────────────────────────────────────────────────────────────

    public AdminWindow()
    {
        InitializeComponent();

        // Hiển thị tên admin đang đăng nhập
        InfoText.Text = $"Admin: {Session.CurrentUser?.Username}";

        // Tự động load thống kê khi mở cửa sổ
        _ = LoadStatsAsync();
    }

    // ═══════════════════════════════════════════════════════════════
    //  TAB 1: THỐNG KÊ
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Load dữ liệu thống kê (tổng số học sinh, tổng số giáo viên)
    /// </summary>
    private async Task LoadStatsAsync()
    {
        await using var db = new QuanLyHocSinhContext();

        // Đếm số học sinh đang học
        TotalStudentText.Text = (await db.HocSinhs.AsNoTracking().CountAsync(hs => hs.TrangThai == "Đang học")).ToString();

        // Đếm số giáo viên đang giảng dạy
        TotalTeacherText.Text = (await db.GiaoViens.AsNoTracking().CountAsync(gv => gv.TrangThai == "Đang giảng dạy")).ToString();
    }

    /// <summary>
    /// Nút "Tải lại" thống kê
    /// </summary>
    private void LoadStats_Click(object sender, RoutedEventArgs e) => _ = LoadStatsAsync();

    /// <summary>
    /// Mở cửa sổ xét lên lớp cho học sinh
    /// </summary>
    private void OpenPromotion_Click(object sender, RoutedEventArgs e)
    {
        var win = new PromotionWindow { Owner = this };
        win.ShowDialog();
    }

    // ═══════════════════════════════════════════════════════════════
    //  TAB 2: QUẢN LÝ GIÁO VIÊN
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Load danh sách giáo viên từ database
    /// </summary>
    private async Task LoadTeachersAsync()
    {
        await using var db = new QuanLyHocSinhContext();

        // Lấy danh sách giáo viên
        var gvList = await db.GiaoViens
            .AsNoTracking()
            .OrderBy(x => x.GiaoVienId)
            .ToListAsync();

        // Lấy thông tin tài khoản của từng giáo viên
        var gvIds = gvList.Select(x => x.GiaoVienId).ToList();
        var tkByGvId = await db.TaiKhoans
            .AsNoTracking()
            .Where(t => t.GiaoVienId.HasValue && gvIds.Contains(t.GiaoVienId.Value))
            .ToDictionaryAsync(t => t.GiaoVienId!.Value, t => t.IsActive);

        // Hiển thị danh sách lên DataGrid
        TeachersGrid.ItemsSource = gvList
            .Select(gv => new TeacherRow(
                gv.GiaoVienId,
                gv.MaGiaoVien,
                gv.HoTen,
                gv.GioiTinh ?? string.Empty,
                gv.ChuyenMon,
                gv.SoDienThoai,
                gv.Email,
                gv.TrangThai,
                tkByGvId.TryGetValue(gv.GiaoVienId, out var active) && active))
            .ToList();
    }

    /// <summary>
    /// Khi tab "Giáo viên" được tải, tự động load danh sách
    /// </summary>
    private void TeachersTab_Loaded(object sender, RoutedEventArgs e) => _ = LoadTeachersAsync();

    /// <summary>
    /// Nút "Tải lại" danh sách giáo viên
    /// </summary>
    private void LoadTeachers_Click(object sender, RoutedEventArgs e) => _ = LoadTeachersAsync();

    /// <summary>
    /// Thêm giáo viên mới
    /// </summary>
    private void AddTeacher_Click(object sender, RoutedEventArgs e)
    {
        var win = new TeacherDetailWindow { Owner = this };
        win.ShowDialog();
        if (win.Saved) _ = LoadTeachersAsync();  // Tải lại danh sách nếu đã lưu
    }

    private void EditTeacher_Click(object sender, RoutedEventArgs e)
    {
        if (TeachersGrid.SelectedItem is not TeacherRow row)
        {
            MessageBox.Show("Vui lòng chọn một giáo viên để sửa.", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _ = EditTeacherAsync(row.GiaoVienId);
    }

    private async Task EditTeacherAsync(int giaoVienId)
    {
        try
        {
            await using var db = new QuanLyHocSinhContext();

            // Lấy thông tin giáo viên từ database
            var gv = await db.GiaoViens
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.GiaoVienId == giaoVienId);

            if (gv is null)
            {
                MessageBox.Show("Không tìm thấy giáo viên.", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Mở cửa sổ sửa thông tin
            var win = new TeacherDetailWindow(gv) { Owner = this };
            win.ShowDialog();

            if (win.Saved) _ = LoadTeachersAsync();  // Tải lại danh sách nếu đã lưu
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void DeleteTeacher_Click(object sender, RoutedEventArgs e)
    {
        if (TeachersGrid.SelectedItem is not TeacherRow gv)
        {
            MessageBox.Show("Vui lòng chọn một giáo viên để xóa.", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show($"Bạn có chắc chắn muốn xóa giáo viên '{gv.HoTen}'?",
            "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        try
        {
            await using var db = new QuanLyHocSinhContext();

            // Nếu ID = 0, nghĩa là giáo viên chưa lưu trong DB (chỉ tồn tại trên grid)
            if (gv.GiaoVienId == 0)
            {
                var list = (TeachersGrid.ItemsSource as IEnumerable<TeacherRow>)?.ToList()
                    ?? new List<TeacherRow>();
                list.Remove(gv);
                TeachersGrid.ItemsSource = list;
                return;
            }

            // Xóa tài khoản liên quan (nếu có)
            var taiKhoan = await db.TaiKhoans
                .FirstOrDefaultAsync(t => t.GiaoVienId == gv.GiaoVienId);
            if (taiKhoan is not null)
                db.TaiKhoans.Remove(taiKhoan);

            // Xóa giáo viên
            var entity = await db.GiaoViens.FindAsync(gv.GiaoVienId);
            if (entity is not null)
                db.GiaoViens.Remove(entity);

            await db.SaveChangesAsync();
            await LoadTeachersAsync();  // Tải lại danh sách

            MessageBox.Show("Xóa giáo viên thành công!", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Lưu tất cả thay đổi của giáo viên (cho grid edit trực tiếp)
    /// </summary>
    private async void SaveTeachers_Click(object sender, RoutedEventArgs e)
    {
        if (TeachersGrid.ItemsSource is not IEnumerable<GiaoVien> items)
            return;

        try
        {
            await using var db = new QuanLyHocSinhContext();

            // Duyệt qua từng giáo viên trong grid
            foreach (var gv in items)
            {
                if (gv.GiaoVienId == 0) db.GiaoViens.Add(gv);    // Thêm mới
                else db.GiaoViens.Update(gv);   // Cập nhật
            }

            await db.SaveChangesAsync();
            MessageBox.Show("Đã lưu thông tin giáo viên.", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Information);
            await LoadTeachersAsync();  // Tải lại danh sách
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Xem và reset mật khẩu của giáo viên
    /// </summary>
    private void ViewTeacherPassword_Click(object sender, RoutedEventArgs e)
    {
        if (TeachersGrid.SelectedItem is not TeacherRow gv)
        {
            MessageBox.Show("Vui lòng chọn một giáo viên.", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _ = ShowAndResetGVPasswordAsync(gv.GiaoVienId, gv.HoTen);
    }

    /// <summary>
    /// Hiển thị cửa sổ reset mật khẩu cho giáo viên
    /// </summary>
    private async Task ShowAndResetGVPasswordAsync(int giaoVienId, string hoTen)
    {
        await using var db = new QuanLyHocSinhContext();

        // Tìm tài khoản của giáo viên
        var tk = await db.TaiKhoans
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.GiaoVienId == giaoVienId);

        if (tk is null)
        {
            MessageBox.Show("Giáo viên này chưa có tài khoản.", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Mở cửa sổ reset mật khẩu
        var reset = new ResetPasswordWindow(tk.TaiKhoanId, hoTen) { Owner = this };
        reset.ShowDialog();
    }

    /// <summary>
    /// Khóa/Mở khóa tài khoản giáo viên
    /// </summary>
    private async void ToggleTeacherAccount_Click(object sender, RoutedEventArgs e)
    {
        // Kiểm tra đã chọn giáo viên chưa
        if (TeachersGrid.SelectedItem is not TeacherRow gv)
        {
            MessageBox.Show("Vui lòng chọn một giáo viên.", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            await using var db = new QuanLyHocSinhContext();

            // Tìm tài khoản của giáo viên
            var tk = await db.TaiKhoans
                .SingleOrDefaultAsync(t => t.GiaoVienId == gv.GiaoVienId);

            if (tk is null)
            {
                MessageBox.Show("Giáo viên này chưa có tài khoản.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Xác định hành động: khóa hay mở khóa
            bool willDisable = tk.IsActive;
            string action = willDisable ? "khóa" : "mở khóa";

            // Xác nhận
            if (MessageBox.Show($"Bạn có chắc muốn {action} tài khoản của '{gv.HoTen}'?",
                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            // Thực hiện khóa/mở khóa
            tk.IsActive = !tk.IsActive;
            await db.SaveChangesAsync();

            MessageBox.Show(willDisable ? "Đã khóa tài khoản." : "Đã mở khóa tài khoản.",
                "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            await LoadTeachersAsync();  // Tải lại danh sách
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  TAB 3: QUẢN LÝ LỚP HỌC
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Load danh sách lớp học từ database
    /// </summary>
    private async Task LoadClassesAsync()
    {
        await using var db = new QuanLyHocSinhContext();

        ClassesGrid.ItemsSource = await db.Lops
            .AsNoTracking()
            .OrderByDescending(x => x.NamHoc)
            .ThenBy(x => x.Khoi)
            .ThenBy(x => x.TenLop)
            .ToListAsync();
    }

    /// <summary>
    /// Khi tab "Lớp học" được tải, tự động load danh sách
    /// </summary>
    private void ClassesTab_Loaded(object sender, RoutedEventArgs e)
        => _ = LoadClassesAsync();

    /// <summary>
    /// Nút "Tải lại" danh sách lớp học
    /// </summary>
    private void LoadClasses_Click(object sender, RoutedEventArgs e)
        => _ = LoadClassesAsync();

    /// <summary>
    /// Thêm lớp học mới (trên grid, chưa lưu vào DB)
    /// </summary>
    private void AddClass_Click(object sender, RoutedEventArgs e)
    {
        // Lấy danh sách hiện tại
        var list = (ClassesGrid.ItemsSource as IEnumerable<Lop>)?.ToList()
            ?? new List<Lop>();

        // Thêm lớp mới với giá trị mặc định
        list.Add(new Lop
        {
            TenLop = "10A1",
            Khoi = 10,
            NamHoc = $"{DateTime.Now.Year}-{DateTime.Now.Year + 1}",
            IsActive = true
        });

        // Cập nhật lại grid
        ClassesGrid.ItemsSource = new List<Lop>(list);
    }

    /// <summary>
    /// Xóa lớp học
    /// </summary>
    private async void DeleteClass_Click(object sender, RoutedEventArgs e)
    {
        // Kiểm tra đã chọn lớp chưa
        if (ClassesGrid.SelectedItem is not Lop lop)
        {
            MessageBox.Show("Vui lòng chọn một lớp để xóa.", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Nếu lớp chưa lưu trong DB (ID = 0), chỉ xóa khỏi grid
        if (lop.LopId == 0)
        {
            var list = (ClassesGrid.ItemsSource as IEnumerable<Lop>)?.ToList()
                ?? new List<Lop>();
            list.Remove(lop);
            ClassesGrid.ItemsSource = new List<Lop>(list);
            return;
        }

        // Xác nhận xóa
        if (MessageBox.Show($"Bạn có chắc chắn muốn xóa lớp '{lop.TenLop}'?",
            "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        try
        {
            await using var db = new QuanLyHocSinhContext();

            // Tìm và xóa lớp
            var entity = await db.Lops.FindAsync(lop.LopId);
            if (entity is not null)
                db.Lops.Remove(entity);

            await db.SaveChangesAsync();
            await LoadClassesAsync();  // Tải lại danh sách

            MessageBox.Show("Xóa lớp học thành công!", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Lưu tất cả thay đổi của lớp học (cho grid edit trực tiếp)
    /// </summary>
    private async void SaveClasses_Click(object sender, RoutedEventArgs e)
    {
        if (ClassesGrid.ItemsSource is not IEnumerable<Lop> items)
            return;

        try
        {
            await using var db = new QuanLyHocSinhContext();

            // Duyệt qua từng lớp trong grid
            foreach (var lop in items)
            {
                if (lop.LopId == 0)
                    db.Lops.Add(lop);      // Thêm mới
                else
                    db.Lops.Update(lop);   // Cập nhật
            }

            await db.SaveChangesAsync();
            MessageBox.Show("Đã lưu thông tin lớp học.", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Information);
            await LoadClassesAsync();  // Tải lại danh sách
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  TAB 4: QUẢN LÝ MÔN HỌC
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Load danh sách môn học từ database
    /// </summary>
    private async Task LoadSubjectsAsync()
    {
        await using var db = new QuanLyHocSinhContext();

        SubjectsGrid.ItemsSource = await db.MonHocs
            .AsNoTracking()
            .OrderBy(x => x.MaMh)  // Sắp xếp theo mã môn
            .ToListAsync();
    }

    /// <summary>
    /// Khi tab "Môn học" được tải, tự động load danh sách
    /// </summary>
    private void SubjectsTab_Loaded(object sender, RoutedEventArgs e)
        => _ = LoadSubjectsAsync();

    /// <summary>
    /// Nút "Tải lại" danh sách môn học
    /// </summary>
    private void LoadSubjects_Click(object sender, RoutedEventArgs e)
        => _ = LoadSubjectsAsync();

    /// <summary>
    /// Thêm môn học mới (trên grid, chưa lưu vào DB)
    /// </summary>
    private void AddSubject_Click(object sender, RoutedEventArgs e)
    {
        // Lấy danh sách hiện tại
        var list = (SubjectsGrid.ItemsSource as IEnumerable<MonHoc>)?.ToList()
            ?? new List<MonHoc>();

        // Thêm môn mới với giá trị mặc định
        list.Add(new MonHoc
        {
            MaMh = $"MH{list.Count + 1:D3}",  // MH001, MH002,...
            TenMh = "Môn mới",
            HeSo = 1
        });

        // Cập nhật lại grid
        SubjectsGrid.ItemsSource = new List<MonHoc>(list);
    }

    /// <summary>
    /// Xóa môn học
    /// </summary>
    private async void DeleteSubject_Click(object sender, RoutedEventArgs e)
    {
        // Kiểm tra đã chọn môn chưa
        if (SubjectsGrid.SelectedItem is not MonHoc mh)
        {
            MessageBox.Show("Vui lòng chọn một môn học để xóa.", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Nếu môn chưa lưu trong DB (ID = 0), chỉ xóa khỏi grid
        if (mh.MonHocId == 0)
        {
            var list = (SubjectsGrid.ItemsSource as IEnumerable<MonHoc>)?.ToList()
                ?? new List<MonHoc>();
            list.Remove(mh);
            SubjectsGrid.ItemsSource = new List<MonHoc>(list);
            return;
        }

        // Xác nhận xóa
        if (MessageBox.Show($"Bạn có chắc chắn muốn xóa môn '{mh.TenMh}'?",
            "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        try
        {
            await using var db = new QuanLyHocSinhContext();

            // Tìm và xóa môn học
            var entity = await db.MonHocs.FindAsync(mh.MonHocId);
            if (entity is not null)
                db.MonHocs.Remove(entity);

            await db.SaveChangesAsync();
            await LoadSubjectsAsync();  // Tải lại danh sách

            MessageBox.Show("Xóa môn học thành công!", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Lưu tất cả thay đổi của môn học (cho grid edit trực tiếp)
    /// </summary>
    private async void SaveSubjects_Click(object sender, RoutedEventArgs e)
    {
        if (SubjectsGrid.ItemsSource is not IEnumerable<MonHoc> items)
            return;

        try
        {
            await using var db = new QuanLyHocSinhContext();

            // Duyệt qua từng môn trong grid
            foreach (var mh in items)
            {
                if (mh.MonHocId == 0)
                    db.MonHocs.Add(mh);      // Thêm mới
                else
                    db.MonHocs.Update(mh);   // Cập nhật
            }

            await db.SaveChangesAsync();
            MessageBox.Show("Đã lưu thông tin môn học.", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Information);
            await LoadSubjectsAsync();  // Tải lại danh sách
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  TAB 5: PHÂN CÔNG GIẢNG DẠY
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Lấy danh sách phân công từ database
    /// </summary>
    private async Task<List<AssignmentRow>> FetchAssignmentsAsync()
    {
        await using var db = new QuanLyHocSinhContext();

        // Lấy danh sách phân công kèm thông tin lớp, môn, giáo viên
        var list = await db.PhanCongs
            .AsNoTracking()
            .Include(p => p.Lop)
            .Include(p => p.MonHoc)
            .Include(p => p.GiaoVien)
            .OrderByDescending(p => p.NamHoc)
            .ThenBy(p => p.HocKy)
            .ThenBy(p => p.Lop.TenLop)
            .ToListAsync();

        // Chuyển đổi sang format hiển thị
        return list.Select(p => new AssignmentRow
        {
            PhanCongId = p.PhanCongId,
            TenLop = p.Lop.TenLop,
            TenMH = p.MonHoc.TenMh,
            TenGV = p.GiaoVien.HoTen,
            NamHoc = p.NamHoc,
            HocKy = p.HocKy,
            IsChuNhiem = p.IsChuNhiem,
            LopId = p.LopId,
            MonHocId = p.MonHocId,
            GiaoVienId = p.GiaoVienId
        }).ToList();
    }

    /// <summary>
    /// Khi tab "Phân công" được tải, tự động load danh sách
    /// </summary>
    private void AssignmentsTab_Loaded(object sender, RoutedEventArgs e)
        => _ = LoadAssignmentsAsync();

    /// <summary>
    /// Nút "Tải lại" danh sách phân công
    /// </summary>
    private void LoadAssignments_Click(object sender, RoutedEventArgs e)
        => _ = LoadAssignmentsAsync();

    /// <summary>
    /// Load danh sách phân công lên DataGrid
    /// </summary>
    private async Task LoadAssignmentsAsync()
    {
        AssignmentsGrid.ItemsSource = await FetchAssignmentsAsync();
    }

    /// <summary>
    /// Thêm phân công mới
    /// </summary>
    private void AddAssignment_Click(object sender, RoutedEventArgs e)
    {
        var win = new AssignmentEditWindow { Owner = this };
        win.ShowDialog();

        if (win.Saved)
            _ = LoadAssignmentsAsync();  // Tải lại danh sách nếu đã lưu
    }

    /// <summary>
    /// Xóa phân công
    /// </summary>
    private async void DeleteAssignment_Click(object sender, RoutedEventArgs e)
    {
        // Kiểm tra đã chọn phân công chưa
        if (AssignmentsGrid.SelectedItem is not AssignmentRow row)
        {
            MessageBox.Show("Vui lòng chọn một phân công để xóa.", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Xác nhận xóa
        if (MessageBox.Show("Bạn có chắc chắn muốn xóa phân công này?",
            "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        try
        {
            await using var db = new QuanLyHocSinhContext();

            // Tìm và xóa phân công
            var entity = await db.PhanCongs.FindAsync(row.PhanCongId);
            if (entity is not null)
            {
                db.PhanCongs.Remove(entity);
                await db.SaveChangesAsync();
            }

            await LoadAssignmentsAsync();  // Tải lại danh sách

            MessageBox.Show("Xóa phân công thành công!", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Lưu thay đổi của phân công (chỉ cho phép sửa IsChuNhiem, NamHoc, HocKy)
    /// </summary>
    private async void SaveAssignments_Click(object sender, RoutedEventArgs e)
    {
        if (AssignmentsGrid.ItemsSource is not IEnumerable<AssignmentRow> items)
            return;

        try
        {
            await using var db = new QuanLyHocSinhContext();

            // Duyệt qua từng phân công trong grid
            foreach (var row in items)
            {
                var pc = await db.PhanCongs.FindAsync(row.PhanCongId);
                if (pc is null) continue;  // Không tìm thấy, bỏ qua

                // Chỉ cập nhật các trường được phép
                pc.IsChuNhiem = row.IsChuNhiem;  // Cập nhật trạng thái chủ nhiệm

                if (!string.IsNullOrWhiteSpace(row.NamHoc))
                    pc.NamHoc = row.NamHoc.Trim();  // Cập nhật năm học

                if (row.HocKy is 1 or 2)
                    pc.HocKy = row.HocKy;  // Cập nhật học kỳ
            }

            await db.SaveChangesAsync();
            MessageBox.Show("Đã lưu thông tin phân công.", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Information);
            await LoadAssignmentsAsync();  // Tải lại danh sách
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

    /// <summary>
    /// Đổi mật khẩu của admin
    /// </summary>
    private void ChangePassword_Click(object sender, RoutedEventArgs e)
        => new ChangePasswordWindow { Owner = this }.ShowDialog();

    /// <summary>
    /// Đăng xuất
    /// </summary>
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

/// <summary>
/// Lớp dữ liệu hiển thị phân công trên DataGrid
/// </summary>
public sealed class AssignmentRow
{
    public int PhanCongId { get; init; }
    public string TenLop { get; init; } = "";
    public string TenMH { get; init; } = "";
    public string TenGV { get; init; } = "";
    public int LopId { get; init; }
    public int MonHocId { get; init; }
    public int GiaoVienId { get; init; }
    public string NamHoc { get; set; } = "";
    public byte HocKy { get; set; }
    public bool IsChuNhiem { get; set; }
}