using System;
using Avalonia.Controls;
using Carrots.ViewModels;

namespace Carrots.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
    
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        
        // Clean up resources when window is closed
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.Cleanup();
        }
    }
}