using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PokemonStorageDesktop.Views;

public partial class DialogBoxYesNo : Window
{
    public DialogBoxYesNo()
    {
        InitializeComponent();
        lblContent.Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed ut leo blandit urna ultricies placerat. Integer a dolor vitae elit?";
        btnYes.Content = "Yes";
        btnNo.Content = "No";
        btnCancel.Content = "Cancel";
    }

    public DialogBoxYesNo(string label, string title="Message", string yesText="Yes", string noText="No", string cancelText="Cancel")
    {
        InitializeComponent();
        Title = title;
        lblContent.Text = label;
        btnYes.Content = yesText;
        btnNo.Content = noText;
        if (string.IsNullOrWhiteSpace(cancelText))
        {
            buttonContainer.Children.Remove(btnCancel);
        }
        else
        {
            btnCancel.Content = cancelText;
        }
        
    }

    private async void Yes_Click(object? sender, RoutedEventArgs e)
    {
        this.Close(1);
    }
    private async void No_Click(object? sender, RoutedEventArgs e)
    {
        this.Close(0);
    }
    private async void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        this.Close(null);
    }
}