using System.Windows;
using System.Windows.Input;

namespace VizitShop
{
    public partial class EditProductWindow : Window
    {
        public Sneaker Product { get; private set; }
        public string WindowTitle => _isNewProduct ? "Добавление товара" : "Редактирование товара";
        private readonly bool _isNewProduct;

        public EditProductWindow(Sneaker product, bool isNewProduct = false)
        {
            InitializeComponent();
            _isNewProduct = isNewProduct;

            Product = new Sneaker
            {
                Id = product.Id,
                Name = product.Name,
                Brand = product.Brand,
                Size = product.Size,
                Price = product.Price,
                ImageUrl = product.ImageUrl
            };

            DataContext = this;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Product.Name))
            {
                MessageBox.Show("Введите название товара", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Product.Price < 0)
            {
                MessageBox.Show("Цена не может быть отрицательной", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}