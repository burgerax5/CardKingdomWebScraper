using CardKingdomWebScraper.Data;
using CardKingdomWebScraper.Models;
using Microsoft.Extensions.DependencyInjection;
using CardKingdomWebScraper.Utility;
using Microsoft.EntityFrameworkCore;

public class WebScraper
{
	private static System.Timers.Timer? _timer;

	public static async Task Main()
	{
		var serviceProvider = new ServiceCollection()
			.AddDbContext<DataContext>(options => 
				options.UseSqlServer("Data Source=DESKTOP-FQB978B\\SQLEXPRESS;Initial Catalog=mtg;Integrated Security=True;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False"))
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

			// Set up a timer to run the scraping service periodically
			_timer = new System.Timers.Timer(1000 * 60 * 60 * 24);
			_timer.Elapsed += async (sender, e) => await OnTimedEvent(serviceProvider);
			_timer.AutoReset = true;
			_timer.Enabled = true;

			Console.WriteLine("Press the Enter key to exit the program at any time... ");
			Console.ReadLine();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"An error occurred: {ex.Message}");
			Console.WriteLine(ex.StackTrace);
		}
	}

	private static async Task OnTimedEvent(IServiceProvider serviceProvider)
	{
		using (var scope = serviceProvider.CreateScope())
		{
			var scrapingService = scope.ServiceProvider.GetRequiredService<ScrapingService>();
			await ScrapeData(scrapingService);
		}
	}

	public static async Task ScrapeData(ScrapingService scrapingService)
	{
		Console.WriteLine("Intiating web scraper...");
		await scrapingService.ScrapeAllCards();
	}
}