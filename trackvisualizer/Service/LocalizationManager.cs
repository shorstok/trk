using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using trackvisualizer.Config;

namespace trackvisualizer.Service
{
    public class LocalizationManager
    {
        private readonly TrekplannerConfiguration _configuration;
        private readonly IUiService _uiService;

        public Tuple<string, string>[] AvailableLocalizations { get; } =
        {
            Tuple.Create(@"en", @"English"),
            Tuple.Create(@"uk", @"Українська"),
        };

        public LocalizationManager(TrekplannerConfiguration configuration, IUiService uiService)
        {
            _configuration = configuration;
            _uiService = uiService;
        }

        public void InitLocalization()
        {
            if (string.IsNullOrWhiteSpace(_configuration.CurrentLanguage))
            {
                _configuration.CurrentLanguage = AvailableLocalizations.First().Item1;
                _configuration.Save();
            }

            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(_configuration.CurrentLanguage);
        }

        public async Task ChooseLocalizationAsync()
        {
            var loc = await _uiService.ChooseAsync(AvailableLocalizations, @"Select language") ??
                      AvailableLocalizations.First().Item1;

            _configuration.CurrentLanguage = loc;
            _configuration.Save();
        }
    }
}