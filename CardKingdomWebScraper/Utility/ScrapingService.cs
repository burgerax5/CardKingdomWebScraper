using CardKingdomWebScraper.Data;
using CardKingdomWebScraper.Models;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace CardKingdomWebScraper.Utility
{
	public class ScrapingService
	{
        private readonly DataContext _context;
		private readonly ILogger<ScrapingService> _logger;

        public ScrapingService(DataContext context, ILogger<ScrapingService> logger)
        {
            _context = context;
			_logger = logger;
        }

        public async Task<List<Edition>> ScrapeEditionNames()
        {
            List<Edition> editions = await Scraper.GetEditionNames();
            await AddEditions(editions);
            return editions;
        }

        public async Task ScrapeEditionCards(Edition edition)
        {
			List<Card> scrapedCards = await Scraper.GetCardsFromEdition(edition);
            await UpsertCards(scrapedCards, edition.Id);
        }

        public async Task ScrapeAllCards()
        {
            await ScrapeEditionNames();
            List<Edition> editions = await _context.Editions.ToListAsync();
			_logger.LogInformation($"Editions: {editions.Count}");
            foreach (Edition edition in editions)
            {
				_logger.LogInformation($"Scraping: {edition.Name}");
				await ScrapeEditionCards(edition);
			}
		}

        public async Task AddEditions(List<Edition> editions)
        {
            if (_context.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
            {
				using var transaction = await _context.Database.BeginTransactionAsync();
				try
				{
                    await AddEditionsInternal(editions);
					await transaction.CommitAsync();
				}
				catch (Exception ex)
				{
					await transaction.RollbackAsync();
					_logger.LogError(ex, "Error while adding editions.");
					throw;
				}
			} else
            {
                await AddEditionsInternal(editions);
            }
        }

        private async Task AddEditionsInternal(List<Edition> editions)
        {
			foreach (Edition edition in editions)
			{
				bool editionExists = await _context.Editions.AnyAsync(e => e.Name == edition.Name);
				if (!editionExists)
					await _context.Editions.AddAsync(edition);
			}

			await _context.SaveChangesAsync();
		}

        public async Task UpsertCards(List<Card> cards, int editionId)
        {
            if (_context.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
            {
				await using var transaction = await _context.Database.BeginTransactionAsync();
				try
				{
					await UpsertCardsInternal(cards, editionId);
					await transaction.CommitAsync();
				}
				catch (Exception ex)
				{
					await transaction.RollbackAsync();
					_logger.LogError(ex, "Error while upserting cards.");
					throw;
				}
			} else
			{
				await UpsertCardsInternal(cards, editionId);
			}
        }

		private async Task UpsertCardsInternal(List<Card> cards, int editionId)
		{
			try
			{
				var batchSize = 100;
				for (int i = 0; i < cards.Count; i += batchSize)
				{
					var batch = cards.Skip(i).Take(batchSize).ToList();
					await ProcessBatch(batch, editionId);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error while upserting cards for edition: {cards[0]?.Edition?.Name}");
				throw;
			}
		}


		private async Task ProcessBatch(List<Card> cards, int editionId)
		{
			var existingCards = await _context.Cards
								.Include(c => c.Conditions)
								.Where(c => c.EditionId == editionId)
								.ToListAsync();

			var cardsToAdd = new List<Card>();
			var cardsToUpdate = new List<Card>();

			foreach (Card card in cards)
			{
				// Ensure the Edition entity is tracked by the context
				var existingEdition = await _context.Editions.FindAsync(card.EditionId);
				if (existingEdition != null)
				{
					card.Edition = existingEdition;
				}

				var existingCard = existingCards.Find(c => c.Name == card.Name && c.EditionId == card.EditionId && c.IsFoil == card.IsFoil);

				if (existingCard == null)
				{
					cardsToAdd.Add(card);
				}
				else
				{
					existingCard.NMPrice = card.NMPrice;
					existingCard.Conditions = card.Conditions;
					cardsToUpdate.Add(existingCard);
				}
			}

			if (cardsToAdd.Any()) await _context.Cards.AddRangeAsync(cardsToAdd);
			if (cardsToUpdate.Any()) _context.Cards.UpdateRange(cardsToUpdate);

			await _context.SaveChangesAsync();
		}
	}
}
