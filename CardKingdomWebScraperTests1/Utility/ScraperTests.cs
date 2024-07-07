using CardKingdomWebScraper.Models;
using HtmlAgilityPack;
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
	}
}