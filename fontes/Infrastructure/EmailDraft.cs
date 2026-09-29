using System;
using System.Runtime.InteropServices;

// Simple MAPI opens the default mail client's compose window; it never sends mail.
static class EmailDraft
{
    const uint MapiTo = 1;
    const uint MapiDialog = 0x8;
    const uint MapiLogonUi = 0x1;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct Recipient
    {
        public uint Reserved;
        public uint Class;
        [MarshalAs(UnmanagedType.LPWStr)] public string Name;
        [MarshalAs(UnmanagedType.LPWStr)] public string Address;
        public uint EntryIdSize;
        public IntPtr EntryId;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct Attachment
    {
        public uint Reserved;
        public uint Flags;
        public uint Position;
        [MarshalAs(UnmanagedType.LPWStr)] public string Path;
        [MarshalAs(UnmanagedType.LPWStr)] public string Name;
        public IntPtr Type;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct Message
    {
        public uint Reserved;
        [MarshalAs(UnmanagedType.LPWStr)] public string Subject;
        [MarshalAs(UnmanagedType.LPWStr)] public string Note;
        [MarshalAs(UnmanagedType.LPWStr)] public string Type;
        [MarshalAs(UnmanagedType.LPWStr)] public string Date;
        [MarshalAs(UnmanagedType.LPWStr)] public string Conversation;
        public uint Flags;
        public IntPtr Originator;
        public uint RecipientCount;
        public IntPtr Recipients;
        public uint AttachmentCount;
        public IntPtr Attachments;
    }

    [DllImport("MAPI32.dll", EntryPoint = "MAPISendMailW", CharSet = CharSet.Unicode)]
    static extern uint SendMail(IntPtr session, IntPtr parent, ref Message message,
        uint flags, uint reserved);

    public static uint Open(string recipient, string reportPath)
    {
        var to = new Recipient { Class = MapiTo, Name = recipient,
            Address = "SMTP:" + recipient };
        var file = new Attachment { Position = uint.MaxValue, Path = reportPath,
            Name = System.IO.Path.GetFileName(reportPath) };
        IntPtr toPointer = Marshal.AllocHGlobal(Marshal.SizeOf<Recipient>());
        IntPtr filePointer = Marshal.AllocHGlobal(Marshal.SizeOf<Attachment>());
        try
        {
            Marshal.StructureToPtr(to, toPointer, false);
            Marshal.StructureToPtr(file, filePointer, false);
            var message = new Message
            {
                Subject = AppInfo.DisplayName + " bug report",
                Note = "Please describe the problem and review the attached logs before sending.",
                RecipientCount = 1, Recipients = toPointer,
                AttachmentCount = 1, Attachments = filePointer
            };
            try
            {
                return SendMail(IntPtr.Zero, IntPtr.Zero, ref message,
                    MapiDialog | MapiLogonUi, 0);
            }
            catch (DllNotFoundException) { return uint.MaxValue; }
            catch (EntryPointNotFoundException) { return uint.MaxValue; }
        }
        finally
        {
            Marshal.DestroyStructure<Recipient>(toPointer);
            Marshal.DestroyStructure<Attachment>(filePointer);
            Marshal.FreeHGlobal(toPointer);
            Marshal.FreeHGlobal(filePointer);
        }
    }
}
