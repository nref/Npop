using System;
using AppKit;

namespace Npop.macOS;

sealed class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Main");
        NSApplication.Init();
        NSApplication.SharedApplication.Delegate = new AppDelegate();
        NSApplication.Main(args);
    }
}