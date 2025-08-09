using System;
using System.Data.SqlClient;
using System.IO;
using System.IO.IsolatedStorage;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Configuration;
using System.Windows.Input;

namespace VizitShop
{
    public partial class LoginWindow : Window
    {
        private string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        private const int RememberMeHours = 2;
        private const string CryptoKey = "SecureKey123!";

        public LoginWindow()
        {
            InitializeComponent();
            TryAutoLogin();
            usernameTextBox.Focus();
        }

        private void TryAutoLogin()
        {
            try
            {
                using (var storage = IsolatedStorageFile.GetUserStoreForAssembly())
                {
                    if (storage.FileExists("auth.dat"))
                    {
                        using (var stream = new IsolatedStorageFileStream("auth.dat", FileMode.Open, storage))
                        using (var reader = new StreamReader(stream))
                        {
                            var data = reader.ReadToEnd().Split('|');
                            if (data.Length == 3 && DateTime.TryParse(data[2], out var expiryDate))
                            {
                                if (expiryDate > DateTime.Now)
                                {
                                    var decryptedUsername = DecryptString(data[0]);
                                    var decryptedPassword = DecryptString(data[1]);

                                    usernameTextBox.Text = decryptedUsername;
                                    passwordBox.Password = decryptedPassword;
                                    rememberCheckBox.IsChecked = true;
                                }
                                else
                                {
                                    storage.DeleteFile("auth.dat");
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void SaveCredentials(string username, SecureString password, bool rememberMe)
        {
            try
            {
                using (var storage = IsolatedStorageFile.GetUserStoreForAssembly())
                {
                    if (rememberMe)
                    {
                        var plainPassword = SecureStringToString(password);
                        using (var stream = new IsolatedStorageFileStream("auth.dat", FileMode.Create, storage))
                        using (var writer = new StreamWriter(stream))
                        {
                            writer.Write($"{EncryptString(username)}|{EncryptString(plainPassword)}|{DateTime.Now.AddHours(RememberMeHours)}");
                        }
                    }
                    else if (storage.FileExists("auth.dat"))
                    {
                        storage.DeleteFile("auth.dat");
                    }
                }
            }
            catch { }
        }

        private string SecureStringToString(SecureString value)
        {
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = System.Runtime.InteropServices.Marshal.SecureStringToGlobalAllocUnicode(value);
                return System.Runtime.InteropServices.Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }

        private string EncryptString(string plainText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(CryptoKey));
                aes.IV = new byte[16];

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (StreamWriter sw = new StreamWriter(cs))
                    {
                        sw.Write(plainText);
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        private string DecryptString(string cipherText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(CryptoKey));
                aes.IV = new byte[16];

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(cipherText)))
                using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (StreamReader sr = new StreamReader(cs))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        private SecureString GetPasswordSecureString()
        {
            SecureString securePassword = new SecureString();
            foreach (char c in passwordBox.Password)
            {
                securePassword.AppendChar(c);
            }
            securePassword.MakeReadOnly();
            return securePassword;
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashBytes);
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var username = usernameTextBox.Text.Trim();
            var password = passwordBox.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Введите логин и пароль!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    var query = "SELECT Id, Password, Role FROM Users WHERE Login = @login";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@login", username);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var storedHash = reader["Password"].ToString();
                                var enteredHash = HashPassword(password);

                                if (enteredHash == storedHash)
                                {
                                    SaveCredentials(username, GetPasswordSecureString(), rememberCheckBox.IsChecked == true);

                                    var userId = Convert.ToInt32(reader["Id"]);
                                    var role = reader["Role"].ToString();
                                    OpenMainWindow(userId, role);
                                    this.Close();
                                }
                                else
                                {
                                    MessageBox.Show("Неверный пароль!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                            else
                            {
                                MessageBox.Show("Пользователь не найден!", "Ошибка",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка входа: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenMainWindow(int userId, string role)
        {
            if (string.IsNullOrEmpty(role))
            {
                role = "User";
            }

            if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                var adminWindow = new AdminWindow(userId, role);
                adminWindow.Show();
            }
            else
            {
                var userWindow = new UserWindow(userId, role);
                userWindow.Show();
            }
        }
        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            var registerWindow = new RegisterWindow();
            registerWindow.Show();
            this.Close();
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoginButton_Click(sender, e);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
    }
}