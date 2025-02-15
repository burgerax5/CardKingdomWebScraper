﻿using CardKingdomWebScraper.Models;
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

			modelBuilder.Entity<Card>()
				.HasIndex(c => c.Rarity)
				.HasDatabaseName("IX_Cards_Rarity");

			modelBuilder.Entity<Card>()
				.HasIndex(c => c.NMPrice)
				.HasDatabaseName("IX_Cards_NMPrice");

			modelBuilder.Entity<CardCondition>()
				.Property(c => c.Condition)
				.HasConversion<string>();
		}
	}
}
