using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PokemonStorageDesktop.Views;

public partial class DialogBoxTextField : Window
{
    public DialogBoxTextField()
    {
        InitializeComponent();
        lblContent.Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed ut leo blandit urna ultricies placerat. Integer a dolor vitae elit?";
        btnDone.Content = "Done";
        btnCancel.Content = "Cancel";
    }

    public DialogBoxTextField(string label, string doneText="Done", string cancelText="Cancel")
    {
        InitializeComponent();
        lblContent.Text = label;
        btnDone.Content = doneText;
        btnCancel.Content = cancelText;
    }

    private async void Done_Click(object? sender, RoutedEventArgs e)
    {
        this.Close(tbContent.Text ?? "");
    }

    private async void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        this.Close("");
    }
}