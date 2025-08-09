using System.Collections.ObjectModel;
using System.ComponentModel;

public class CartViewModel : INotifyPropertyChanged
{
    private ObservableCollection<CartItem> cart;

    public ObservableCollection<CartItem> Cart
    {
        get => cart;
        set
        {
            if (cart != value)
            {
                cart = value;
                OnPropertyChanged(nameof(Cart));
            }
        }
    }

    public CartViewModel()
    {
        Cart = new ObservableCollection<CartItem>();
        Cart.Add(new CartItem
        {
            Product = new Product { Name = "Кроссовки", Price = 5000 },
            Quantity = 1,
            RemoveAction = RemoveFromCart
        });
    }

    private void RemoveFromCart(CartItem item)
    {
        if (Cart.Contains(item))
        {
            Cart.Remove(item);
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string name)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}