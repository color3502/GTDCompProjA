using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Globalization;

namespace GTDCompanion.Pages
{
    public partial class LicensePage : UserControl
    {
        public event Action? LicenseValidated;

        public LicensePage()
        {
            InitializeComponent();
            AppConfig.PopulateEnvironment();

            LocalizationManager.CultureChanged += ApplyTranslations;
            this.DetachedFromVisualTree += (_, __) => LocalizationManager.CultureChanged -= ApplyTranslations;

            ApplyTranslations();
        }

        private async void OnVerifyClick(object? sender, RoutedEventArgs e)
        {
            var licence = LicenseBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(licence))
            {
                StatusText.Text = LocalizationManager.Get("license_enter_id");
                return;
            }

            StatusText.Text = LocalizationManager.Get("license_checking");
            StatusText.Foreground = Brushes.Yellow;

            var urlApiGtd = Environment.GetEnvironmentVariable("URL_GTD_API") ?? "";

            try
            {
                using var http = new HttpClient();
                var payload = JsonSerializer.Serialize(new { app_licence = licence });
                var response = await http.PostAsync(
                    urlApiGtd + "/gtd/gtdcompanion/check",
                    new StringContent(payload, Encoding.UTF8, "application/json")
                );
                var respContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    GTDConfigHelper.SaveGtdId(licence);
                    LicenseValidated?.Invoke();
                }
                else
                {
                    var errorObj = JsonSerializer.Deserialize<JsonElement>(respContent);
                    StatusText.Text = errorObj.TryGetProperty("message", out var msg) ? msg.GetString() : LocalizationManager.Get("license_unknown_error");
                    StatusText.Foreground = Brushes.OrangeRed;
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"{LocalizationManager.Get("error")}: {ex.Message}";
                StatusText.Foreground = Brushes.OrangeRed;
            }
        }

        public async Task<bool> VerifyLicence(string licence)
        {

            var urlApiGtd = Environment.GetEnvironmentVariable("URL_GTD_API") ?? "";
            try
            {
                using var http = new HttpClient();
                var payload = JsonSerializer.Serialize(new { app_licence = licence });
                var response = await http.PostAsync(
                    urlApiGtd + "/gtd/gtdcompanion/check",
                    new StringContent(payload, Encoding.UTF8, "application/json")
                );
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private void ApplyTranslations()
        {
            LicenseTitle.Text = LocalizationManager.Get("license_title");
            LicenseBox.Watermark = LocalizationManager.Get("license_watermark");
            VerifyButton.Content = LocalizationManager.Get("license_verify_btn");
        }
    }
}