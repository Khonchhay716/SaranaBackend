using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Interfaces;

namespace POS.Infrastructure.Data.Configurations
{
    public class MyAppDbContext : DbContext, IMyAppDbContext
    {
        public MyAppDbContext(DbContextOptions<MyAppDbContext> options) : base(options)
        {

        }
    }
}