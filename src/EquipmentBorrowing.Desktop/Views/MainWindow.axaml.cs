using Avalonia.Controls;

namespace EquipmentBorrowing.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void EquipmentButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        MainContent.Content = new EquipmentView();
    }

    private void ActiveBorrowingsButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        MainContent.Content = new BorrowingsView();
    }
}