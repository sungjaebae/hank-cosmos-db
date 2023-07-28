using Microsoft.EntityFrameworkCore;
using HankCosmosDB.Todos;
using System.Reflection.Metadata;

namespace HankCosmosDB.Data
{
    public class TodoDbContext : DbContext
    {
        public DbSet<Todo> Todos { get; set; }
        public TodoDbContext(DbContextOptions<TodoDbContext> options)
        : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Todo>()
                .ToContainer("Todo")
                .HasPartitionKey(t => t.Id);
        }
    }
}
