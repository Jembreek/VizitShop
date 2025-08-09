using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Input;

namespace VizitShop
{
    public partial class OrderDetailsWindow : Window
    {
        public Order CurrentOrder { get; set; }
        public ObservableCollection<OrderItem> OrderItems { get; } = new ObservableCollection<OrderItem>();

        public OrderDetailsWindow(Order order)
        {
            InitializeComponent();
            CurrentOrder = order;
            DataContext = this;
            LoadOrderItems(order.OrderId);
        }

        private void LoadOrderItems(int orderId)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                        SELECT s.Name, s.Brand, s.ImagePath, oi.Quantity, oi.Price
                        FROM OrderItems oi
                        JOIN Sneakers s ON oi.SneakerId = s.Id
                        WHERE oi.OrderId = @OrderId";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@OrderId", orderId);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            OrderItems.Clear();
                            while (reader.Read())
                            {
                                OrderItems.Add(new OrderItem
                                {
                                    ProductName = reader["Name"].ToString(),
                                    Brand = reader["Brand"].ToString(),
                                    ImagePath = reader["ImagePath"].ToString(),
                                    Quantity = Convert.ToInt32(reader["Quantity"]),
                                    Price = Convert.ToDecimal(reader["Price"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки товаров заказа: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void DragWindow(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class OrderItem
    {
        public string ProductName { get; set; }
        public string Brand { get; set; }
        public string ImagePath { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total => Price * Quantity;
    }
}