using CardKingdomWebScraper.Data;
using CardKingdomWebScraper.Models;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;

namespace CardKingdomWebScraper.Utility.Tests
{
	[TestClass()]
	public class ScraperTests
	{
		[TestMethod()]
		public void ScrapeEditionNames_ReturnsEditions()
		{
			// Arrange
			var mockHtml = @"<div class='row anchorList'>
								<a href='/mtg/3rd-edition'>3rd Edition</a>
								<a href='/mtg/4th-edition'>4th Edition</a>
							</div>";

			HtmlDocument htmlDocument = new HtmlDocument();
			htmlDocument.LoadHtml(mockHtml);

			// Act
			var scrapedEditions = Scraper.ScrapeEditionNames(htmlDocument);

			// Assert
			Assert.AreEqual(scrapedEditions[0].Name, "3rd Edition");
			Assert.AreEqual(scrapedEditions[1].Name, "4th Edition");
		}

		[TestMethod()]
		public void ScrapeCardName_ReturnsCorrectName()
		{
			// Arrange
			var mockHtml = @"<div class='productItemWrapper productCardWrapper'>
								<span class='productDetailTitle'>Mock Card Name</span>
							</div>";

			var htmlDocument = new HtmlDocument();
			htmlDocument.LoadHtml(mockHtml);
			var cardNode = htmlDocument.DocumentNode.SelectSingleNode("//div[@class='productItemWrapper productCardWrapper']");

			// Act
			var cardName = Scraper.ScrapeCardName(cardNode);

			// Assert
			Assert.AreEqual("Mock Card Name", cardName);
		}

		[TestMethod()]
		public void ScrapeCardImageURL_ReturnsCorrectURL()
		{
			// Arrange
			var mockHtml = @"<div class='productItemWrapper productCardWrapper'>
								<mtg-card-image src='/images/cards/mock_card.jpg'></mtg-card-image>
							</div>";

			var htmlDocument = new HtmlDocument();
			htmlDocument.LoadHtml(mockHtml);
			var cardNode = htmlDocument.DocumentNode.SelectSingleNode("//div[@class='productItemWrapper productCardWrapper']");

			// Act
			var cardImageURL = Scraper.ScrapeCardImageURL(cardNode);

			// Assert
			Assert.AreEqual("www.cardkingdom.com/images/cards/mock_card.jpg", cardImageURL);
		}

		[TestMethod()]
		public void ScrapeIsFoil_ReturnTrueForFoil_FalseForNormal()
		{
			// Arrange
			var mockHtml = @"<div class='collector-foil-wrapper'>
								<div class=""collector-number d-none d-sm-block"">Collector #: 163</div>
							</div>

							<div class='collector-foil-wrapper'>
								<div class=""collector-foil-wrapper""><div class=""collector-number d-none d-sm-block"">Collector #: 163</div> 
								<div class=""foil"">FOIL</div></div>
							</div>";

			var htmlDocument = new HtmlDocument();
			htmlDocument.LoadHtml(mockHtml);
			var cardNodes = htmlDocument.DocumentNode.SelectNodes("div[@class='collector-foil-wrapper']");

			// Act
			var nonFoilCard = Scraper.ScrapeIsFoil(cardNodes[0]);
			var foilCard = Scraper.ScrapeIsFoil(cardNodes[1]);

			// Assert
			Assert.IsFalse(nonFoilCard);
			Assert.IsTrue(foilCard);
		}

		[TestMethod()]
		public void ScrapeCardConditionDetails_ReturnsCardCondition()
		{
			// Arrange
			var mockHtml = @"<form action='/card/add' method='get' class='addToCartForm'>
									<input name='style[0]' value='NM' />
									<input name='price' value='0.49' />
									<input name='maxQty' value='0' />
							</form>";

			var htmlDocument = new HtmlDocument();
			htmlDocument.LoadHtml(mockHtml);
			var cardNode = htmlDocument.DocumentNode.SelectSingleNode("//form[@class='addToCartForm']");

			// Act
			var cardCondition = Scraper.ScrapeCardConditionDetails(cardNode);

			// Assert
			Assert.AreEqual(Condition.NM, cardCondition.Condition);
			Assert.AreEqual(0.49, cardCondition.Price);
			Assert.AreEqual(0, cardCondition.Quantity);
		}

		[TestMethod()]
		public void ScrapeCardConditions_ReturnsListCardConditions()
		{
			// Arrange
			var mockHtml = @"<div class='productItemWrapper productCardWrapper'>
								<form action='/card/add' method='get' class='addToCartForm'>
										<input name='style[0]' value='NM' />
										<input name='price' value='0.49' />
										<input name='maxQty' value='0' />
								</form>
								<form action='/card/add' method='get' class='addToCartForm'>
									<input name='style[0]' value='EX' />
									<input name='price' value='0.39' />
									<input name='maxQty' value='8' />
							   </form>
							  <form action='/card/add' method='get' class='addToCartForm'>
									<input name='style[0]' value='VG' />
									<input name='price' value='0.29' />
									<input name='maxQty' value='0' />
							   </form>
							   <form action='/card/add' method='get' class='addToCartForm'>
									<input name='style[0]' value='G' />
									<input name='price' value='0.20' />
									<input name='maxQty' value='0' />
							   </form>
							</div>";

			var htmlDocument = new HtmlDocument();
			htmlDocument.LoadHtml(mockHtml);
			var cardNode = htmlDocument.DocumentNode.SelectSingleNode("//div[@class='productItemWrapper productCardWrapper']");

			// Act
			(var NMPrice, var cardConditions) = Scraper.ScrapeCardConditions(cardNode);

			// Assert
			Assert.AreEqual(0.49, NMPrice);
			Assert.AreEqual(Condition.NM, cardConditions[0].Condition);
			Assert.AreEqual(Condition.EX, cardConditions[1].Condition);
			Assert.AreEqual(Condition.VG, cardConditions[2].Condition);
			Assert.AreEqual(Condition.G, cardConditions[3].Condition);
		}

		[TestMethod()]
		public void ScrapeCardRarity_ReturnRarityEnum()
		{
			var mockHtml = @"
							<div class='productDetailTitle'>
								<div class='productDetailSet'><a> Unfinity (C) </a></div>
							</div>
							<div class='productDetailTitle'>
								<div class='productDetailSet'><a> Unfinity (U) </a></div>
							</div>
							<div class='productDetailTitle'>
								<div class='productDetailSet'><a> Unfinity (R) </a></div>
							</div>
							<div class='productDetailTitle'>
								<div class='productDetailSet'><a> Unfinity (M) </a></div>
							</div>";

			var htmlDocument = new HtmlDocument();
			htmlDocument.LoadHtml(mockHtml);
			var cardNodes = htmlDocument.DocumentNode.SelectNodes("//div[@class='productDetailTitle']");

			// Act
			var commonRarity = Scraper.ScrapeCardRarity(cardNodes[0]);
			var uncommonRarity = Scraper.ScrapeCardRarity(cardNodes[1]);
			var rareRarity = Scraper.ScrapeCardRarity(cardNodes[2]);
			var mythicRareRarity = Scraper.ScrapeCardRarity(cardNodes[3]);

			// Assert
			Assert.AreEqual(Rarity.Common, commonRarity);
			Assert.AreEqual(Rarity.Uncommon, uncommonRarity);
			Assert.AreEqual(Rarity.Rare, rareRarity);
			Assert.AreEqual(Rarity.Mythic_Rare, mythicRareRarity);
		}

		[TestMethod()]
		public void HasFoilTab_ReturnsTrue()
		{
			// Arrange
			var mockHtml = @"<li role='presentation'>
								<a href='/catalog/search?filter[tab]=mtg_single'>Singles (X)</a>
							</li>
							<li role='presentation'>
								<a href='/catalog/search?filter[tab]=mtg_foil'>Foils (X)</a>
							</li>";

			var htmlDocument = new HtmlDocument();
			htmlDocument.LoadHtml(mockHtml);
			var tabs = htmlDocument.DocumentNode.SelectNodes("//li[@role='presentation']");

			// Act
			(var hasFoil, var url) = Scraper.HasFoilTab(tabs);

			// Assert
			Assert.IsTrue(hasFoil);
			Assert.AreEqual("https://www.cardkingdom.com/catalog/search?filter[tab]=mtg_foil", url);
		}

		[TestMethod()]
		public void HasFoilTab_ReturnsFalse()
		{
			// Arrange
			var mockHtml = @"<li role='presentation'>
								<a href='/catalog/search?filter[tab]=mtg_single'>Singles (X)</a>
							</li>";

			var htmlDocument = new HtmlDocument();
			htmlDocument.LoadHtml(mockHtml);
			var tabs = htmlDocument.DocumentNode.SelectNodes("//li[@role='presentation']");

			// Act
			(var hasFoil, var url) = Scraper.HasFoilTab(tabs);

			// Assert
			Assert.IsFalse(hasFoil);
			Assert.AreEqual("", url);
		}
	}


	[TestClass()]
	public class ScrapingServiceTests
	{
		private DataContext GetInMemoryDbContext()
		{
			var options = new DbContextOptionsBuilder<DataContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString())
				.Options;

			return new DataContext(options);
		}

		[TestMethod()]
		public async Task AddEditions_AddsNewEditionToDatabase()
		{
			// Arrange
			var context = GetInMemoryDbContext();
			var service = new ScrapingService(context);

			var editions = new List<Edition>
			{
				new Edition { Name = "Edition 1", Code = "edition-1" },
				new Edition { Name = "Edition 2", Code = "edition-2" }
			};

			// Act
			await service.AddEditions(editions);

			// Assert
			var addedEditions = await context.Editions.ToListAsync();
			Assert.AreEqual(2, addedEditions.Count);
			Assert.IsTrue(addedEditions.Contains(editions[0]));
			Assert.IsTrue(addedEditions.Contains(editions[1]));
		}

		[TestMethod()]
		public async Task AddEditions_IgnoresAddingSameEditions()
		{
			// Arrange
			var context = GetInMemoryDbContext();
			var service = new ScrapingService(context);

			var editions = new List<Edition>
			{
				new Edition { Name = "Edition 1", Code = "edition-1" },
				new Edition { Name = "Edition 2", Code = "edition-2" },
			};

			// Act
			await service.AddEditions(editions);
			await service.AddEditions(editions);

			// Assert
			var addedEditions = await context.Editions.ToListAsync();
			Assert.AreEqual(2, addedEditions.Count);
			Assert.IsTrue(addedEditions.Contains(editions[0]));
			Assert.IsTrue(addedEditions.Contains(editions[1]));
		}
	}
}