using System.Runtime.InteropServices;

namespace Npop.Windows;

public static class Program
{
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern bool Shell_NotifyIcon(uint dwMessage, [In] ref NOTIFYICONDATA pnid);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct NOTIFYICONDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public int uID;
        public uint uFlags;
        public int uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public int dwState;
        public int dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public uint uVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    const uint NIM_ADD = 0x00000000;
    const uint NIM_MODIFY = 0x00000001;
    const uint NIM_DELETE = 0x00000002;
    const uint NIF_MESSAGE = 0x00000001;
    const uint NIF_ICON = 0x00000002;
    const uint NIF_TIP = 0x00000004;
    const uint NIF_STATE = 0x00000008;
    const uint NIF_INFO = 0x00000010;
    const uint NIIF_INFO = 0x00000001;
    const uint NOTIFYICON_VERSION_4 = 4;

    static void Main()
    {
        // Create a dummy window handle
        IntPtr hWnd = CreateDummyWindow();

        var nid = new NOTIFYICONDATA
        {
            cbSize = Marshal.SizeOf(typeof(NOTIFYICONDATA)),
            hWnd = hWnd,
            uID = 1,
            uFlags = NIF_ICON | NIF_TIP | NIF_INFO | NIF_MESSAGE,
            uCallbackMessage = 0x500, // WM_USER + 1
            hIcon = ExtractIcon(IntPtr.Zero, @"C:\Windows\System32\shell32.dll", 0),
            szTip = "My Application",
            szInfo = "This is a notification message",
            szInfoTitle = "Notification Title",
            dwInfoFlags = NIIF_INFO,
            uVersion = NOTIFYICON_VERSION_4
        };

        // Add the icon
        Shell_NotifyIcon(NIM_ADD, ref nid);

        nid.szInfo = "This is an updated notification message";

        // Update the icon to show the balloon
        Shell_NotifyIcon(NIM_MODIFY, ref nid);

        Console.WriteLine("Notification sent. Press any key to exit...");
        Console.ReadKey();

        // Remove the icon
        Shell_NotifyIcon(NIM_DELETE, ref nid);

        // Destroy the dummy window
        DestroyWindow(hWnd);
    }

    [DllImport("user32.dll")]
    static extern IntPtr CreateWindowEx(uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll")]
    static extern bool DestroyWindow(IntPtr hWnd);

    static IntPtr CreateDummyWindow()
    {
        return CreateWindowEx(0, "STATIC", "DummyWindow", 0, 0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
    }}
