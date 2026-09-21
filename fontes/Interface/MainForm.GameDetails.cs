using System;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading.Tasks;

partial class MainForm
{
    bool coverLoading;
    Func<string, Task<Image>> coverLoader = CoverService.Load;
    DateTime lastCoverAttempt = DateTime.MinValue;

    async void UpdateGame(Dictionary<string, object> data)
    {
        Func<string, string> get = key => data.ContainsKey(key) && data[key] != null
            ? Convert.ToString(data[key]) : "-";
        gameInfo.Text = get("Title") + "\r\n\r\nSerial: " + get("Serial") + "\r\nMídia: " + get("Type")
            + "\r\nStatus: " + get("Status") + "\r\n\r\nIdentificação: " + get("Detection")
            + "\r\nConsulta: " + get("Lookup") + "\r\nFonte: " + get("DatabaseUrl") + "\r\n\r\nEntrada: "
            + get("Source") + "\r\nImagem: " + get("Image") + "\r\nDetalhe: " + get("Detail");
        string serial = MediaFiles.NormalizeSerial(get("Serial"));
        if (serial.Length == 0)
        {
            serial = MediaFiles.NormalizeSerial(get("Image") + " " + get("Source"));
        }
        await RefreshCover(platform.Text == "PS2" ? serial : "");
    }

    async Task RefreshCover(string serial)
    {
        if (coverLoading && serial == coverSerial)
        {
            return;
        }

        coverSerial = serial;
        int generation = ++coverGeneration;
        var previous = cover.Image;
        cover.Image = null;
        if (previous != null)
        {
            previous.Dispose();
        }

        if (!Regex.IsMatch(serial, @"^[A-Z]{4}-\d{5}$"))
        {
            coverLoading = false;
            coverStatus.Text = "Capa indisponível: sem serial PS2";
            return;
        }

        coverStatus.Text = "Carregando capa…";
        coverLoading = true;
        lastCoverAttempt = DateTime.UtcNow;
        Image image = await coverLoader(serial);
        if (IsDisposed || generation != coverGeneration)
        {
            if (image != null)
            {
                image.Dispose();
            }

            return;
        }

        coverLoading = false;
        cover.Image = image;
        coverStatus.Text = image == null ? "Capa indisponível (rede ou base)" : "Capa: " + serial
            + " • xlenore";
    }

    void BeginEntry()
    {
        taskClock.Restart();
        UpdateElapsed();
        ++coverGeneration;
        coverLoading = false;
        coverSerial = "";
        var previous = cover.Image;
        cover.Image = null;
        if (previous != null)
        {
            previous.Dispose();
        }
        coverStatus.Text = "Aguardando serial…";
        gameInfo.Text = "Identificando a entrada atual…";
    }
}
