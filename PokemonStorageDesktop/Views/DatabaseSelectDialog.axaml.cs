using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PokemonStorageDesktop.Views;

public partial class DatabaseSelectDialog : Window
{
    public string ConnectionString { get; set; }

    public DatabaseSelectDialog()
    {
        InitializeComponent();
        CanResize = false;
    }

    private async void Connect_Click(object? sender, RoutedEventArgs e)
    {
        Console.WriteLine($"Open Button");
        ConnectionString = tbConnectionString.Text ?? "";

        if (!string.IsNullOrWhiteSpace(ConnectionString))
        {
            Console.WriteLine($"Got {ConnectionString}");
            this.Close(true);    
        }
    }
}