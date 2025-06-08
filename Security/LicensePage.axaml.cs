using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System;

namespace GTDCompanion.Pages
{
    public partial class LicensePage : UserControl
    {
        public event Action? LicenseValidated;

        public LicensePage()
        {
            InitializeComponent();
            AppConfig.PopulateEnvironment();
        }

        private async void OnVerifyClick(object? sender, RoutedEventArgs e)
        {
            var licence = LicenseBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(licence))
            {
                StatusText.Text = "Por favor, insira o GTD ID.";
                return;
            }

            StatusText.Text = "Verificando...";
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
                    StatusText.Text = errorObj.TryGetProperty("message", out var msg) ? msg.GetString() : "Erro desconhecido.";
                    StatusText.Foreground = Brushes.OrangeRed;
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Erro: {ex.Message}";
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
    }
}