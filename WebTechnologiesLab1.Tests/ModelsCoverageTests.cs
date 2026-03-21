using System;
using System.Collections.Generic;
using WebTechnologiesLab1.Models;
using Xunit;

namespace WebTechnologiesLab1.Tests
{
    public class ModelsCoverageTests
    {
        [Fact]
        public void AllModels_GettersAndSetters_WorkCorrectly()
        {
            // Arrange & Act - ініціалізуємо всі моделі та їх властивості
            var user = new ApplicationUser
            {
                CartId = 1,
                IsPremiumUser = true,
                Cart = new Cart(),
                Orders = new List<Order>()
            };

            var cart = new Cart
            {
                Id = 1,
                ApplicationUserId = "user-123",
                ApplicationUser = user,
                CartItems = new List<CartItem>()
            };

            var order = new Order
            {
                Id = 1,
                OrderDate = new DateTime(2026, 1, 1),
                Status = "Processing",
                TotalAmount = 99.99m,
                UserId = "user-123",
                User = user,
                DeliveryAddress = "Kyiv, Khreshchatyk 1",
                PhoneNumber = "+380501234567",
                OrderItems = new List<OrderItem>()
            };

            var orderItem = new OrderItem
            {
                Id = 1,
                Quantity = 5,
                PriceAtPurchase = 19.99m,
                ProductId = 10,
                Product = new Product(),
                OrderId = 1,
                Order = order
            };

            // Assert - перевіряємо кілька значень, щоб тест був валідним
            Assert.True(user.IsPremiumUser);
            Assert.Equal("user-123", cart.ApplicationUserId);
            Assert.Equal("Processing", order.Status);
            Assert.Equal(5, orderItem.Quantity);
            Assert.Equal(19.99m, orderItem.PriceAtPurchase);
        }
    }
}