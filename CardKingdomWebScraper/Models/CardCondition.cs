using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardKingdomWebScraper.Models
{
	public enum Condition
	{
		NM,
		EX,
		VG,
		G
	}
	public class CardCondition
	{
		public int Id { get; set; }
		public int CardId { get; set; }
		public Card Card { get; set; }
		public int Quantity { get; set; }
		public Condition Condition { get; set; }
		public double Price { get; set; }
	}
}
