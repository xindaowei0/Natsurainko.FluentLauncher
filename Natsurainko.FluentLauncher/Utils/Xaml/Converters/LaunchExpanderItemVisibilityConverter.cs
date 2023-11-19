﻿using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Natsurainko.FluentLauncher.Services.Launch;
using Nrk.FluentCore.Launch;
using System;
using System.Linq;

namespace Natsurainko.FluentLauncher.Utils.Xaml.Converters;

internal class LaunchExpanderItemVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        string express = parameter.ToString();
        var states = express.Split(',').Select(x => int.Parse(x)).ToArray();

        if (value is MinecraftSessionState state && states.Contains((int)state))
            return Visibility.Visible;

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
