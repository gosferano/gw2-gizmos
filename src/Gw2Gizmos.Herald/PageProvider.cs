using System;
using Wpf.Ui.Abstractions;

namespace Gw2Gizmos.Herald;

/// <summary>Resolves NavigationView pages from the DI container so their view-models get injected.</summary>
public sealed class PageProvider : INavigationViewPageProvider
{
    private readonly IServiceProvider _serviceProvider;

    public PageProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public object? GetPage(Type pageType) => _serviceProvider.GetService(pageType);
}
