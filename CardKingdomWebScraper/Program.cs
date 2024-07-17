using CardKingdomWebScraper.Data;
using Microsoft.Extensions.DependencyInjection;
using CardKingdomWebScraper.Utility;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using DotNetEnv;

public class WebScraper
{

	public static async Task Main()
	{
		Env.Load();

		var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");

		var serviceProvider = new ServiceCollection()
			.AddDbContext<DataContext>(options =>
				options.UseSqlServer(connectionString))
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