using System;
using Microsoft.Maui.Controls;
using VinhKhanhstreetfoods.ViewModels;

namespace VinhKhanhstreetfoods.Views.Components;

public partial class NowPlayingBar : ContentView
{
    public NowPlayingBar()
    {
        InitializeComponent();

        if (MauiProgram.ServiceProvider != null)
        {
            var viewModel = MauiProgram.ServiceProvider.GetService<NowPlayingViewModel>();
            if (viewModel != null)
            {
                BindingContext = viewModel;
            }
        }
    }
}