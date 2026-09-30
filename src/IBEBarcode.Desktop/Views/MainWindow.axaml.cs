using System;
using System.Diagnostics;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using IBEBarcode.Desktop.ViewModels;
using IBEBarcode.Localization;

namespace IBEBarcode.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OnSaveLabelSheetPdfClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        if (!viewModel.TryGenerateLabelSheetPdf(out var pdfBytes, out var error) || pdfBytes is null)
        {
            viewModel.ErrorMessage = error;
            return;
        }

        var pdfFileType = new FilePickerFileType("PDF document")
        {
            Patterns = new[] { "*.pdf" },
            MimeTypes = new[] { "application/pdf" },
        };

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            // The menu caption carries the trailing ellipsis a command that opens a dialog
            // gets by convention; a dialog title does not.
            Title = viewModel.S.SaveLabelSheetPdf.TrimEnd('.', '…'),
            SuggestedFileName = "label-sheet",
            DefaultExtension = "pdf",
            FileTypeChoices = new[] { pdfFileType },
            ShowOverwritePrompt = true,
        });

        if (file is null)
        {
            return;
        }

        await using var stream = await file.OpenWriteAsync();
        await stream.WriteAsync(pdfBytes.AsMemory(0, pdfBytes.Length));
    }

    private void OnHelpClick(object? sender, RoutedEventArgs e)
    {
        var helpFilePath = Path.Combine(AppContext.BaseDirectory, "Assets", "help.html");
        Process.Start(new ProcessStartInfo(helpFilePath) { UseShellExecute = true });
    }

    private void OnLanguageClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem
            && DataContext is MainViewModel viewModel
            && Enum.TryParse<AppLanguage>(menuItem.Tag?.ToString(), out var language))
        {
            viewModel.SelectedLanguage = language;
        }
    }
}
