using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WebTechnologiesLab1.Data;
using WebTechnologiesLab1.Models;
using System.Net.Http;
using System.Web;
using Microsoft.Extensions.Logging;

namespace WebTechnologiesLab1.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly WebDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(WebDbContext context, IConfiguration configuration, ILogger<OrdersController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        // GET: Orders
        public async Task<IActionResult> Index()
        {
            var webDbContext = _context.Orders.Include(o => o.User);
            return View(await webDbContext.ToListAsync());
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: Orders/Checkout
        public async Task<IActionResult> Checkout()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.ApplicationUserId == userId);

            if (cart == null || !cart.CartItems.Any())
            {
                TempData["Error"] = "Ваш кошик порожній. Додайте товари для оформлення.";
                return RedirectToAction("Index", "Carts");
            }

            return View(cart);
        }

        // POST: Orders/PlaceOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(string DeliveryAddress, string PhoneNumber)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.ApplicationUserId == userId);

            var order = new Order
            {
                UserId = userId, 
                OrderDate = DateTime.Now,
                Status = "New",
                DeliveryAddress = DeliveryAddress,
                PhoneNumber = PhoneNumber,
                OrderItems = new List<OrderItem>() 
            };

            _context.Orders.Add(order);

            decimal totalAmount = 0;

            foreach (var cartItem in cart.CartItems)
            {
                var itemPrice = cartItem.Product.price;

                var orderItem = new OrderItem
                {
                    ProductId = cartItem.ProductId,
                    Quantity = cartItem.Quantity,
                    PriceAtPurchase = itemPrice,
                };
                totalAmount += itemPrice * cartItem.Quantity;

                order.OrderItems.Add(orderItem);
            }

            order.TotalAmount = totalAmount;

            _context.CartItems.RemoveRange(cart.CartItems);

            await _context.SaveChangesAsync();

            try
            {
                string token = _configuration["Telegram:BotToken"];
                string chatId = _configuration["Telegram:ChatId"];

                if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(chatId))
                {
                    string message = $"🎉 Нове замовлення!\nID: {order.Id}\nСума: {order.TotalAmount:C}\nАдреса: {order.DeliveryAddress}\nТелефон: {order.PhoneNumber}";
                    string encodedMessage = HttpUtility.UrlEncode(message);
                    string url = $"https://api.telegram.org/bot{token}/sendMessage?chat_id={chatId}&text={encodedMessage}";

                    using (var httpClient = new HttpClient())
                    {
                        HttpResponseMessage response = await httpClient.GetAsync(url);

                        if (!response.IsSuccessStatusCode)
                        {
                            string errorBody = await response.Content.ReadAsStringAsync();
                            _logger.LogError("Telegram API Error: {StatusCode} - {ErrorBody}", response.StatusCode, errorBody);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending Telegram notification");
            }

            return RedirectToAction("Confirmation", new { id = order.Id });
        }
        // GET: Orders/Confirmation/5
        public async Task<IActionResult> Confirmation(int id)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var order = await _context.Orders
                .Include(o => o.OrderItems) 
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null || order.UserId != userId)
            {
                return NotFound();
            }
            return View(order);

        }

    }
}
