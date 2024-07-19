# CardKingdomWebScraper
The data scraped from CardKingdom is used to populate the database used by my website. I decided not to deploy this as an Azure App Function but will instead just run it manually.

## Other Repositories:
- Web API: https://github.com/burgerax5/MagicGatherer_API
- Frontend: https://github.com/burgerax5/CardKingdomWebScraper

## Setup
To get the app up and running, you will need to change the connection string and connect to a database:
- In `appsettings.json` replace the value of the key **DefaultConnection** with your database connection string
- In `Program.cs` replace the value of the `connectionString` with your connection string

Afterwards, in Visual Studio open up the **Package Manager Console** and enter the commands:
```
Add-Migration <NameOfMigration>
Update-Database
```

It should be runnable now, assuming all the necessary packages are installed.
