namespace QuanLyHocSinh;

using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using QuanLyHocSinh.Services;

public partial class PromotionWindow : Window
{
    private readonly PromotionService _promotionService = new();
    private string? _lastPreviewNamHoc;
    private int _lastPreviewTotal;
    private int _lastPreviewLenLop;
    private int _lastPreviewOLai;
    private int _lastPreviewTotNghiep;

    public PromotionWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadNamHocAsync();
    }

    private async Task LoadNamHocAsync()
    {
        await using var db = new Models.QuanLyHocSinhContext();
        var namHocList = await db.Lops.AsNoTracking()
            .Select(l => l.NamHoc)
            .Distinct()
            .OrderByDescending(n => n)
            .ToListAsync();
        NamHocCombo.ItemsSource = namHocList;
        if (namHocList.Count > 0) NamHocCombo.SelectedIndex = 0;
    }

    private async void Preview_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (NamHocCombo.SelectedItem is not string namHoc)
            {
                MessageBox.Show("Vui lòng chọn năm học.");
                return;
            }

            StatusText.Text = "Đang tải...";
            PreviewGrid.ItemsSource = null;
            ApplyBtn.IsEnabled = false;

            var results = await _promotionService.PreviewAsync(namHoc);
            PreviewGrid.ItemsSource = results;

            int lenLop = results.Count(r => r.KetQua == "Lên lớp");
            int oLai = results.Count(r => r.KetQua == "Ở lại");
            int totNghiep = results.Count(r => r.KetQua == "Tốt nghiệp");

            _lastPreviewNamHoc = namHoc;
            _lastPreviewTotal = results.Count;
            _lastPreviewLenLop = lenLop;
            _lastPreviewOLai = oLai;
            _lastPreviewTotNghiep = totNghiep;

            SummaryText.Text = $"Tổng: {results.Count} | Lên lớp: {lenLop} | Ở lại: {oLai} | Tốt nghiệp: {totNghiep}";
            StatusText.Text = string.Empty;

            if (results.Count > 0)
                ApplyBtn.IsEnabled = true;
        }
        catch (Exception ex)
        {
            StatusText.Text = string.Empty;
            ApplyBtn.IsEnabled = false;
            PreviewGrid.ItemsSource = null;
            MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Apply_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (NamHocCombo.SelectedItem is not string namHoc) return;

            if (_lastPreviewNamHoc != namHoc || _lastPreviewTotal <= 0)
            {
                MessageBox.Show("Vui lòng bấm 'Xem trước kết quả' trước khi áp dụng.",
                    "Chưa có dữ liệu xem trước", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string? namHocMoi = null;
            var parts = namHoc.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[0], out int yearStart))
                namHocMoi = $"{yearStart + 1}-{yearStart + 2}";

            var confirm1 = MessageBox.Show(
                $"Bạn sắp ÁP DỤNG LÊN LỚP cho năm học: {namHoc}" +
                (namHocMoi is null ? "" : $"\nNăm học mới sẽ là: {namHocMoi}") +
                $"\n\nTổng: {_lastPreviewTotal}" +
                $"\n• Lên lớp: {_lastPreviewLenLop}" +
                $"\n• Ở lại: {_lastPreviewOLai}" +
                $"\n• Tốt nghiệp: {_lastPreviewTotNghiep}" +
                "\n\nBạn có muốn tiếp tục?",
                "Xác nhận lần 1", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm1 != MessageBoxResult.Yes) return;

            var confirm2 = MessageBox.Show(
                "⚠️ Thao tác này sẽ CẬP NHẬT CSDL và KHÔNG THỂ HOÀN TÁC.\nBạn chắc chắn muốn áp dụng lên lớp?",
                "Xác nhận lần 2", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm2 != MessageBoxResult.Yes) return;

            ApplyBtn.IsEnabled = false;
            StatusText.Text = "Đang xử lý...";

            var (lenLop, oLai, totNghiep, error) = await _promotionService.ApplyAsync(namHoc);

            if (!string.IsNullOrEmpty(error))
            {
                MessageBox.Show(error, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = string.Empty;
                return;
            }

            MessageBox.Show(
                $"Xử lý hoàn tất!\n• Lên lớp: {lenLop} học sinh\n• Ở lại: {oLai} học sinh\n• Tốt nghiệp: {totNghiep} học sinh",
                "Hoàn thành", MessageBoxButton.OK, MessageBoxImage.Information);

            StatusText.Text = "Đã áp dụng.";
            // Reload preview
            await Preview_Click_Async(namHoc);
        }
        catch (Exception ex)
        {
            StatusText.Text = string.Empty;
            MessageBox.Show($"Lỗi áp dụng lên lớp: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task Preview_Click_Async(string namHoc)
    {
        try
        {
            var results = await _promotionService.PreviewAsync(namHoc);
            PreviewGrid.ItemsSource = results;
            int lenLop = results.Count(r => r.KetQua == "Lên lớp");
            int oLai = results.Count(r => r.KetQua == "Ở lại");
            int totNghiep = results.Count(r => r.KetQua == "Tốt nghiệp");
            SummaryText.Text = $"Tổng: {results.Count} | Lên lớp: {lenLop} | Ở lại: {oLai} | Tốt nghiệp: {totNghiep}";
        }
        catch
        {
            // Ignore background refresh errors
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}

public sealed class BoolToYesNoConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && b ? "✓ Đạt" : "✗ Chưa đạt";
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
