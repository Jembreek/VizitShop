using System;
using System.ComponentModel;

namespace VizitShop
{
    public class Order : INotifyPropertyChanged
    {
        private int _orderId;
        private DateTime _orderDate;
        private decimal _totalAmount;

        public int OrderId
        {
            get => _orderId;
            set { _orderId = value; OnPropertyChanged(nameof(OrderId)); }
        }

        public DateTime OrderDate
        {
            get => _orderDate;
            set { _orderDate = value; OnPropertyChanged(nameof(OrderDate)); }
        }

        public decimal TotalAmount
        {
            get => _totalAmount;
            set { _totalAmount = value; OnPropertyChanged(nameof(TotalAmount)); }
        }

        public string FullName { get; internal set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}