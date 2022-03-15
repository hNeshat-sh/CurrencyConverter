using Microsoft.Extensions.Caching.Memory;
using System.Linq;

namespace MateMachine.Services
{

    public class CurrencyConverter : ICurrencyConverter
    {
        private readonly IMemoryCache _cache;

        private List<Tuple<string, string, double>> _conversionRates { get; set; }

        public CurrencyConverter(IMemoryCache cache)
        {
            _cache = cache;
        }

        public void ClearConfiguration()
        {
            ((MemoryCache)_cache).Compact(1);
        }

        public double Convert(string fromCurrency, string toCurrency, double amount)
        {
            return amount * GetCurrencyRate(fromCurrency, toCurrency);
        }

        public void UpdateConfiguration(IEnumerable<Tuple<string, string, double>> conversionRates)
        {
            _conversionRates = conversionRates.ToList();
            AddReverseCurrencyRates(); 
        }

        void AddReverseCurrencyRates()
        {
            var r = new List<Tuple<string, string, double>>();
            foreach (var conversionRate in _conversionRates)
            {
                if (!_conversionRates.Any(a => a.Item1 == conversionRate.Item2 && a.Item2 == conversionRate.Item1))
                    r.Add(new Tuple<string, string, double>(conversionRate.Item2, conversionRate.Item1, 1 / conversionRate.Item3));
            }
            _conversionRates = _conversionRates.Concat(r).ToList();
        }

        public List<Tuple<string, string, double>> GetCurrencyConversion(string fromCurrency, string toCurrency)
        {
            List<Tuple<string, string, double>> result = new List<Tuple<string, string, double>>();

            #region direct conversion
            var directConversion = _conversionRates.SingleOrDefault(a => a.Item1 == fromCurrency && a.Item2 == toCurrency);
            if (directConversion != null)
            {
                result.Add(directConversion);
                return result;
            }
            #endregion

            #region shortest path
            var allPaths = GetAllPaths(fromCurrency, toCurrency);
            if (!allPaths.Any())
                return result;
            var min = allPaths.Min(a => a.Length);
            var shortestPath = allPaths.First(a => a.Length == min);
            foreach (var path in shortestPath.Split(','))
            {
                result.Add(_conversionRates.Single(a => a.Item1 == path.Split('-')[0] && a.Item2 == path.Split('-')[1]));
            }
            return result;
            #endregion
        }

        private IEnumerable<string> GetAllPaths(string fromCurrency, string toCurrency)
        {
            var result = new List<string>();
            foreach (var parent in _conversionRates.Where(a => a.Item1 == fromCurrency))
            {
                Traverse(parent, ref result, toCurrency);
            }
            return result.Where(a => a.EndsWith($"-{toCurrency}"));
        }

        public double GetCurrencyRate(string fromCurrency, string toCurrency)
        {
            // read from cache
            if (_cache.TryGetValue<double>($"{fromCurrency}-{toCurrency}", out double cacheValue))
                return cacheValue;

            double result = 1;
            var conversions = GetCurrencyConversion(fromCurrency, toCurrency);
            if (!conversions.Any())
                throw new Exception($"Conversion rate {fromCurrency}-{toCurrency} not found !");
            foreach (var conversion in conversions)
            {
                result *= conversion.Item3;
            }

            // set cahe value
            _cache.Set<double>($"{fromCurrency}-{toCurrency}", result);

            return result;
        }

        public IEnumerable<T> Traverse<T>(T item, Func<T, IEnumerable<T>> childSelector)
        {
            var stack = new Stack<T>();
            stack.Push(item);
            while (stack.Any())
            {
                var next = stack.Pop();
                yield return next;
                foreach (var child in childSelector(next))
                    stack.Push(child);
            }
        }

        void Traverse(Tuple<string, string, double> parent, ref List<string> list, string dest, string p = "")
        {
            if (string.IsNullOrEmpty(p))
                p = $"{parent.Item1}-{parent.Item2}";
            foreach (var child in _conversionRates.Where(a => a.Item1 == parent.Item2))
            {
                if (p.Contains($"{child.Item1}-{child.Item2}"))
                    return;
                list.Add($"{p},{child.Item1}-{child.Item2}");
                if (child.Item2 != dest)
                    Traverse(child, ref list, dest, $"{p},{child.Item1}-{child.Item2}");
            }
        }
    }
}