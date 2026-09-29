using System;
using System.Diagnostics;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using IBEBarcode.Desktop.ViewModels;

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
            Title = "Save Label Sheet PDF",
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
}
