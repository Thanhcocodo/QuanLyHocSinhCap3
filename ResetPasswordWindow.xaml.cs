namespace QuanLyHocSinh;

using System.Windows;
using QuanLyHocSinh.Services;

public partial class ResetPasswordWindow : Window
{
    private readonly int _taiKhoanId;
    private readonly PasswordService _svc = new();

    public ResetPasswordWindow(int taiKhoanId, string tenNguoiDung)
    {
        _taiKhoanId = taiKhoanId;
        InitializeComponent();
        TitleText.Text = $"Reset mật khẩu: {tenNguoiDung}";
    }

    // ── Toggle ──
    private void ShowBtn_Checked(object sender, RoutedEventArgs e)
    { NewPasswordPlain.Text = NewPassword.Password; NewPasswordPlain.Visibility = Visibility.Visible; NewPassword.Visibility = Visibility.Collapsed; }

    private void ShowBtn_Unchecked(object sender, RoutedEventArgs e)
    { NewPassword.Password = NewPasswordPlain.Text; NewPassword.Visibility = Visibility.Visible; NewPasswordPlain.Visibility = Visibility.Collapsed; }

    private string GetPassword() => NewPassword.Visibility == Visibility.Visible ? NewPassword.Password : NewPasswordPlain.Text;

    private async void Confirm_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = string.Empty;
        var newPass = GetPassword();

        if (string.IsNullOrWhiteSpace(newPass))
        { ErrorText.Text = "Mật khẩu không được trống."; return; }

        if (MessageBox.Show("Bạn có chắc muốn reset mật khẩu tài khoản này?", "Xác nhận reset mật khẩu",
            MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        var (ok, message) = await _svc.ResetPasswordAsync(_taiKhoanId, newPass);
        if (ok)
        {
            MessageBox.Show(message, "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        else
        {
            ErrorText.Text = message;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
}
