using Microsoft.AspNetCore.Identity;

namespace AuthAPI.Data
{
    public class ApplicationUser:IdentityUser
    {
        public string Name { get; set; }
    }
}
