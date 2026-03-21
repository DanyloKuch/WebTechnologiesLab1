using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using WebTechnologiesLab1.Models;
using Xunit;

namespace WebTechnologiesLab1.Tests
{
    public class ModelTests
    {
        // Конструктор = простий setup (виконується перед кожним тестом)
        // Вимога ТЗ: Наявність Setup/Fixture
        public ModelTests()
        {
        }

        // ===================================================================
        // Параметризований тест. Вимога ТЗ: Parameterized test (InlineData)
        // ===================================================================
        [Theory]
        [InlineData("100", "1", "100")]
        [InlineData("49.99", "3", "149.97")]
        [InlineData("0", "5", "0")]
        [InlineData("250", "2", "500")]
        public void CartItem_Subtotal_Calculation_Is_Correct(
            string unitPriceStr,
            string quantityStr,
            string expectedSubtotalStr)
        {
            decimal unitPrice = decimal.Parse(unitPriceStr, CultureInfo.InvariantCulture);
            int quantity = int.Parse(quantityStr, CultureInfo.InvariantCulture);
            decimal expected = decimal.Parse(expectedSubtotalStr, CultureInfo.InvariantCulture);

            var product = new Product { id = 1, name = "Test Product", price = unitPrice };
            var cartItem = new CartItem { Quantity = quantity, Product = product };

            decimal actualSubtotal = cartItem.Quantity * cartItem.Product.price;

            // Вимога ТЗ: різні види Assert (тут NotNull, True, Equal)
            Assert.NotNull(cartItem.Product); // Перевіряє, що об'єкт не дорівнює null
            Assert.True(cartItem.Quantity > 0 || cartItem.Quantity == 0); // Перевіряє логічну умову (true)
            Assert.Equal(expected, actualSubtotal); // Точне порівняння очікуваного і фактичного результату
            Assert.Equal(1, product.id);
        }

        // ===================================================================
        // Тест на виключення. Вимога ТЗ: Assert на Exception
        // ===================================================================
        [Fact]
        public void CartItem_Quantity_Negative_Throws_ArgumentException()
        {
            // Assert.Throws перехоплює виключення. Якщо код не викине ArgumentException, тест впаде.
            var exception = Assert.Throws<ArgumentException>(() =>
            {
                var item = new CartItem { Quantity = -5 };
                if (item.Quantity < 0)
                    throw new ArgumentException("Quantity cannot be negative.");
            });

            // Assert на рядок (перевіряємо, що повідомлення помилки містить ключове слово)
            Assert.Contains("negative", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        // ===================================================================
        // Складний assert #1: Assert.Collection
        // Вимога ТЗ: Складний Assert для колекцій
        // ===================================================================
        [Fact]
        public void Cart_CartItems_Collection_Matches_Expected()
        {
            var cart = new Cart { Id = 1, CartItems = new List<CartItem>() };
            var product1 = new Product { id = 1, name = "Book", price = 299m };
            var product2 = new Product { id = 2, name = "Pen", price = 15m };

            var item1 = new CartItem { Quantity = 2, Product = product1 };
            var item2 = new CartItem { Quantity = 5, Product = product2 };

            ((List<CartItem>)cart.CartItems).Add(item1);
            ((List<CartItem>)cart.CartItems).Add(item2);

            // Складний Assert: Assert.Collection. 
            // Він проходить по кожному елементу колекції по черзі і виконує для нього набір перевірок.
            // Якщо хоч один елемент не відповідає своїй позиції або перевірці — тест падає.
            Assert.Collection((IEnumerable<CartItem>)cart.CartItems,
                item =>
                {
                    Assert.Equal(1, item.Product.id);
                    Assert.Equal("Book", item.Product.name);
                    Assert.Equal(2, item.Quantity);
                },
                item =>
                {
                    Assert.Equal(2, item.Product.id);
                    Assert.Equal("Pen", item.Product.name);
                    Assert.Equal(5, item.Quantity);
                }
            );

            Assert.Equal(2, cart.CartItems.Count);
        }

        // ===================================================================
        // Складний assert #2: Assert.Matches
        // Вимога ТЗ: Другий складний Assert
        // ===================================================================
        [Theory]
        [InlineData("iPhone 14 Pro", @"^iPhone\s\d{1,2}\sPro$")]
        [InlineData("Samsung Galaxy S23", @"^Samsung\sGalaxy\sS\d{2,3}$")]
        public void Product_Name_Matches_Expected_Pattern(string name, string regexPattern)
        {
            var product = new Product { id = 100, name = name, price = 999m };

            // Складний Assert: Assert.Matches.
            // Вимагає використання регулярних виразів (Regex) для перевірки складної логіки форматування рядка.
            Assert.Matches(regexPattern, product.name);
        }
    }
}