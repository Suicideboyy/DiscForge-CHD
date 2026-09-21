using System;
using System.IO;
using System.Text;

sealed partial class ConversionSession
{
    void Say(string text)
    {
        lock (_logLock)
        {
            _report(text);
            if (!text.StartsWith("GUI_") && !text.StartsWith("Etapa:"))
            {
                File.AppendAllText(Path.Combine(_temporaryDirectory, "log.txt"), text + Environment.NewLine,
                    Encoding.UTF8);
            }
        }
    }

    void Show(GameInfo game)
    {
        Say("GUI_GAME:" + System.Convert.ToBase64String(Encoding.UTF8.GetBytes(_serializer.Serialize(game))));
    }
}
