using CardKingdomWebScraper.Data;
using CardKingdomWebScraper.Models;
using Microsoft.EntityFrameworkCore;

namespace CardKingdomWebScraper.Utility
{
	public class ScrapingService
	{
        private readonly DataContext _context;
        public ScrapingService(DataContext context)
        {
            _context = context;
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
            await UpsertCards(scrapedCards);
        }

        public async Task ScrapeAllCards()
        {
            await ScrapeEditionNames();
            List<Edition> editions = await _context.Editions.ToListAsync();
            Console.WriteLine($"Editions: {editions.Count}");
            foreach (Edition edition in editions)
            {
                Console.WriteLine($"Scraping: {edition.Name}");
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
				catch (Exception)
				{
					await transaction.RollbackAsync();
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

        public async Task UpsertCards(List<Card> cards)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
				foreach (Card card in cards)
				{
					var existingCard = await _context.Cards
                        .Include(c => c.Conditions)
                        .FirstOrDefaultAsync(c => c.Name == card.Name && c.EditionId == card.EditionId && c.IsFoil == card.IsFoil);

					if (existingCard == null)
						await _context.Cards.AddAsync(card);
					else
                    {
                        existingCard.Conditions = card.Conditions;
                        _context.Cards.Update(existingCard);
                    }
				}

				await _context.SaveChangesAsync();
                await transaction.CommitAsync();
			}
            catch(Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
