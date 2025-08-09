using System;
using System.ComponentModel;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;

namespace VizitShop.Models
{
    public class Sneaker : INotifyPropertyChanged
    {
        private string _name;
        private string _brand;
        private double _size;
        private decimal _price;
        private string _imageUrl;

        public int Id { get; set; }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        public string Brand
        {
            get => _brand;
            set { _brand = value; OnPropertyChanged(nameof(Brand)); }
        }

        public double Size
        {
            get => _size;
            set { _size = value; OnPropertyChanged(nameof(Size)); }
        }

        public decimal Price
        {
            get => _price;
            set { _price = value; OnPropertyChanged(nameof(Price)); }
        }

        public string ImageUrl
        {
            get => _imageUrl;
            set { _imageUrl = value; OnPropertyChanged(nameof(ImageUrl)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class AdminUser : INotifyPropertyChanged
    {
        private string _role;

        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public DateTime RegistrationDate { get; set; }

        public string Role
        {
            get => _role;
            set
            {
                if (_role != value)
                {
                    _role = value;
                    OnPropertyChanged(nameof(Role));
                    UpdateUserRoleInDatabase();
                }
            }
        }

        private void UpdateUserRoleInDatabase()
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (var command = new SqlCommand("UPDATE Users SET Role = @Role WHERE UserId = @UserId", connection))
                    {
                        command.Parameters.AddWithValue("@Role", Role);
                        command.Parameters.AddWithValue("@UserId", UserId);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления роли пользователя: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class StatsViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class PriceFilter
    {
        public string DisplayName { get; }
        public Func<Sneaker, bool> Filter { get; }

        public PriceFilter(string displayName, Func<Sneaker, bool> filter)
        {
            DisplayName = displayName;
            Filter = filter;
        }
    }
}