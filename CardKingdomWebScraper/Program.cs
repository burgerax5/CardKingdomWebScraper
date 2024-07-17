using CardKingdomWebScraper.Data;
using Microsoft.Extensions.DependencyInjection;
using CardKingdomWebScraper.Utility;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class WebScraper
{

	public static async Task Main()
	{
		var serviceProvider = new ServiceCollection()
			.AddDbContext<DataContext>(options => 
				options.UseSqlServer("Data Source=DESKTOP-FQB978B\\SQLEXPRESS;Initial Catalog=mtg;Integrated Security=True;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False"))
			.AddLogging(configure => configure.AddConsole())
			.AddTransient<ScrapingService>()
			.BuildServiceProvider();

		try
		{
			// Perform an initial scraping run
			using (var scope = serviceProvider.CreateScope())
			{
				var scrapingService = scope.ServiceProvider.GetRequiredService<ScrapingService>();
				await ScrapeData(scrapingService);
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"An error occurred: {ex.Message}");
			Console.WriteLine(ex.StackTrace);
		}
	}

	public static async Task ScrapeData(ScrapingService scrapingService)
	{
		Console.WriteLine("Intiating web scraper...");
		await scrapingService.ScrapeAllCards();
	}
}