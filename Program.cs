using MateMachine.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MateMachine
{
    public class Program
    {
        static void Main(string[] args)
        {
            //setup DI
            var serviceProvider = new ServiceCollection()
                .AddLogging()
                .AddMemoryCache()
                .AddSingleton<ICurrencyConverter, CurrencyConverter>()
                .BuildServiceProvider();

            var _currencyConverter = serviceProvider.GetRequiredService<ICurrencyConverter>();

            _currencyConverter.UpdateConfiguration(new Tuple<string, string, double>[]
            {
                new Tuple<string, string, double>("USD", "CAD", 1.34),
                new Tuple<string, string, double>("CAD", "GBP", 0.58),
                new Tuple<string, string, double>("USD", "EUR", 0.86)
                });

            Console.WriteLine($"USD-EUR: {_currencyConverter.GetCurrencyRate("USD", "EUR")}"); // 0.86
            Console.WriteLine($"USD-EUR: {_currencyConverter.GetCurrencyRate("CAD", "USD")}"); // 0.7462686567164179
            Console.WriteLine($"USD-EUR: {_currencyConverter.GetCurrencyRate("CAD", "EUR")}"); // 0.6417910447761194
        }
    }
}