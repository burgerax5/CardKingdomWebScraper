using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardKingdomWebScraper.Models
{
	public enum Rarity
	{
		Common,
		Uncommon,
		Rare,
		Mythic_Rare
	};

	public class Card
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string ImageURL { get; set; }
		public int EditionId { get; set; }
		public required Edition Edition { get; set; }
		public Rarity Rarity { get; set; }
		public List<CardCondition> Conditions { get; set; } = new List<CardCondition>();
		public bool IsFoil { get; set; }
	}
}
