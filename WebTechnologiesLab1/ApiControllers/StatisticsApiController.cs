using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebTechnologiesLab1.Data;

namespace WebTechnologiesLab1.ApiControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatisticsApiController : ControllerBase
    {
        private readonly WebDbContext _context;

        public StatisticsApiController(WebDbContext context)
        {
            _context = context;
        }

        [HttpGet("ProductSalesData")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductSalesData()
        {
            var salesData = await _context.OrderItems
                .Where(oi => oi.Product != null)
                .GroupBy(oi => oi.Product.name)
                .Select(g => new
                {
                    ProductName = g.Key,
                    TotalQuantity = g.Sum(oi => oi.Quantity)
                })
                .OrderByDescending(d => d.TotalQuantity)
                .ToListAsync();

            if (!salesData.Any())
            {
                return NotFound("Немає даних про замовлення.");
            }

            return Ok(salesData);
        }
    }
}
