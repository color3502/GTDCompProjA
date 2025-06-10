using System;
using System.Globalization;
using System.Reflection;
using System.Resources;
using Avalonia;
using Avalonia.Data;
using Avalonia.Markup.Xaml.MarkupExtensions;

namespace GTDCompanion.Helpers
{
    public static class LocalizationManager
    {
        private static CultureInfo _culture = new("pt-BR");
        private static readonly ResourceManager _rm = new("GTDCompanion.Resources", Assembly.GetExecutingAssembly());

        public static event Action? LanguageChanged;

        public static CultureInfo CurrentCulture
        {
            get => _culture;
        }

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

    public class LocalizedBinding : IBinding, IDisposable
    {
        private readonly string _key;
        private readonly string? _default;
        private event Action? _valueChanged;
        public LocalizedBinding(string key, string? def)
        {
            _key = key;
            _default = def;
            LocalizationManager.LanguageChanged += OnChanged;
        }
        public InstancedBinding? Initiate(AvaloniaObject target, AvaloniaProperty? targetProperty, object? anchor = null, bool enableDataValidation = false)
        {
            return InstancedBinding.OneWay(GetValue(), _ => { _valueChanged = _; });
        }
        private object GetValue() => LocalizationManager.Translate(_key, _default);
        private void OnChanged() => _valueChanged?.Invoke();
        public void Dispose() => LocalizationManager.LanguageChanged -= OnChanged;
    }

    public class TrExtension : MarkupExtension
    {
        public string Key { get; set; } = string.Empty;
        public string? Default { get; set; }
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new LocalizedBinding(Key, Default);
        }
    }
}
