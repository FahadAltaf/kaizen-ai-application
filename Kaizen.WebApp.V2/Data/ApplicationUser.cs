using Microsoft.AspNetCore.Identity;

namespace Kaizen.WebApp.V2.Data
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public string Name { get; set; }
        public string? ProfileUrl { get; set; }
    }

}
