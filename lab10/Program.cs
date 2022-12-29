using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Specialized;
using System.Data;
using System.Xml.Linq;

namespace lab10
{
    public class Ticker
    {
        public int Id { get; set; }
        public string? ticker { get; set; }

        public TodaysCondition? TodaysCondition { get; set; }
    }
    public class Prices
    {
        public int Id { get; set; }
        public double price { get; set; }
        public string date { get; set; }

        public int TickerId { get; set; }
        public Ticker Ticker { get; set; }

    }
    public class TodaysCondition
    {
        public int Id { get; set; }
        public double state { get; set; }

        public int TickerId { get; set; }
        public Ticker Ticker { get; set; }

    }
    public class ApplicationContext : DbContext
    {
        public DbSet<Ticker> Tickers { get; set; } = null!;
        public DbSet<Prices> Prices { get; set; } = null!;
        public DbSet<TodaysCondition> TodaysConditions { get; set; } = null!;

        public ApplicationContext()
        {
            Database.EnsureDeleted();
            Database.EnsureCreated();
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=helloapp.db");
        }
    }

    

    public class Program
    {
        public static ApplicationContext data = new ApplicationContext();
        public static async Task WriteDB(string csv, Ticker ticker)
        {
            
            double[] a = new double[2], b = new double[2];
            string[] lines = csv.Split('\n');

            if (lines.Length < 3)
                return;

            data.Tickers.Add(ticker);

            for (int i = 0; i < 2; i++)
            {
                string[] fields = lines[lines.Length - 1 - i].Split(',');
                if (fields.Length > 4)
                {
                    if (fields[2] != "null" && fields[3] != "null")
                    {
                        a[i] = Convert.ToDouble(fields[2].Replace('.', ','));
                        b[i] = Convert.ToDouble(fields[3].Replace('.', ','));


                        data.Prices.Add(new Prices { TickerId = ticker.Id, date = fields[0], price = (a[i] + b[i]) / 2, Ticker = ticker });
                        data.SaveChanges();
                    }
                }
            }

            data.TodaysConditions.Add(new TodaysCondition { TickerId = ticker.Id, state =( (a[0] + b[0]) / 2 - (a[1] + b[1]) / 2 ), Ticker = ticker });
            data.SaveChanges();
        }

        static async Task Main(string[] args)
        {
            using (ApplicationContext data = new ApplicationContext())
            {
                HttpClient client = new HttpClient();
                string name, response;

                List<Task> results = new List<Task>();
                Ticker ticker;
                
                DateTime time2 = DateTime.Now;
                DateTime time1 = time2.AddDays(-3);

                using (StreamReader reader = new StreamReader("ticker.txt"))
                {
                    int i = 1;
                    while (!reader.EndOfStream)
                    {
                        try
                        {
                            //Thread.Sleep(1000);
                            name = reader.ReadLine();
                            ticker = new Ticker { Id = i++, ticker = name };
                            response = await client.GetStringAsync($"https://query1.finance.yahoo.com/v7/finance/download/{name}?period1={((DateTimeOffset)time1).ToUnixTimeSeconds()}&period2={((DateTimeOffset)time2).ToUnixTimeSeconds()}&interval=1d&events=history&includeAdjustedClose=true");
                            if (response.Length > 0)
                                results.Add(WriteDB(response, ticker));
                        }
                        catch (Exception e)
                        {
                            //Console.WriteLine(e); 
                        }
                    }

                    Task.WaitAll(results.ToArray());
                    
                }

                Console.WriteLine("Enter ticker");
                string? rticker = Console.ReadLine();
               
                
                if (rticker == null)
                    Console.WriteLine("error");

                List<TodaysCondition> tickers = data.TodaysConditions.Include(x => x.Ticker).ToList();
                foreach(var i in tickers)
                {
                    if (i.Ticker.ticker == rticker)
                        if (i.state >= 0)
                            Console.WriteLine("up");
                        else
                            Console.WriteLine("down");
                }

            }

        }
    }
}