using CardKingdomWebScraper.Models;

namespace CardKingdomWebScraper.Utility.Tests
{
	[TestClass()]
	public class ScraperTests
	{
		[TestMethod()]
		public async Task ScrapeEditionNamesTest()
		{
			List<Edition> editions = await Scraper.ScrapeEditionNames();

			// As of 20th June 2024 there are 339 editions
			Assert.IsTrue(editions.Count >= 339);

			// First edition to be grabbed should be 3rd Edition
			Edition firstEdition = editions[0];
			Assert.AreEqual("3rd Edition", firstEdition.Name);
			Assert.AreEqual("3rd-edition", firstEdition.Code);
		}

		[TestMethod()]
		public async Task ScrapeEditionCardsTest()
		{
			Edition edition = new Edition
			{
				Name = "Portal II",
				Code = "portal-ii"
			};

			List<Card> cardsInPortalII = await Scraper.ScrapeCardsFromEdition(edition);
			Card firstCard = cardsInPortalII.First();

			// There should be 165 cards (no foils) with the first one being "Goblin War Strike"
			Assert.AreEqual(165, cardsInPortalII.Count);
			Assert.AreEqual("Goblin War Strike", firstCard.Name);
		}

		[TestMethod()]
		public async Task ScrapeInvalidEditionCardsTest()
		{
			Edition edition = new Edition
			{
				Name = "No Edition",
				Code = "no-edition"
			};

			List<Card> cards = await Scraper.ScrapeCardsFromEdition(edition);
			Assert.AreEqual(0, cards.Count);
		}
	}
}