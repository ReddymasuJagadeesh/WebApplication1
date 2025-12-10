using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext (DbContextOptions options) : base(options)
        {

        }

        public DbSet<Students> Students { get; set; }   
        
        

        
    }
}
