using Microsoft.AspNetCore.Identity;

namespace WebTechnologiesLab1.Models
{
    public class ApplicationUser : IdentityUser
    {
        public int CartId { get; set; }
        public Cart Cart { get; set; }
        public bool IsPremiumUser { get; set; } = false;
        public ICollection<Order>? Orders { get; set; }
    }
}
