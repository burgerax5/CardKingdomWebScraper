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
	}
}