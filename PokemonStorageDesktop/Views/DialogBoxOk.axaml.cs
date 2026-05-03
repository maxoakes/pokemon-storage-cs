using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PokemonStorageDesktop.Views;

public partial class DialogBoxOk : Window
{
    public DialogBoxOk()
    {
        InitializeComponent();
        lblContent.Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed ut leo blandit urna ultricies placerat. Integer a dolor vitae elit?";
        btnOk.Content = "Ok";
    }

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