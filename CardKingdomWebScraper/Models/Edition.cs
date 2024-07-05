using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardKingdomWebScraper.Models
{
	public class Edition
	{
		public int Id { get; set; }
		public required string Name { get; set; }
		public required string Code { get; set; }
		public ICollection<Card> Cards { get; set; } = new List<Card>();
	}
}
