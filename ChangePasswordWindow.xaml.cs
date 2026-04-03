namespace QuanLyHocSinh;

using System.Windows;
using QuanLyHocSinh.Services;

public partial class ChangePasswordWindow : Window
{
    private readonly PasswordService _svc = new();

    public ChangePasswordWindow()
    {
        InitializeComponent();
    }

    // ── Toggle: Mật khẩu cũ ──
    private void ShowOld_Checked(object sender, RoutedEventArgs e)
    { OldPasswordPlain.Text = OldPassword.Password; OldPasswordPlain.Visibility = Visibility.Visible; OldPassword.Visibility = Visibility.Collapsed; }
    private void ShowOld_Unchecked(object sender, RoutedEventArgs e)
    { OldPassword.Password = OldPasswordPlain.Text; OldPassword.Visibility = Visibility.Visible; OldPasswordPlain.Visibility = Visibility.Collapsed; }

    // ── Toggle: Mật khẩu mới ──
    private void ShowNew_Checked(object sender, RoutedEventArgs e)
    { NewPasswordPlain.Text = NewPassword.Password; NewPasswordPlain.Visibility = Visibility.Visible; NewPassword.Visibility = Visibility.Collapsed; }
    private void ShowNew_Unchecked(object sender, RoutedEventArgs e)
    { NewPassword.Password = NewPasswordPlain.Text; NewPassword.Visibility = Visibility.Visible; NewPasswordPlain.Visibility = Visibility.Collapsed; }

    // ── Toggle: Xác nhận mật khẩu ──
    private void ShowNew2_Checked(object sender, RoutedEventArgs e)
    { NewPassword2Plain.Text = NewPassword2.Password; NewPassword2Plain.Visibility = Visibility.Visible; NewPassword2.Visibility = Visibility.Collapsed; }
    private void ShowNew2_Unchecked(object sender, RoutedEventArgs e)
    { NewPassword2.Password = NewPassword2Plain.Text; NewPassword2.Visibility = Visibility.Visible; NewPassword2Plain.Visibility = Visibility.Collapsed; }

    private string GetOld() => OldPassword.Visibility == Visibility.Visible ? OldPassword.Password : OldPasswordPlain.Text;
    private string GetNew() => NewPassword.Visibility == Visibility.Visible ? NewPassword.Password : NewPasswordPlain.Text;
    private string GetNew2() => NewPassword2.Visibility == Visibility.Visible ? NewPassword2.Password : NewPassword2Plain.Text;

    private async void Change_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = string.Empty;
        var user = Session.CurrentUser;
        if (user is null) { ErrorText.Text = "Bạn chưa đăng nhập."; return; }

        string oldPw = GetOld();
        string newPw = GetNew();
        string confirmPw = GetNew2();

        // Validate
        if (string.IsNullOrEmpty(oldPw))
        { ErrorText.Text = "Vui lòng nhập mật khẩu cũ."; return; }
        if (string.IsNullOrEmpty(newPw))
        { ErrorText.Text = "Vui lòng nhập mật khẩu mới."; return; }
        if (newPw.Length < 6)
        { ErrorText.Text = "Mật khẩu mới phải có ít nhất 6 ký tự."; return; }
        if (newPw != confirmPw)
        { ErrorText.Text = "Xác nhận mật khẩu không khớp."; return; }
        if (newPw == oldPw)
        { ErrorText.Text = "Mật khẩu mới phải khác mật khẩu cũ."; return; }

        if (MessageBox.Show("Bạn có chắc muốn đổi mật khẩu?", "Xác nhận đổi mật khẩu",
            MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        var (ok, msg) = await _svc.ChangePasswordAsync(user.TaiKhoanId, oldPw, newPw);
        ErrorText.Foreground = ok ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;
        ErrorText.Text = msg;
        if (ok)
        {
            await Task.Delay(900);
            Close();
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
}
