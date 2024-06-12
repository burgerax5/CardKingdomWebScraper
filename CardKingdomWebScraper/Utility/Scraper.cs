using HtmlAgilityPack;
using System;
using CardKingdomWebScraper.Models;
using System.Net;
using System.Threading;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using System.Collections.ObjectModel;
using OpenQA.Selenium.Support.UI;

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
				WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

				// Visit CardKingdom website
				driver.Navigate().GoToUrl("https://www.cardkingdom.com");

				// Add cookie to modify max results to 1000
				driver.Manage().Cookies.AddCookie(new OpenQA.Selenium.Cookie("limit", "1000"));

				while (hasNextPage)
				{
					driver.Navigate().GoToUrl(url);

					// Wait for the card elements to be loaded
					wait.Until(d => d.FindElement(By.XPath("//div[@class='productItemWrapper productCardWrapper']")));

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
					}

					var pagination = driver.FindElement(By.XPath("//ul[@class='pagination justify-content-center']"));
					var tabs = driver.FindElements(By.XPath("//div[@class='categoryTabs']//ul[@class='nav nav-tabs subtab singles']/li[@role='presentation']"));
					var nextPageButton = pagination != null ? pagination.FindElement(By.XPath("./li[@class='page-item']/a[@aria-label='Next']")) : null;

					// If there are no more pages left, check the foils.
					// If there are no more pages in the foil tab, exit function.
					if (pagination == null || nextPageButton == null)
					{
						hasNextPage = false;
						(bool hasFoilTab, url) = HasFoilTab(tabs);
						if (hasFoilTab && !inFoilsTab)
						{
							inFoilsTab = true;
							hasNextPage = true;
						}

						if (!hasNextPage) break;
					}

					// Asynchronous delay before going to next page
					await Task.Delay(5000);

					if (nextPageButton != null)
						url = WebUtility.HtmlDecode(nextPageButton.GetAttribute("href"));
				}
			}

			return cards;
		}

		private static string ScrapeCardName(IWebElement card)
		{
			var cardNameElement = card.FindElement(By.XPath(".//span[@class='productDetailTitle']"));
			return WebUtility.HtmlDecode(cardNameElement.Text);
		}
		private static string ScrapeCardImageURL(IWebElement card)
		{
			var cardImage = card.FindElement(By.XPath(".//img[@class='card-image']"));
			return "www.cardkingdom.com" + cardImage.GetAttribute("src");
		}

		private static bool ScrapeIsFoil(IWebElement card)
		{
			try
			{
				var foilElement = card.FindElement(By.XPath(".//div[@class='foil']"));
				return foilElement != null;
			} 
			catch (OpenQA.Selenium.NoSuchElementException)
			{
				return false;
			} 
			
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
		public static string? GetNextPage(IWebElement htmlDocument)
		{
			var pagination = htmlDocument.FindElement(By.XPath("//ul[@class=\"pagination justify-content-center\"]"));
			var nextPageButton = pagination.FindElement(By.XPath("./li[@class=\"page-item\"]/a[@aria-label=\"Next\"]"));
			if (nextPageButton == null)
				return null;

			string nextPageURL = nextPageButton.GetAttribute("href");
			return nextPageURL;
		}

		public static (bool hasFoilTab, string url) HasFoilTab(ReadOnlyCollection<IWebElement> tabs)
		{
			string url = "";
			foreach (IWebElement tab in tabs)
			{
				if (tab.FindElement(By.XPath("a")).Text.Contains("Foils"))
					return (true, url = "https://www.cardkingdom.com" + tab.FindElement(By.XPath("a")).GetAttribute("href"));
			}
			return (false, url);
		}
	}
}
