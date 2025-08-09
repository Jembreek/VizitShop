using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using VizitShop;
using VizitShop.Models;
using VizitShop.Commands;
using System.Collections.ObjectModel;
using System.Linq;

namespace VizitShop.Tests
{
    [TestClass]
    public class UnitTests
    {
        [TestMethod]
        public void Sneaker_PropertyChanged_TriggersEvent()
        {
            var sneaker = new Sneaker();
            bool wasRaised = false;
            sneaker.PropertyChanged += (s, e) => wasRaised = e.PropertyName == nameof(Sneaker.Name);
            sneaker.Name = "Test";
            Assert.IsTrue(wasRaised);
        }

        [TestMethod]
        public void CartItem_Total_CalculatesCorrectly()
        {
            var item = new CartItem
            {
                Product = new Product { Price = 1000 },
                Quantity = 3
            };
            Assert.AreEqual(3000, item.Total);
        }

        [TestMethod]
        public void CartItem_RemoveCommand_DecreasesQuantity()
        {
            var item = new CartItem
            {
                Product = new Product { Price = 1000 },
                Quantity = 2
            };
            item.RemoveCommand.Execute(null);
            Assert.AreEqual(1, item.Quantity);
        }

        [TestMethod]
        public void PriceFilter_Filter_WorksCorrectly()
        {
            var filter = new PriceFilter("Cheap", s => s.Price <= 1000);
            var sneaker = new Sneaker { Price = 900 };
            Assert.IsTrue(filter.Filter(sneaker));
        }

        [TestMethod]
        public void RelayCommand_CanExecute_TrueByDefault()
        {
            var command = new RelayCommandImplementation(_ => { });
            Assert.IsTrue(command.CanExecute(null));
        }

        [TestMethod]
        public void RelayCommand_Execute_RunsAction()
        {
            bool executed = false;
            var command = new RelayCommandImplementation(_ => executed = true);
            command.Execute(null);
            Assert.IsTrue(executed);
        }

        [TestMethod]
        public void AdminWindow_Filter_ByPrice()
        {
            var filter = new PriceFilter("До 5000", s => s.Price <= 5000);
            var sneakers = new ObservableCollection<Sneaker>
            {
                new Sneaker { Price = 4000 },
                new Sneaker { Price = 6000 }
            };
            var result = sneakers.Where(filter.Filter).ToList();
            Assert.AreEqual(1, result.Count);
        }

        [TestMethod]
        public void Sneaker_SetPrice_RaisesPropertyChanged()
        {
            var sneaker = new Sneaker();
            bool raised = false;
            sneaker.PropertyChanged += (s, e) => raised = e.PropertyName == nameof(Sneaker.Price);
            sneaker.Price = 9999;
            Assert.IsTrue(raised);
        }

        [TestMethod]
        public void CartViewModel_AddAndRemove_WorksCorrectly()
        {
            var vm = new CartViewModel();
            int originalCount = vm.Cart.Count;

            var item = new CartItem
            {
                Product = new Product { Name = "Test", Price = 500 },
                Quantity = 1,
                RemoveAction = cartItem => vm.Cart.Remove(cartItem)
            };

            vm.Cart.Add(item);
            item.RemoveCommand.Execute(null);
            Assert.AreEqual(originalCount, vm.Cart.Count);
        }

        [TestMethod]
        public void OrderItem_Total_IsCorrect()
        {
            var orderItem = new OrderItem
            {
                Price = 2500,
                Quantity = 2
            };
            Assert.AreEqual(5000, orderItem.Total);
        }
    }
}
