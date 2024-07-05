using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardKingdomWebScraper.Models
{
	public class CardOwned
	{
		public int Id { get; set; }
		public int CardConditionId { get; set; }
		public required CardCondition CardCondition { get; set; }
		public int Quantity { get; set; }
		public int UserId { get; set; }
		public required User User { get; set; }
	}
}
