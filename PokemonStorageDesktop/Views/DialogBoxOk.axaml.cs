using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PokemonStorageDesktop.Views;

public partial class DialogBoxOk : Window
{
    public DialogBoxOk(string label, string buttonText="Ok")
    {
        InitializeComponent();
        lblContent.Text = label;
        btnOk.Content = buttonText;
    }

    private async void Ok_Click(object? sender, RoutedEventArgs e)
    {
        this.Close(true);
    }
}