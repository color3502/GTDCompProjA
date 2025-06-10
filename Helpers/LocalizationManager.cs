using Avalonia.Data;
using Avalonia.Markup.Xaml;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace GTDCompanion.Helpers
{
    public static class LocalizationManager
    {
        private static CultureInfo _culture = new("pt-BR");
        private static readonly ResourceManager _rm = new("GTDCompanion.Resources", Assembly.GetExecutingAssembly());

        public static event Action? LanguageChanged;

        public static CultureInfo CurrentCulture => _culture;

        public static void SetCulture(string name)
        {
            try
            {
                _culture = new CultureInfo(name);
            }
            catch
            {
                _culture = new CultureInfo("pt-BR");
            }
            LanguageChanged?.Invoke();
        }

        public static string Translate(string key, string? defaultValue = null)
        {
            var value = _rm.GetString(key, _culture);
            return string.IsNullOrEmpty(value) ? defaultValue ?? key : value;
        }
    }

    // REMOVE the old LocalizedBinding class

    // REPLACE the old TrExtension with this implementation
    public class TrExtension : MarkupExtension, INotifyPropertyChanged
    {
        public string Key { get; set; } = string.Empty;
        public string? Default { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// The property that the UI will bind to. It always returns the current translation.
        /// </summary>
        public string Value => LocalizationManager.Translate(Key, Default);

        public TrExtension(string key)
        {
            Key = key;
        }

        public TrExtension() { }

        /// <summary>
        /// Returns a binding to this extension's 'Value' property.
        /// </summary>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            // Subscribe to language changes to notify the UI.
            LocalizationManager.LanguageChanged += OnLanguageChanged;

            // The binding system holds a reference to this extension instance, keeping it alive.
            return new Binding(nameof(Value)) { Source = this, Mode = BindingMode.OneWay };
        }

        /// <summary>
        /// When the language changes, this method fires the PropertyChanged event,
        /// telling the binding to update its value.
        /// </summary>
        private void OnLanguageChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
        }
    }
}