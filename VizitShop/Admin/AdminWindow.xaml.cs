using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace VizitShop
{
    public partial class AdminWindow : Window, INotifyPropertyChanged
    {
        private string _searchText;
        private PriceFilter _selectedPriceFilter;
        private ObservableCollection<Sneaker> _filteredProducts = new ObservableCollection<Sneaker>();
        private readonly string _connectionString;
        private readonly string _role;
        private Sneaker _selectedProduct;
        private bool _isProductsVisible = true;
        private bool _isSalesHistoryVisible;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<Sneaker> AllProducts { get; private set; } = new ObservableCollection<Sneaker>();
        public ObservableCollection<OrderWithUser> AllOrders { get; } = new ObservableCollection<OrderWithUser>();

        public ObservableCollection<Sneaker> Products
        {
            get => _filteredProducts;
            private set
            {
                _filteredProducts = value;
                OnPropertyChanged(nameof(Products));
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value) return;
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                ApplyFilters();
            }
        }

        public ObservableCollection<PriceFilter> PriceFilters { get; private set; }

        public PriceFilter SelectedPriceFilter
        {
            get => _selectedPriceFilter;
            set
            {
                if (_selectedPriceFilter == value) return;
                _selectedPriceFilter = value;
                OnPropertyChanged(nameof(SelectedPriceFilter));
                ApplyFilters();
            }
        }

        public bool IsProductsVisible
        {
            get => _isProductsVisible;
            set
            {
                _isProductsVisible = value;
                OnPropertyChanged(nameof(IsProductsVisible));
            }
        }

        public bool IsSalesHistoryVisible
        {
            get => _isSalesHistoryVisible;
            set
            {
                _isSalesHistoryVisible = value;
                OnPropertyChanged(nameof(IsSalesHistoryVisible));
            }
        }

        public Sneaker SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                _selectedProduct = value;
                OnPropertyChanged(nameof(SelectedProduct));
            }
        }

        public AdminWindow(int userId, string role)
        {
            InitializeComponent();
            _role = role;
            _connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            DataContext = this;

            InitializePriceFilters();
            LoadProducts();
        }

        private void InitializePriceFilters()
        {
            PriceFilters = new ObservableCollection<PriceFilter>
            {
                new PriceFilter("Все цены", null),
                new PriceFilter("До 5000", p => p.Price <= 5000),
                new PriceFilter("5000-15000", p => p.Price > 5000 && p.Price <= 15000),
                new PriceFilter("15000+", p => p.Price > 15000)
            };
            SelectedPriceFilter = PriceFilters[0];
        }

        private void LoadProducts()
        {
            try
            {
                AllProducts.Clear();
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new SqlCommand("SELECT Id, Name, Brand, Size, Price, ImagePath FROM Sneakers", connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            AllProducts.Add(new Sneaker
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Brand = reader.GetString(2),
                                Size = reader.GetDouble(3),
                                Price = reader.GetDecimal(4),
                                ImageUrl = reader.IsDBNull(5) ? "/Images/default.png" : reader.GetString(5)
                            });
                        }
                    }
                }
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки товаров: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSalesHistory()
        {
            try
            {
                AllOrders.Clear();
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    string query = @"
                        SELECT o.Id AS OrderId, o.OrderDate, o.TotalAmount, 
                               u.FullName
                        FROM Orders o
                        JOIN Users u ON o.UserId = u.Id
                        ORDER BY o.OrderDate DESC";

                    using (var command = new SqlCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            AllOrders.Add(new OrderWithUser
                            {
                                OrderId = reader.GetInt32(0),
                                OrderDate = reader.GetDateTime(1),
                                TotalAmount = reader.GetDecimal(2),
                                FullName = reader.GetString(3)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки истории продаж: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            if (AllProducts == null || AllProducts.Count == 0) return;

            var filtered = AllProducts.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                filtered = filtered.Where(p =>
                    p.Name.ToLower().Contains(searchLower) ||
                    p.Brand.ToLower().Contains(searchLower));
            }

            if (SelectedPriceFilter?.Filter != null)
            {
                filtered = filtered.Where(SelectedPriceFilter.Filter);
            }

            Products = new ObservableCollection<Sneaker>(filtered);
        }

        private void ViewOrderDetails(object parameter)
        {
            if (parameter is OrderWithUser order)
            {
                var detailsWindow = new OrderDetailsWindow(new Order
                {
                    OrderId = order.OrderId,
                    OrderDate = order.OrderDate,
                    TotalAmount = order.TotalAmount
                });
                detailsWindow.ShowDialog();
            }
        }

        public ICommand SwitchToProductsCommand => new RelayCommandImplementation(_ =>
        {
            IsProductsVisible = true;
            IsSalesHistoryVisible = false;
        });

        public ICommand SwitchToSalesHistoryCommand => new RelayCommandImplementation(_ =>
        {
            IsProductsVisible = false;
            IsSalesHistoryVisible = true;
            LoadSalesHistory();
        });

        public ICommand SearchCommand => new RelayCommandImplementation(_ => ApplyFilters());
        public ICommand ResetSearchCommand => new RelayCommandImplementation(_ => ResetFilters());
        public ICommand DeleteProductCommand => new RelayCommandImplementation(DeleteProduct);
        public ICommand EditProductCommand => new RelayCommandImplementation(EditProduct);
        public ICommand ViewOrderDetailsCommand => new RelayCommandImplementation(ViewOrderDetails);
        public ICommand AddProductCommand => new RelayCommandImplementation(_ => AddNewProduct());

        private void ResetFilters()
        {
            SearchText = string.Empty;
            SelectedPriceFilter = PriceFilters[0];
        }

        private void DeleteProduct(object parameter)
        {
            if (parameter is Sneaker product)
            {
                var result = MessageBox.Show($"Вы уверены, что хотите удалить товар {product.Name}?",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var connection = new SqlConnection(_connectionString))
                        {
                            connection.Open();
                            using (var command = new SqlCommand("DELETE FROM Sneakers WHERE Id = @Id", connection))
                            {
                                command.Parameters.AddWithValue("@Id", product.Id);
                                command.ExecuteNonQuery();
                            }
                        }

                        AllProducts.Remove(product);
                        ApplyFilters();
                        MessageBox.Show("Товар успешно удален", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении товара: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void EditProduct(object parameter)
        {
            if (parameter is Sneaker productToEdit)
            {
                var editWindow = new EditProductWindow(productToEdit)
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                if (editWindow.ShowDialog() == true)
                {
                    try
                    {
                        using (var connection = new SqlConnection(_connectionString))
                        {
                            connection.Open();
                            using (var command = new SqlCommand(
                                "UPDATE Sneakers SET Name = @Name, Brand = @Brand, Size = @Size, Price = @Price, ImagePath = @ImagePath WHERE Id = @Id",
                                connection))
                            {
                                command.Parameters.AddWithValue("@Id", editWindow.Product.Id);
                                command.Parameters.AddWithValue("@Name", editWindow.Product.Name);
                                command.Parameters.AddWithValue("@Brand", editWindow.Product.Brand);
                                command.Parameters.AddWithValue("@Size", editWindow.Product.Size);
                                command.Parameters.AddWithValue("@Price", editWindow.Product.Price);
                                command.Parameters.AddWithValue("@ImagePath", editWindow.Product.ImageUrl ?? (object)DBNull.Value);

                                int rowsAffected = command.ExecuteNonQuery();

                                if (rowsAffected == 0)
                                {
                                    MessageBox.Show("Товар не был найден в базе данных", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                    return;
                                }
                            }
                        }

                        var originalProduct = AllProducts.FirstOrDefault(p => p.Id == editWindow.Product.Id);
                        if (originalProduct != null)
                        {
                            originalProduct.Name = editWindow.Product.Name;
                            originalProduct.Brand = editWindow.Product.Brand;
                            originalProduct.Size = editWindow.Product.Size;
                            originalProduct.Price = editWindow.Product.Price;
                            originalProduct.ImageUrl = editWindow.Product.ImageUrl;

                            OnPropertyChanged(nameof(AllProducts));
                            ApplyFilters();

                            MessageBox.Show("Товар успешно обновлен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при обновлении товара: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        LoadProducts();
                    }
                }
            }
        }

        private void AddNewProduct()
        {
            var newProduct = new Sneaker
            {
                Name = "Новый товар",
                Brand = "Бренд",
                Size = 0,
                Price = 0,
                ImageUrl = "/Images/default.png"
            };

            var editWindow = new EditProductWindow(newProduct, isNewProduct: true)
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            if (editWindow.ShowDialog() == true)
            {
                try
                {
                    int newId;
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();
                        using (var command = new SqlCommand(
                            "INSERT INTO Sneakers (Name, Brand, Size, Price, ImagePath) " +
                            "VALUES (@Name, @Brand, @Size, @Price, @ImagePath); " +
                            "SELECT SCOPE_IDENTITY();", connection))
                        {
                            command.Parameters.AddWithValue("@Name", editWindow.Product.Name);
                            command.Parameters.AddWithValue("@Brand", editWindow.Product.Brand);
                            command.Parameters.AddWithValue("@Size", editWindow.Product.Size);
                            command.Parameters.AddWithValue("@Price", editWindow.Product.Price);
                            command.Parameters.AddWithValue("@ImagePath", editWindow.Product.ImageUrl ?? (object)DBNull.Value);

                            newId = Convert.ToInt32(command.ExecuteScalar());
                        }
                    }

                    newProduct.Id = newId;
                    AllProducts.Add(newProduct);
                    ApplyFilters();

                    MessageBox.Show("Товар успешно добавлен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при добавлении товара: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class OrderWithUser
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string FullName { get; set; }
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
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        public string Brand
        {
            get => _brand;
            set
            {
                if (_brand != value)
                {
                    _brand = value;
                    OnPropertyChanged(nameof(Brand));
                }
            }
        }

        public double Size
        {
            get => _size;
            set
            {
                if (_size != value)
                {
                    _size = value;
                    OnPropertyChanged(nameof(Size));
                }
            }
        }

        public decimal Price
        {
            get => _price;
            set
            {
                if (_price != value)
                {
                    _price = value;
                    OnPropertyChanged(nameof(Price));
                }
            }
        }

        public string ImageUrl
        {
            get => _imageUrl;
            set
            {
                if (_imageUrl != value)
                {
                    _imageUrl = value;
                    OnPropertyChanged(nameof(ImageUrl));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class RelayCommandImplementation : ICommand
    {
        private readonly Action<object> _executeAction;
        private readonly Func<object, bool> _canExecuteFunc;

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public RelayCommandImplementation(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _executeAction = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecuteFunc = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecuteFunc == null || _canExecuteFunc(parameter);

        public void Execute(object parameter) => _executeAction(parameter);
    }
}