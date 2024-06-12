using CardKingdomWebScraper.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardKingdomWebScraper.Data
{
	public class DataContext : DbContext
	{
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
            
        }

        public DbSet<Card> Cards { get; set; }
		public DbSet<Edition> Editions { get; set; }
		public DbSet<CardCondition> CardConditions { get; set; }
		public DbSet<CardOwned> CardsOwned { get; set; }
		public DbSet<User> Users { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Edition>(entity =>
			{
				entity.HasIndex(e => e.Name).IsUnique();
			});

			modelBuilder.Entity<CardCondition>()
				.Property(c => c.Condition)
				.HasConversion<string>();

			modelBuilder.Entity<Card>()
				.Property(c => c.Rarity)
				.HasConversion<string>();
		}
	}
}
