using System.ComponentModel;
using System.Windows.Input;
using System;
using GalaSoft.MvvmLight.Command;

public class CartItem : INotifyPropertyChanged
{
    private int quantity;
    public Product Product { get; set; }

    public int Quantity
    {
        get => quantity;
        set
        {
            if (quantity != value)
            {
                quantity = value;
                OnPropertyChanged(nameof(Quantity));
                OnPropertyChanged(nameof(Total));
                QuantityChanged?.Invoke();
            }
        }
    }

    public ICommand RemoveCommand { get; set; }
    public Action<CartItem> RemoveAction { get; set; }
    public Action QuantityChanged { get; set; }

    public CartItem()
    {
        RemoveCommand = new RelayCommand(RemoveItem);
    }

    private void RemoveItem()
    {
        if (Quantity > 1)
        {
            Quantity--;
            QuantityChanged?.Invoke();
        }
        else
        {
            RemoveAction?.Invoke(this);
        }
    }

    public decimal Total => Product.Price * Quantity;

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string name)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}