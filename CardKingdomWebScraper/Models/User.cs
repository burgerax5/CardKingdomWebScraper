using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardKingdomWebScraper.Models
{
	public class User
	{
		public int Id { get; set; }
		public required string Username { get; set; }
		public required string Password { get; set; }
		public required string Salt { get; set; }
		public List<CardOwned> CardsOwned { get; set; } = new List<CardOwned>();
	}
}
