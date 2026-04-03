namespace QuanLyHocSinh;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using QuanLyHocSinh.Services;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => UsernameTextBox.Focus();
    }

    // ── Toggle mật khẩu ──
    private void ShowPassBtn_Checked(object sender, RoutedEventArgs e)
    {
        PasswordPlain.Text = PasswordBox.Password;
        PasswordPlain.Visibility = Visibility.Visible;
        PasswordBox.Visibility = Visibility.Collapsed;
    }

    private void ShowPassBtn_Unchecked(object sender, RoutedEventArgs e)
    {
        PasswordBox.Password = PasswordPlain.Text;
        PasswordBox.Visibility = Visibility.Visible;
        PasswordPlain.Visibility = Visibility.Collapsed;
    }

    private void Input_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) Login_Click(sender, e);
    }

    private async void Login_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = string.Empty;
        LoginBtn.IsEnabled = false;

        string username = UsernameTextBox.Text.Trim();
        string password = PasswordBox.Visibility == Visibility.Visible
            ? PasswordBox.Password
            : PasswordPlain.Text;

        if (string.IsNullOrEmpty(username))
        { ErrorText.Text = "Vui lòng nhập tài khoản."; LoginBtn.IsEnabled = true; return; }
        if (string.IsNullOrEmpty(password))
        { ErrorText.Text = "Vui lòng nhập mật khẩu."; LoginBtn.IsEnabled = true; return; }

        try
        {
            var svc = new AuthService();
            var (taiKhoan, isDisabled) = await svc.LoginAsync(username, password);

            if (taiKhoan is null)
            {
                ErrorText.Text = isDisabled
                    ? "Tài khoản đã bị vô hiệu hóa."
                    : "Tài khoản hoặc mật khẩu không đúng.";
                LoginBtn.IsEnabled = true;
                return;
            }

            Session.CurrentUser = taiKhoan;

            Window next = taiKhoan.Role switch
            {
                1 => new AdminWindow(),
                2 => new TeacherWindow(),
                _ => new StudentWindow()
            };
            next.Show();
            Close();
        }
        catch (Exception ex)
        {
            ErrorText.Text = $"Lỗi kết nối: {ex.Message}";
            LoginBtn.IsEnabled = true;
        }
    }
}
