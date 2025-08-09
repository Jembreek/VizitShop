using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace VizitShop
{
    public partial class UserWindow : Window, INotifyPropertyChanged
    {
        public ObservableCollection<Product> Products { get; set; } = new ObservableCollection<Product>();
        public ObservableCollection<CartItem> Cart { get; set; } = new ObservableCollection<CartItem>();
        public ObservableCollection<Order> Orders { get; set; } = new ObservableCollection<Order>();
        private ObservableCollection<Product> _allProducts;

        private string _searchText;
        private PriceFilter _selectedPriceFilter;
        private ObservableCollection<PriceFilter> _priceFilters;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public bool IsCartEmpty => Cart == null || Cart.Count == 0;
        public decimal TotalPrice => Cart.Sum(item => item.Product.Price * item.Quantity);
        public User CurrentUser { get; set; }
        public Product SelectedProduct { get; set; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                FilterProducts();
            }
        }

        public PriceFilter SelectedPriceFilter
        {
            get => _selectedPriceFilter;
            set
            {
                _selectedPriceFilter = value;
                OnPropertyChanged(nameof(SelectedPriceFilter));
                FilterProducts();
            }
        }

        public ObservableCollection<PriceFilter> PriceFilters
        {
            get => _priceFilters;
            set
            {
                _priceFilters = value;
                OnPropertyChanged(nameof(PriceFilters));
            }
        }

        public class PriceFilter
        {
            public string DisplayName { get; set; }
            public string Value { get; set; }
        }

        public UserWindow(int userId, string userName)
        {
            InitializeComponent();
            CurrentUser = new User { UserId = userId, UserName = userName };
            DataContext = this;
            LoadData();
        }

        private void LoadData()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            _allProducts = new ObservableCollection<Product>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string productQuery = "SELECT Id, Name, Brand, Price, ImagePath, Size FROM Sneakers";
                using (SqlCommand command = new SqlCommand(productQuery, connection))
                {
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        _allProducts.Add(new Product
                        {
                            ProductId = Convert.ToInt32(reader["Id"]),
                            Name = reader["Name"].ToString(),
                            Brand = reader["Brand"].ToString(),
                            Size = reader["Size"].ToString(),
                            Price = Convert.ToDecimal(reader["Price"]),
                            ImagePath = reader["ImagePath"].ToString()
                        });
                    }
                    reader.Close();
                }

                PriceFilters = new ObservableCollection<PriceFilter>
                {
                    new PriceFilter { DisplayName = "Любая цена", Value = "all" },
                    new PriceFilter { DisplayName = "До 10000 ₽", Value = "0-10000" },
                    new PriceFilter { DisplayName = "10000-20000 ₽", Value = "10000-20000" },
                    new PriceFilter { DisplayName = "От 20000 ₽", Value = "20000-100000" }
                };
                SelectedPriceFilter = PriceFilters[0];
                FilterProducts();

                string cartQuery = @"
                    SELECT s.Id, s.Name, s.Brand, s.Price, s.ImagePath, s.Size, c.Quantity
                    FROM CartItems c
                    JOIN Sneakers s ON c.SneakerId = s.Id
                    WHERE c.UserId = @UserId";
                using (SqlCommand command = new SqlCommand(cartQuery, connection))
                {
                    command.Parameters.AddWithValue("@UserId", CurrentUser.UserId);
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        Cart.Add(new CartItem
                        {
                            Product = new Product
                            {
                                ProductId = Convert.ToInt32(reader["Id"]),
                                Name = reader["Name"].ToString(),
                                Brand = reader["Brand"].ToString(),
                                Size = reader["Size"].ToString(),
                                Price = Convert.ToDecimal(reader["Price"]),
                                ImagePath = reader["ImagePath"].ToString()
                            },
                            Quantity = Convert.ToInt32(reader["Quantity"])
                        });
                    }
                    reader.Close();
                }

                string orderQuery = "SELECT Id, OrderDate, TotalAmount FROM Orders WHERE UserId = @UserId";
                using (SqlCommand command = new SqlCommand(orderQuery, connection))
                {
                    command.Parameters.AddWithValue("@UserId", CurrentUser.UserId);
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        Orders.Add(new Order
                        {
                            OrderId = Convert.ToInt32(reader["Id"]),
                            OrderDate = Convert.ToDateTime(reader["OrderDate"]),
                            TotalAmount = Convert.ToDecimal(reader["TotalAmount"])
                        });
                    }
                    reader.Close();
                }
            }
        }

        private void FilterProducts()
        {
            if (_allProducts == null) return;

            var filtered = _allProducts.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = filtered.Where(p =>
                    p.Name.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    p.Brand.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (SelectedPriceFilter != null && SelectedPriceFilter.Value != "all")
            {
                var priceRange = SelectedPriceFilter.Value.Split('-');
                if (priceRange.Length == 2 &&
                    decimal.TryParse(priceRange[0], out decimal minPrice) &&
                    decimal.TryParse(priceRange[1], out decimal maxPrice))
                {
                    filtered = filtered.Where(p => p.Price >= minPrice && p.Price <= maxPrice);
                }
            }

            Products.Clear();
            foreach (var product in filtered)
            {
                Products.Add(product);
            }
        }

        private void AddToCart(Product product)
        {
            if (product == null) return;

            var existingItem = Cart.FirstOrDefault(ci => ci.Product.ProductId == product.ProductId);
            if (existingItem == null)
            {
                Cart.Add(new CartItem { Product = product, Quantity = 1 });

                using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    connection.Open();
                    string query = "INSERT INTO CartItems (UserId, SneakerId, Quantity) VALUES (@UserId, @SneakerId, @Quantity)";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@UserId", CurrentUser.UserId);
                    command.Parameters.AddWithValue("@SneakerId", product.ProductId);
                    command.Parameters.AddWithValue("@Quantity", 1);
                    command.ExecuteNonQuery();
                }
            }
            else
            {
                existingItem.Quantity++;

                using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    connection.Open();
                    string query = "UPDATE CartItems SET Quantity = @Quantity WHERE UserId = @UserId AND SneakerId = @SneakerId";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Quantity", existingItem.Quantity);
                    command.Parameters.AddWithValue("@UserId", CurrentUser.UserId);
                    command.Parameters.AddWithValue("@SneakerId", product.ProductId);
                    command.ExecuteNonQuery();
                }
            }

            OnPropertyChanged(nameof(TotalPrice));
            OnPropertyChanged(nameof(IsCartEmpty));
        }

        private void RemoveFromCart(CartItem cartItem)
        {
            if (cartItem.Quantity > 1)
            {
                cartItem.Quantity--;

                using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    connection.Open();
                    string query = "UPDATE CartItems SET Quantity = @Quantity WHERE UserId = @UserId AND SneakerId = @SneakerId";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", CurrentUser.UserId);
                        command.Parameters.AddWithValue("@SneakerId", cartItem.Product.ProductId);
                        command.Parameters.AddWithValue("@Quantity", cartItem.Quantity);
                        command.ExecuteNonQuery();
                    }
                }
            }
            else
            {
                using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    connection.Open();
                    string query = "DELETE FROM CartItems WHERE UserId = @UserId AND SneakerId = @SneakerId";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", CurrentUser.UserId);
                        command.Parameters.AddWithValue("@SneakerId", cartItem.Product.ProductId);
                        command.ExecuteNonQuery();
                    }
                }
                Cart.Remove(cartItem);
            }

            OnPropertyChanged(nameof(TotalPrice));
            OnPropertyChanged(nameof(IsCartEmpty));
        }

        private void Checkout()
        {
            try
            {
                if (Cart.Count == 0)
                {
                    MessageBox.Show("Ваша корзина пуста!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    connection.Open();

                    var orderCommand = new SqlCommand(
                        "INSERT INTO Orders (UserId, OrderDate, TotalAmount) OUTPUT INSERTED.Id VALUES (@UserId, @OrderDate, @TotalAmount)",
                        connection);
                    orderCommand.Parameters.AddWithValue("@UserId", CurrentUser.UserId);
                    orderCommand.Parameters.AddWithValue("@OrderDate", DateTime.Now);
                    orderCommand.Parameters.AddWithValue("@TotalAmount", TotalPrice);
                    int orderId = (int)orderCommand.ExecuteScalar();

                    foreach (var cartItem in Cart)
                    {
                        var orderItemCommand = new SqlCommand(
                            "INSERT INTO OrderItems (OrderId, SneakerId, Quantity, Price) VALUES (@OrderId, @SneakerId, @Quantity, @Price)",
                            connection);
                        orderItemCommand.Parameters.AddWithValue("@OrderId", orderId);
                        orderItemCommand.Parameters.AddWithValue("@SneakerId", cartItem.Product.ProductId);
                        orderItemCommand.Parameters.AddWithValue("@Quantity", cartItem.Quantity);
                        orderItemCommand.Parameters.AddWithValue("@Price", cartItem.Product.Price);
                        orderItemCommand.ExecuteNonQuery();
                    }

                    var clearCartCommand = new SqlCommand(
                        "DELETE FROM CartItems WHERE UserId = @UserId",
                        connection);
                    clearCartCommand.Parameters.AddWithValue("@UserId", CurrentUser.UserId);
                    clearCartCommand.ExecuteNonQuery();

                    Cart.Clear();
                    OnPropertyChanged(nameof(IsCartEmpty));
                    MessageBox.Show("Заказ успешно оформлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    Orders.Clear();
                    string orderQuery = "SELECT Id, OrderDate, TotalAmount FROM Orders WHERE UserId = @UserId";
                    using (SqlCommand command = new SqlCommand(orderQuery, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", CurrentUser.UserId);
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            Orders.Add(new Order
                            {
                                OrderId = Convert.ToInt32(reader["Id"]),
                                OrderDate = Convert.ToDateTime(reader["OrderDate"]),
                                TotalAmount = Convert.ToDecimal(reader["TotalAmount"])
                            });
                        }
                        reader.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка оформления заказа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewOrderDetails(Order order)
        {
            if (order == null) return;
            var detailsWindow = new OrderDetailsWindow(order);
            detailsWindow.ShowDialog();
        }

        private void CartButton_Click(object sender, RoutedEventArgs e)
        {
            CartPopup.IsOpen = !CartPopup.IsOpen;
        }

        public ICommand AddToCartCommand => new RelayCommand<Product>(AddToCart);
        public ICommand RemoveFromCartCommand => new RelayCommand<CartItem>(RemoveFromCart);
        public ICommand CheckoutCommand => new RelayCommand(Checkout);
        public ICommand ViewOrderDetailsCommand => new RelayCommand<Order>(ViewOrderDetails);
        public ICommand SearchCommand => new RelayCommand(FilterProducts);
        public ICommand ResetSearchCommand => new RelayCommand(() =>
        {
            SearchText = string.Empty;
            SelectedPriceFilter = PriceFilters[0];
        });

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}