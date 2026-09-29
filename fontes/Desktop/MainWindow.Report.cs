using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;

sealed partial class MainWindow
{
    // Review comes first; Simple MAPI then opens a draft with the report attached.
    async Task ReportIssueAsync()
    {
        try
        {
            string path = IssueReport.Create(input.Text);
            string details = Localization.T("Report to") + ": " + AppInfo.BugEmail
                + "\n" + Localization.T("Attachment") + ": " + path
                + "\n" + Localization.T("Size") + ": "
                + (new FileInfo(path).Length / 1024.0).ToString("N0") + " KB"
                + "\n\n" + Localization.T("Review the logs before sending.");
            var dialog = new ContentDialog
            {
                XamlRoot = tabs.XamlRoot,
                Title = Localization.T("Report issue"),
                Content = details,
                PrimaryButtonText = Localization.T("Open email with attachment"),
                SecondaryButtonText = Localization.T("Open report folder"),
                CloseButtonText = Localization.T("Cancel"),
                DefaultButton = ContentDialogButton.Primary
            };
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Secondary)
            {
                Process.Start(new ProcessStartInfo("explorer.exe", "/select,\"" + path + "\"")
                    { UseShellExecute = true });
            }
            else if (result == ContentDialogResult.Primary)
            {
                uint status = await OpenEmailDraftAsync(path);
                if (status == 0 || status == 1) return; // Sent draft or user canceled it.
                Process.Start(new ProcessStartInfo("explorer.exe", "/select,\"" + path + "\"")
                    { UseShellExecute = true });
                await new ContentDialog
                {
                    XamlRoot = tabs.XamlRoot,
                    Title = Localization.T("Email client unavailable"),
                    Content = Localization.T("The report was saved. Attach it manually to an email to")
                        + " " + AppInfo.BugEmail + ".\n\n" + path,
                    CloseButtonText = "OK"
                }.ShowAsync();
            }
        }
        catch (Exception ex)
        {
            CrashReporter.Record(ex, "ReportIssueAsync");
            await new ContentDialog
            {
                XamlRoot = tabs.XamlRoot,
                Title = Localization.T("Report issue"),
                Content = ex.Message,
                CloseButtonText = "OK"
            }.ShowAsync();
        }
    }

    static Task<uint> OpenEmailDraftAsync(string path)
    {
        var completion = new TaskCompletionSource<uint>(TaskCreationOptions.RunContinuationsAsynchronously);
        var worker = new Thread(() =>
        {
            try { completion.SetResult(EmailDraft.Open(AppInfo.BugEmail, path)); }
            catch (Exception ex) { completion.SetException(ex); }
        });
        worker.SetApartmentState(ApartmentState.STA);
        worker.IsBackground = true;
        worker.Start();
        return completion.Task;
    }
}
