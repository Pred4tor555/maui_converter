using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace MauiCurrencyConverter
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
    
            BindingContext = new ConverterViewModel();
        }
    }
    
    public class ConverterViewModel : INotifyPropertyChanged
    {
        private string _amountFrom; // Сумма в исходной валюте
        private string _amountTo; // Сумма в целевой валюте
        private Valute _selectedFromCurrency; // Выбранная исходная валюта
        private Valute _selectedToCurrency; // Выбранная целевая валюта
        private bool _isUpdating; // Флаг для предотвращения зацикливания
        private DateTime _selectedDate; // Выбранная дата для загрузки курсов

        public ObservableCollection<Valute> Currencies { get; } = new ObservableCollection<Valute>();

        public ConverterViewModel()
        {
            AmountFrom = string.Empty;
            AmountTo = string.Empty;
            SelectedDate = DateTime.Today;  // Начальная дата - сегодня
        }

        public string AmountFrom
        {
            get => _amountFrom;
            set
            {
                if (_amountFrom != value)
                {
                    _amountFrom = value;
                    OnPropertyChanged();
                    if (!_isUpdating)
                        ConvertCurrency(true); // Конвертация из исходной валюты в целевую
                }
            }
        }

        public string AmountTo
        {
            get => _amountTo;
            set
            {
                if (_amountTo != value)
                {
                    _amountTo = value;
                    OnPropertyChanged();
                    if (!_isUpdating)
                        ConvertCurrency(false); // Конвертация из целевой валюты в исходную
                }
            }
        }

        public Valute SelectedFromCurrency
        {
            get => _selectedFromCurrency;
            set
            {
                if (_selectedFromCurrency != value)
                {
                    _selectedFromCurrency = value;
                    OnPropertyChanged();
                    ConvertCurrency(true); // Обновляем конвертацию при изменении валюты
                }
            }
        }

        public Valute SelectedToCurrency
        {
            get => _selectedToCurrency;
            set
            {
                if (_selectedToCurrency != value)
                {
                    _selectedToCurrency = value;
                    OnPropertyChanged();
                    ConvertCurrency(true); // Обновляем конвертацию при изменении валюты
                }
            }
        }

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (_selectedDate != value)
                {
                    _selectedDate = value;
                    OnPropertyChanged();
                    LoadCurrencyData();  // Загружаем данные для выбранной даты
                }
            }
        }

        private async void LoadCurrencyData()
        {
            try
            {
                // Сохраняем текущие выбранные валюты
                var previousFromCurrency = SelectedFromCurrency;
                var previousToCurrency = SelectedToCurrency;

                // Формируем URL с учетом выбранной даты
                string formattedDate = SelectedDate.ToString("yyyy'/'MM'/'dd");
                string url = $"https://www.cbr-xml-daily.ru/archive/{formattedDate}/daily_json.js";

                Console.WriteLine($"Запрашиваемый URL: {url}");

                using HttpClient client = new HttpClient();
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string jsonResponse = await response.Content.ReadAsStringAsync();

                var currencyData = JsonSerializer.Deserialize<CurrencyResponse>(jsonResponse);

                // Очищаем список валют перед обновлением
                Currencies.Clear();

                foreach (var currency in currencyData.Valute.Values)
                {
                    Currencies.Add(currency);
                }
                Currencies.Add(new Valute { CharCode = "RUB", Value = 1, Nominal = 1 });

                // Восстанавливаем выбранные валюты, если они присутствуют в обновлённом списке
                if (previousFromCurrency != null && previousToCurrency != null)
                {
                    SelectedFromCurrency = Currencies.FirstOrDefault(x => x.CharCode == previousFromCurrency?.CharCode) ?? Currencies.FirstOrDefault();
                    SelectedToCurrency = Currencies.FirstOrDefault(x => x.CharCode == previousToCurrency?.CharCode) ?? Currencies.FirstOrDefault();
                }
                else
                {
                    SelectedFromCurrency = Currencies.FirstOrDefault(x => x.CharCode == "RUB");
                    SelectedToCurrency = Currencies.FirstOrDefault(x => x.CharCode == "USD");
                }
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                SelectedDate = SelectedDate.AddDays(-1);
            }
        }


        private void ConvertCurrency(bool fromSource)
        {
            if (SelectedFromCurrency == null || SelectedToCurrency == null)
                return;

            _isUpdating = true;
            
            try
            {
                decimal fromRate = SelectedFromCurrency.Value;
                decimal toRate = SelectedToCurrency.Value;

                if (fromSource)
                {
                    var tryParse = decimal.TryParse(AmountFrom, out decimal amount);
                    if (tryParse)
                    {
                        AmountTo = ((amount * fromRate) / toRate * SelectedToCurrency.Nominal).ToString("F2");
                    }
                }
                else
                {
                    if (decimal.TryParse(AmountTo, out decimal amount))
                    {
                        AmountFrom = ((amount * toRate) / fromRate * SelectedFromCurrency.Nominal).ToString("F2");
                    }
                }
            }
            finally
            {
                _isUpdating = false;
            }
        }
 
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


    public class Valute
    {
        public string CharCode { get; set; } // Код валюты (например, USD, EUR)
        public decimal Value { get; set; } // Текущая стоимость
        public int Nominal { get; set; } // Предыдущая стоимость
    }

    public class CurrencyResponse
    {
        public Dictionary<string, Valute> Valute { get; set; }
    }
}