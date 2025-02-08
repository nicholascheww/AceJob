using AceJobAgency.ViewModels;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
namespace AceJobAgency.Model
{
    public class AuthDbContext : IdentityDbContext
    {
        private readonly IConfiguration _configuration;
        private DbSet<User> users;

        //public AuthDbContext(DbContextOptions<AuthDbContext> options):base(options){ }
        public AuthDbContext(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string connectionString = _configuration.GetConnectionString("ConnectionString"); optionsBuilder.UseSqlServer(connectionString);
        }
        public DbSet<User> Users { get => users; set => users = value; }
        public DbSet<UserSessions> UserSessions { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

    }
}