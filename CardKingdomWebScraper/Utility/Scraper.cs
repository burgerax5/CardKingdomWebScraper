using HtmlAgilityPack;
using System;
using CardKingdomWebScraper.Models;
using System.Net;
using System.Threading;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;

namespace CardKingdomWebScraper.Utility
{
	public class Scraper
	{
		public static async Task<List<Edition>> ScrapeEditionNames()
		{
			string url = "https://www.cardkingdom.com/catalog/magic_the_gathering/by_az";

			// Send GET request to CardKingdom
			using var httpClient = new HttpClient();
			var html = await httpClient.GetStringAsync(url);
			var htmlDocument = new HtmlDocument();
			htmlDocument.LoadHtml(html);

			List<Edition> editions = new List<Edition>();

			// Retrieve edition names
			HtmlNode editionContainer = htmlDocument.DocumentNode.SelectSingleNode("//div[@class='row anchorList']");

			if (editionContainer == null) return editions;

			HtmlNodeCollection editionAnchors = editionContainer.SelectNodes(".//a");
			if (editionAnchors == null) return editions;

			foreach (var editionAnchor in editionAnchors)
			{
				string editionName = WebUtility.HtmlDecode(editionAnchor.InnerText.Trim());
				string editionURL = editionAnchor.GetAttributeValue("href", null);
				if (editionURL == null) continue;
				string editionCode = editionURL.Substring(editionURL.LastIndexOf("/") + 1);

				Edition edition = new Edition
				{
					Name = editionName,
					Code = editionCode,
				};
				editions.Add(edition);
			}

			return editions;
		}

		public static async Task<List<Card>> ScrapeCardsFromEdition(Edition edition)
		{
			string url = "https://www.cardkingdom.com/mtg/" + edition.Code;
			List<Card> cards = new List<Card>();
			bool hasNextPage = true;
			bool inFoilsTab = false;

			// Open Chrome in headless mode
			ChromeOptions chromeOptions = new ChromeOptions();
			chromeOptions.AddArguments("headless");

			using (var driver = new ChromeDriver())
			{
				while (hasNextPage)
				{
					driver.Navigate().GoToUrl(url);
					var cardElements = driver.FindElements(By.XPath("//div[@class='productItemWrapper productCardWrapper']"));

					if (cardElements == null)
					{
						Console.WriteLine("Could not find any cards on this page."); 
						break;
					}

					foreach (var cardEl in cardElements)
					{
						Card card = new Card
						{
							Name = ScrapeCardName(cardEl),
							ImageURL = ScrapeCardImageURL(cardEl),
							IsFoil = ScrapeIsFoil(cardEl),
							Edition = edition,
							EditionId = edition.Id,
							Conditions = ScrapeCardConditions(cardEl),
							Rarity = ScrapeCardRarity(cardEl)
						};

						cards.Add(card);

						var pagination = driver.FindElement(By.XPath("//ul[@class='pagination justify-content-center']"));
						var tabs = driver.FindElements(By.XPath("//div[@class='categoryTabs']//ul[@class='nav nav-tabs subtab singles']/li[@role='presentation']"));
						var nextPageButton = pagination != null ? pagination.FindElement(By.XPath("./li[@class='page-item']/a[@aria-label='Next']")) : null;
					}
				}
			}
		}

		private static string ScrapeCardName(HtmlNode card)
		{
			HtmlNode cardNameElement = card.SelectSingleNode(".//span[@class='productDetailTitle']");
			return WebUtility.HtmlDecode(cardNameElement.InnerText);
		}
		private static string ScrapeCardImageURL(HtmlNode card)
		{
			HtmlNode cardImage = card.SelectSingleNode(".//mtg-card-image");
			return "www.cardkingdom.com" + cardImage.GetAttributeValue("src", "");
		}

		private static bool ScrapeIsFoil(HtmlNode card)
		{
			HtmlNode foilElement = card.SelectSingleNode(".//div[@class='foil']");
			return foilElement != null;
		}

		private static List<CardCondition> ScrapeCardConditions(IWebElement card)
		{
			List<CardCondition> cardConditions = new List<CardCondition>();

			var cardForm = card.FindElements(By.XPath(".//form[@class='addToCartForm']"));
			foreach (var cardFormInput in cardForm)
			{
				CardCondition cardCondition = ScrapeCardConditionDetails(cardFormInput);
				cardConditions.Add(cardCondition);
			}

			return cardConditions;
		}

		private static CardCondition ScrapeCardConditionDetails(IWebElement cardFormInput)
		{
			// Condition
			string cardConditionName = cardFormInput.FindElement(By.XPath("./input[@name='style[0]']"))
												 .GetAttribute("value");

			Enum.TryParse(cardConditionName, out Condition condition);

			// Price
			var cardConditionPriceElement = cardFormInput.FindElement(By.XPath("./input[@name='price']"));
			double cardConditionPrice = Convert.ToDouble(cardConditionPriceElement
											   .GetAttribute("value"));

			// Quantity
			var cardConditionQuantityElement = cardFormInput.FindElement(By.XPath("./input[@name='maxQty']"));
			int cardConditionQuantity = Convert.ToInt32(cardConditionQuantityElement
											   .GetAttribute("value"));
			cardConditionQuantity = cardConditionQuantity < 0 ? 0 : cardConditionQuantity;

			CardCondition cardCondition = new CardCondition
			{
				Condition = condition,
				Price = cardConditionPrice,
				Quantity = cardConditionQuantity
			};

			return cardCondition;
		}

		private static Rarity ScrapeCardRarity(IWebElement card)
		{
			var productDetailSet = card.FindElement(By.XPath(".//div[@class='productDetailSet']"));
			var editionNameAndRarity = productDetailSet.FindElement(By.XPath("a")).Text;
			var rarity = editionNameAndRarity.Trim().Replace("\n", "")[^3..];

			switch (rarity)
			{
				case "(U)": return Rarity.Uncommon;
				case "(R)": return Rarity.Rare;
				case "(M)": return Rarity.Mythic_Rare;
				default: return Rarity.Common;
			}
		}

		/*
		 Return URL of next page if there is a next page, otherwise return null
		 */
		public static string? GetNextPage(HtmlDocument htmlDocument)
		{
			HtmlNode pagination = htmlDocument.DocumentNode.SelectSingleNode("//ul[@class=\"pagination justify-content-center\"]");
			HtmlNode nextPageButton = pagination.SelectSingleNode("./li[@class=\"page-item\"]/a[@aria-label=\"Next\"]");
			if (nextPageButton == null)
				return null;

			string nextPageURL = nextPageButton.GetAttributeValue("href", "");
			return nextPageURL;
		}

		public static (bool hasFoilTab, string url) HasFoilTab(HtmlNodeCollection tabs)
		{
			string url = "";
			foreach (HtmlNode tab in tabs)
			{
				if (tab.SelectSingleNode("a").InnerText.Contains("Foils"))
					return (true, url = "https://www.cardkingdom.com" + tab.SelectSingleNode("a").GetAttributeValue("href", ""));
			}
			return (false, url);
		}
	}
}
