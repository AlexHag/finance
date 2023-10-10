using Microsoft.AspNetCore.Mvc;
using MarketSim.Core.Entities;
using MarketSim.Core.Database;

namespace Alex.Market.Service.Controllers;

[ApiController]
[Route("[controller]")]
public class SeedController : ControllerBase
{
    private readonly ILogger<SeedController> _logger;
    private readonly AppDbContext _dbContext;

    public SeedController(ILogger<SeedController> logger, AppDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    [HttpGet("Stock")]
    public async Task<IActionResult> Stocks()
    {
        if (_dbContext.Stocks.Any())
            return BadRequest("Already seeded");

        var amzn = CreateStock("AMZN", "Amazon");
        var intc = CreateStock("INTC", "Intel");
        var msft = CreateStock("MSFT", "Microsoft");
        var nvda = CreateStock("NVDA", "Nvidia");
        var tsla = CreateStock("TSLA", "Tesla");
        var aapl = CreateStock("AAPL", "Apple");

        await _dbContext.Stocks.AddAsync(amzn);
        await _dbContext.Stocks.AddAsync(intc);
        await _dbContext.Stocks.AddAsync(msft);
        await _dbContext.Stocks.AddAsync(nvda);
        await _dbContext.Stocks.AddAsync(tsla);
        await _dbContext.Stocks.AddAsync(aapl);

        await _dbContext.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("SystemSettings")]
    public async Task<IActionResult> SystemSettings([FromBody] DateTime today)
    {
        if (today.DayOfWeek == DayOfWeek.Saturday || today.DayOfWeek == DayOfWeek.Sunday)
            return BadRequest($"Today ({today}) cannot be a weekend ({today.DayOfWeek}), markets are closed on weekends");
        
        if (!_dbContext.SystemSettings.Any())
        {
            await _dbContext.SystemSettings.AddAsync(new SystemSettings
            {
                CurrentDay = today
            });
        }
        else
        {
            var system = _dbContext.SystemSettings.FirstOrDefault()!;
            system.CurrentDay = today;
        }
        await _dbContext.SaveChangesAsync();
        return Ok(today.DayOfWeek.ToString());
    }

    private Stock CreateStock(string ticker, string name)
    {
        var stock = new Stock
        {
            Name = name,
            Ticker = ticker
        };

        var data = System.IO.File.ReadAllLines($"../Stocks/{ticker}.csv");
        for (int i = 1; i < data.Count(); i++)
        {
            var row = data[i].Split(",");
            var price = new StockPrice
            {
                Date = DateTime.Parse(row[0]),
                Open = Double.Parse(row[1]),
                Close = Double.Parse(row[3])
            };
            stock.StockPrices.Add(price);
        }

        return stock;
    }
}
