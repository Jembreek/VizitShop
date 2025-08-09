using System;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;

public class Product
{
    public int ProductId { get; set; }
    public string Name { get; set; }
    public string Brand { get; set; }
    public string Size { get; set; }
    public decimal Price { get; set; }
    public string ImagePath { get; set; }

    public ICommand AddToCartCommand { get; set; }

    public void SetAddToCartCommand(Action<Product> addAction)
    {
        AddToCartCommand = new RelayCommand(() => addAction(this));
    }
}