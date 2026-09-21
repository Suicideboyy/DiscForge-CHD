using System;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;

partial class MainForm
{
    async void UpdateGame(Dictionary<string, object> data)
    {
        Func<string, string> get = key => data.ContainsKey(key) && data[key] != null
            ? Convert.ToString(data[key]) : "-";
        gameInfo.Text = get("Title") + "\r\n\r\nSerial: " + get("Serial") + "\r\nMídia: " + get("Type")
            + "\r\nStatus: " + get("Status") + "\r\n\r\nIdentificação: " + get("Detection")
            + "\r\nConsulta: " + get("Lookup") + "\r\nFonte: " + get("DatabaseUrl") + "\r\n\r\nEntrada: "
            + get("Source") + "\r\nImagem: " + get("Image") + "\r\nDetalhe: " + get("Detail");
        string serial = platform.Text == "PS2" ? get("Serial") : "";
        if (serial == coverSerial)
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
            coverStatus.Text = "Capa indisponível: sem serial PS2";
            return;
        }

        coverStatus.Text = "Carregando capa…";
        Image image = await CoverService.Load(serial);
        if (IsDisposed || generation != coverGeneration)
        {
            if (image != null)
            {
                image.Dispose();
            }

            return;
        }

        cover.Image = image;
        coverStatus.Text = image == null ? "Capa indisponível (rede ou base)" : "Capa: " + serial
            + " • xlenore";
    }
}
