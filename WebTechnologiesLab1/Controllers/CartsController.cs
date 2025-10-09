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

namespace WebTechnologiesLab1.Controllers
{
    [Authorize]
    public class CartsController : Controller
    {
        private readonly WebDbContext _context;

        public CartsController(WebDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> AddItem(int productId, int quantity = 1)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var cart = await _context.Carts
                .Include(c => c.CartItems)
        .       FirstOrDefaultAsync(c => c.ApplicationUserId == userId);

            if (cart == null)
            {
                cart = new Cart { ApplicationUserId = userId };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);

            if (cartItem != null)
            {
                cartItem.Quantity += quantity; 
            }
            else
            {
                _context.CartItems.Add(new CartItem { CartId = cart.Id, ProductId = productId, Quantity = quantity }); 
            }
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Carts");
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.ApplicationUserId == userId);

            if (cart == null)
            {
                cart = new Cart { CartItems = new List<CartItem>() };
            }

            return View(cart); 
        }

        // POST: Cart/UpdateQuantity
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int newQuantity)
        {
            var cartItem = await _context.CartItems
                .Include(ci => ci.Cart) 
                .FirstOrDefaultAsync(ci => ci.Id == cartItemId);

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (cartItem == null || cartItem.Cart.ApplicationUserId != userId)
            {
                return NotFound();
            }

            cartItem.Quantity = newQuantity;

            _context.Update(cartItem);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
