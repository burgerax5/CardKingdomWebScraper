using HtmlAgilityPack;
using System;
using CardKingdomWebScraper.Models;
using System.Net;
using System.Threading;

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
			var httpClientHandler = new HttpClientHandler { CookieContainer = new CookieContainer() };
			httpClientHandler.CookieContainer.Add(new Cookie("limit", "1000", "/", "www.cardkingdom.com"));

			using var httpClient = new HttpClient(httpClientHandler);
			var htmlDocument = new HtmlDocument();

			List<Card> cards = new List<Card>();
			bool hasNextPage = true;
			bool inFoilsTab = false;

			while (hasNextPage)
			{
				var html = await httpClient.GetStringAsync(url);
				htmlDocument.LoadHtml(html);

				HtmlNodeCollection cardElements = htmlDocument.DocumentNode.SelectNodes("//div[@class='productItemWrapper productCardWrapper']");
				if (cardElements == null) 
				{ 
					Console.WriteLine("Could not find any cards on this page."); 
					break; 
				}

				foreach (HtmlNode card in cardElements)
				{
					Card _card = new Card
					{
						Name = ScrapeCardName(card),
						ImageURL = ScrapeCardImageURL(card),
						IsFoil = ScrapeIsFoil(card),
						Edition = edition,
						EditionId = edition.Id,
						Conditions = ScrapeCardConditions(card),
						Rarity = ScrapeCardRarity(card),
					};

					cards.Add(_card);
				}

				HtmlNode pagination = htmlDocument.DocumentNode.SelectSingleNode("//ul[@class='pagination justify-content-center']");
				HtmlNodeCollection tabs = htmlDocument.DocumentNode.SelectNodes("//div[@class='categoryTabs']//ul[@class='nav nav-tabs subtab singles']/li[@role='presentation']");
				HtmlNode? nextPageButton = pagination != null ? pagination.SelectSingleNode("./li[@class='page-item']/a[@aria-label='Next']") : null;

				/*
                 If there are no more pages left, check the foils.
                 If there are no more pages in the foil tab, exit function.
                 */
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

				if (nextPageButton != null) url = WebUtility.HtmlDecode(nextPageButton.GetAttributeValue("href", ""));
			}

			return cards;
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

		private static List<CardCondition> ScrapeCardConditions(HtmlNode card)
		{
			List<CardCondition> cardConditions = new List<CardCondition>();

			HtmlNodeCollection cardForm = card.SelectNodes(".//form[@class='addToCartForm']");
			foreach (HtmlNode cardFormInput in cardForm)
			{
				CardCondition cardCondition = ScrapeCardConditionDetails(cardFormInput);
				cardConditions.Add(cardCondition);
			}

			return cardConditions;
		}

		private static CardCondition ScrapeCardConditionDetails(HtmlNode cardFormInput)
		{
			// Condition
			string cardConditionName = cardFormInput.SelectSingleNode("./input[@name='style[0]']")
													.GetAttributeValue("value", "");
			Enum.TryParse(cardConditionName, out Condition condition);

			// Price
			HtmlNode cardConditionPriceElement = cardFormInput.SelectSingleNode("./input[@name='price']");
			double cardConditionPrice = Convert.ToDouble(cardConditionPriceElement
											   .GetAttributeValue("value", ""));

			// Quantity
			HtmlNode cardConditionQuantityElement = cardFormInput.SelectSingleNode("./input[@name='maxQty']");
			int cardConditionQuantity = Convert.ToInt32(cardConditionQuantityElement
											   .GetAttributeValue("value", ""));
			cardConditionQuantity = cardConditionQuantity < 0 ? 0 : cardConditionQuantity;

			CardCondition cardCondition = new CardCondition
			{
				Condition = condition,
				Price = cardConditionPrice,
				Quantity = cardConditionQuantity
			};

			return cardCondition;
		}

		private static Rarity ScrapeCardRarity(HtmlNode card)
		{
			HtmlNode productDetailSet = card.SelectSingleNode(".//div[@class='productDetailSet']");
			var editionNameAndRarity = productDetailSet.SelectSingleNode("a").InnerText;
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
