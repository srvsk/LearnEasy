using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LearnEasy.Wpf.Infrastructure;

/// <summary>true → Visible, false → Collapsed. Pass "invert" to flip.</summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c)
    {
        bool b = value is true;
        if (p as string == "invert") b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type t, object? p, CultureInfo c)
        => throw new NotSupportedException();
}

/// <summary>Non-empty string → Visible, otherwise Collapsed.</summary>
public sealed class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c)
        => string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type t, object? p, CultureInfo c)
        => throw new NotSupportedException();
}

/// <summary>Maps an avatar key to a friendly emoji glyph.</summary>
public sealed class AvatarGlyphConverter : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c) => (value as string) switch
    {
        "fox" => "🦊",
        "robot" => "🤖",
        "panda" => "🐼",
        "rocket" => "🚀",
        "owl" => "🦉",
        _ => "🐥",
    };

    public object ConvertBack(object? value, Type t, object? p, CultureInfo c)
        => throw new NotSupportedException();
}
