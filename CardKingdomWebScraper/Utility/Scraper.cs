using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CardKingdomWebScraper.Models;

namespace CardKingdomWebScraper.Utility
{
	public class Scraper
	{
		public static async Task<List<Edition>> GetEditionNames()
		{
			string url = "https://www.cardkingdom.com/catalog/magic_the_gathering/by_az";

			using var httpClient = new HttpClient();
			var html = await httpClient.GetStringAsync(url);
			var htmlDocument = new HtmlDocument();
			htmlDocument.LoadHtml(html);

			var editions = ScrapeEditionNames(htmlDocument);
			return editions;
		}

		public static List<Edition> ScrapeEditionNames(HtmlDocument htmlDocument)
		{
			List<Edition> editions = new List<Edition>();

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

		public static async Task<List<Card>> GetCardsFromEdition(Edition edition)
		{
			string url = "https://www.cardkingdom.com/mtg/" + edition.Code;
			var httpClientHandler = new HttpClientHandler { CookieContainer = new CookieContainer() };
			httpClientHandler.CookieContainer.Add(new Cookie("limit", "1000", "/", "www.cardkingdom.com"));
			httpClientHandler.CookieContainer.Add(new Cookie("sortBy", "name_asc", "/", "www.cardkingdom.com"));

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
					(var NMPrice, var conditions) = ScrapeCardConditions(card);

					Card _card = new Card
					{
						Name = ScrapeCardName(card),
						ImageURL = ScrapeCardImageURL(card),
						IsFoil = ScrapeIsFoil(card),
						Edition = edition,
						EditionId = edition.Id,
						Conditions = conditions,
						Rarity = ScrapeCardRarity(card),
						NMPrice = NMPrice
					};

					cards.Add(_card);
				}

				HtmlNode pagination = htmlDocument.DocumentNode.SelectSingleNode("//ul[@class='pagination justify-content-center']");
				HtmlNodeCollection tabs = htmlDocument.DocumentNode.SelectNodes("//div[@class='categoryTabs']//ul[@class='nav nav-tabs subtab singles']/li[@role='presentation']");
				HtmlNode? nextPageButton = pagination?.SelectSingleNode("./li[@class='page-item']/a[@aria-label='Next']");

				if (pagination == null || nextPageButton == null)
				{
					hasNextPage = false;
					(bool hasFoilTab, string nextUrl) = HasFoilTab(tabs);
					if (hasFoilTab && !inFoilsTab)
					{
						inFoilsTab = true;
						hasNextPage = true;
						url = nextUrl;
					}

					if (!hasNextPage) break;
				}

				await Task.Delay(5000);

				if (nextPageButton != null) url = WebUtility.HtmlDecode(nextPageButton.GetAttributeValue("href", ""));
			}

			return cards;
		}

		public static string ScrapeCardName(HtmlNode card)
		{
			HtmlNode cardNameElement = card.SelectSingleNode(".//span[@class='productDetailTitle']");
			return WebUtility.HtmlDecode(cardNameElement.InnerText);
		}

		public static string ScrapeCardImageURL(HtmlNode card)
		{
			HtmlNode cardImage = card.SelectSingleNode(".//mtg-card-image");
			return "www.cardkingdom.com" + cardImage.GetAttributeValue("src", "");
		}

		public static bool ScrapeIsFoil(HtmlNode card)
		{
			HtmlNode foilElement = card.SelectSingleNode(".//div[@class='foil']");
			return foilElement != null;
		}

		public static (double NMPrice, List<CardCondition>) ScrapeCardConditions(HtmlNode card)
		{
			List<CardCondition> cardConditions = new List<CardCondition>();

			HtmlNodeCollection cardForm = card.SelectNodes(".//form[@class='addToCartForm']");
			foreach (HtmlNode cardFormInput in cardForm)
			{
				CardCondition cardCondition = ScrapeCardConditionDetails(cardFormInput);
				cardConditions.Add(cardCondition);
			}

			return (cardConditions[0].Price, cardConditions);
		}

		public static CardCondition ScrapeCardConditionDetails(HtmlNode cardFormInput)
		{
			string cardConditionName = cardFormInput.SelectSingleNode("./input[@name='style[0]']").GetAttributeValue("value", "");
			Enum.TryParse(cardConditionName, out Condition condition);

			HtmlNode cardConditionPriceElement = cardFormInput.SelectSingleNode("./input[@name='price']");
			double cardConditionPrice = Convert.ToDouble(cardConditionPriceElement.GetAttributeValue("value", ""));

			HtmlNode cardConditionQuantityElement = cardFormInput.SelectSingleNode("./input[@name='maxQty']");
			int cardConditionQuantity = Convert.ToInt32(cardConditionQuantityElement.GetAttributeValue("value", ""));
			cardConditionQuantity = cardConditionQuantity < 0 ? 0 : cardConditionQuantity;

			CardCondition cardCondition = new CardCondition
			{
				Condition = condition,
				Price = cardConditionPrice,
				Quantity = cardConditionQuantity
			};

			return cardCondition;
		}

		public static Rarity ScrapeCardRarity(HtmlNode card)
		{
			HtmlNode productDetailSet = card.SelectSingleNode(".//div[@class='productDetailSet']");
			var editionNameAndRarity = productDetailSet.SelectSingleNode("a").InnerText;
			var rarity = editionNameAndRarity.Trim().Replace("\n", "")[^3..];

			return rarity switch
			{
				"(U)" => Rarity.Uncommon,
				"(R)" => Rarity.Rare,
				"(M)" => Rarity.Mythic_Rare,
				_ => Rarity.Common,
			};
		}

		public static string? GetNextPage(HtmlDocument htmlDocument)
		{
			HtmlNode pagination = htmlDocument.DocumentNode.SelectSingleNode("//ul[@class='pagination justify-content-center']");
			HtmlNode? nextPageButton = pagination?.SelectSingleNode("./li[@class='page-item']/a[@aria-label='Next']");
			return nextPageButton?.GetAttributeValue("href", null);
		}

		public static (bool hasFoilTab, string url) HasFoilTab(HtmlNodeCollection tabs)
		{
			foreach (HtmlNode tab in tabs)
			{
				if (tab.SelectSingleNode("a").InnerText.Contains("Foils"))
					return (true, "https://www.cardkingdom.com" + tab.SelectSingleNode("a").GetAttributeValue("href", ""));
			}
			return (false, "");
		}
	}
}
