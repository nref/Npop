using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Npop.macOS.Raw;

/// <summary>
/// An attempt at macOS desktop notifications without creating a Bundle and without using the net8.0-macos TFM.
/// The conventional wisdom is to "swizzle" the bundle identifier, but I did not succeed.
/// I discovered late into this that UNUserNotificationCenter requires the app to be signed.
/// At that point I switched to using .net8.0-macos.
/// </summary>
partial class Program
{
    // Define the delegate type
    delegate IntPtr TwoArgWithRetDelegate(IntPtr self, IntPtr cmd);
    delegate void DidFinishLaunchingHandler(IntPtr self, IntPtr cmd, IntPtr notification);
    delegate void RequestAuthorizationCompletionHandler(IntPtr self, IntPtr cmd, bool granted, IntPtr error);
      
    static IntPtr BundleIdentifierPtr = IntPtr.Zero;
    static IntPtr BundleIdentifierPtr2 = IntPtr.Zero;
    static IntPtr BundleUrlPtr = IntPtr.Zero;
    static IntPtr DidFinishLaunchingPtr = IntPtr.Zero;
    static IntPtr HandleAuthorizationCompletedPtr = IntPtr.Zero;
    
    // Define the method implementation
    static IntPtr GetBundleIdentifier(IntPtr self, IntPtr cmd) => CFStringCreateWithCString(IntPtr.Zero, "com.apple.finder", kCFStringEncodingUTF8);
    static IntPtr GetBundleUrl(IntPtr self, IntPtr cmd) => CFStringCreateWithCString(IntPtr.Zero, "/fake/dir", kCFStringEncodingUTF8);

    public static async Task Main(string[] args)
    {
        // Might be a prereq for Appkit in some cases
        // https://stackoverflow.com/questions/3414523/debugging-a-crash-when-a-library-is-opened-via-dlopen-on-osx
        //IntPtr coreFoundation = dlopen(CoreFoundation, RTLD_NOW);
        
        // Needed for DidFinishLoading to be called
        // . Crashes if we have swizzled the bundle identifier, but works if we subclass NSApplication and swizzle bundleIdentifier of NSBundle.
        IntPtr appKitHandle = dlopen(AppKit, RTLD_NOW);
        
        IntPtr userNotificationsHandle = dlopen(UserNotifications, RTLD_NOW);
        IntPtr foundationHandle = dlopen(Foundation, RTLD_NOW);
        
        InitFunctionPointers();

        //Swizzle();
        LoadSharedApp();
        //LoadCustomApp();
       
        //ShowOsaNotification("test", "message");
        //ShowCFUserNotification();
        //ShowNSUserNotification("test", "message");

        // WithAutoReleasePool(() =>
        // {
        //     ShowUNUserNotification();
        // });
        
        await Task.Delay(TimeSpan.FromSeconds(5));
    }

    private static void InitFunctionPointers()
    {
        DidFinishLaunchingHandler del = DidFinishLaunching;
        TwoArgWithRetDelegate del1 = GetBundleIdentifier;
        TwoArgWithRetDelegate del2 = __bundleIdentifier;
        TwoArgWithRetDelegate del3 = GetBundleUrl;
        RequestAuthorizationCompletionHandler del4 = HandleAuthorizationCompleted;
        
        DidFinishLaunchingPtr = Marshal.GetFunctionPointerForDelegate(del);
        BundleIdentifierPtr = Marshal.GetFunctionPointerForDelegate(del1);
        BundleIdentifierPtr2 = Marshal.GetFunctionPointerForDelegate(del2);
        BundleUrlPtr = Marshal.GetFunctionPointerForDelegate(del3);
        HandleAuthorizationCompletedPtr = Marshal.GetFunctionPointerForDelegate(del4);
    }

    static void WithAutoReleasePool(Action a)
    {
        var pool = IntPtr.Zero;
        
        try
        {
            pool = objc_getClass("NSAutoreleasePool");
            pool = objc_msgSend(pool, sel_registerName("alloc"), sel_registerName("init"));
            a();
        }
        finally
        {
            objc_msgSend(pool, sel_registerName("release"));
        }
    }

    static void LoadCustomApp()
    {
        //InstallNSBundleHook();
        
        // Initialize NSApplication
        IntPtr customNSApplicationClass = objc_allocateClassPair(objc_getClass("NSApplication"), "CustomNSApplication", 0);
        objc_registerClassPair(customNSApplicationClass);
        
        IntPtr nsApplicationClass = objc_getClass("CustomNSApplication");
        IntPtr sharedApplicationSelector = sel_registerName("sharedApplication");
        IntPtr nsApp = objc_msgSend(nsApplicationClass, sharedApplicationSelector);
        
        // Set Application Delegate
        IntPtr appDelegate = CreateAppDelegate();
        IntPtr setDelegateSelector = sel_registerName("setDelegate:");
        objc_msgSend(nsApp, setDelegateSelector, appDelegate);
        
        // Run the Application
        IntPtr runSelector = sel_registerName("run");
        objc_msgSend(nsApp, runSelector);
    }
  
    /// <summary>
    /// Swizzle the bundleIdentifier method of NSBundle
    /// </summary>
    static void InstallNSBundleHook()
    {
        IntPtr nsBundleClass = objc_getClass("NSBundle");
        IntPtr originalSelector = sel_registerName("bundleIdentifier");
        IntPtr swizzledSelector = sel_registerName("__bundleIdentifier");

        IntPtr originalMethod = class_getInstanceMethod(nsBundleClass, originalSelector);
        IntPtr swizzledMethod = class_getInstanceMethod(nsBundleClass, swizzledSelector);
        
        class_addMethod(nsBundleClass, swizzledSelector, BundleIdentifierPtr2, "@@:");
        
        method_exchangeImplementations(originalMethod, swizzledMethod);
    }
    
    static IntPtr __bundleIdentifier(IntPtr self, IntPtr cmd)
    {
        IntPtr mainBundle = objc_msgSend(objc_getClass("NSBundle"), sel_registerName("mainBundle"));
        
        if (self == mainBundle)
        {
            const string fakeBundleIdentifier = "com.apple.finder";
            return NSStringFromString(fakeBundleIdentifier);
        }
        
        IntPtr originalSelector = sel_registerName("__bundleIdentifier");
        return objc_msgSend(self, originalSelector);
    }
    
    static void LoadSharedApp()
    {
        IntPtr nsApplicationClass = objc_getClass("NSApplication");
        IntPtr sharedApplicationSelector = sel_registerName("sharedApplication");
        IntPtr nsApp = objc_msgSend(nsApplicationClass, sharedApplicationSelector);

        IntPtr appDelegate = CreateAppDelegate();
        IntPtr setDelegateSelector = sel_registerName("setDelegate:");
        objc_msgSend(nsApp, setDelegateSelector, appDelegate);

        // Run the Application
        IntPtr runSelector = sel_registerName("run");
        objc_msgSend(nsApp, runSelector);
    }

    private static IntPtr CreateAppDelegate()
    {
        // https://developer.apple.com/documentation/appkit/nsapplication
        
        IntPtr nsObjectClass = objc_getClass("NSObject");
        IntPtr appDelegateClass = objc_allocateClassPair(nsObjectClass, "AppDelegate", 0);

        IntPtr didFinishLaunchingSelector = sel_registerName("applicationDidFinishLaunching:");
        class_addMethod(appDelegateClass, didFinishLaunchingSelector, DidFinishLaunchingPtr, "v@:@");

        objc_registerClassPair(appDelegateClass);

        IntPtr appDelegateInstance = objc_msgSend(appDelegateClass, sel_registerName("new"));
        return appDelegateInstance;
    }

    static void DidFinishLaunching(IntPtr self, IntPtr cmd, IntPtr notification)
    {
        Console.WriteLine(nameof(DidFinishLaunching));
        
        ShowUNUserNotification();
    }
    static void ShowOsaNotification(string title, string message)
    {
        string script = $"""osascript -e 'display notification \"{message}\" with title \"{title}\"'""";

        var processStartInfo = new ProcessStartInfo("bash", $"-c \"{script}\"")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using Process process = new();
        process.StartInfo = processStartInfo;
        process.Start();
    }
    
    static void ShowCFUserNotification()
    {
        // Display a notification using CFUserNotificationDisplayNotice
        int result = CFUserNotificationDisplayNotice(
            3, // 3s timeout
            3 | kCFUserNotificationNoDefaultButtonFlag, // Alert level (3 = Note)
            IntPtr.Zero, // Icon URL (none)
            IntPtr.Zero, // Sound URL (none)
            IntPtr.Zero, // Localization URL (none)
            NSStringFromString("Hello!"), // Title
            NSStringFromString("This is a test notification."), // Message
            IntPtr.Zero //NSStringFromString("OK") // Default button title
        ); 
    }
    
    static void ShowNSUserNotification(string title, string message)
    {
        IntPtr nsUserNotificationCenterClass = objc_getClass("NSUserNotificationCenter");
        IntPtr defaultUserNotificationCenterSelector = sel_registerName("defaultUserNotificationCenter");
        IntPtr notifCenter = objc_msgSend(nsUserNotificationCenterClass, defaultUserNotificationCenterSelector);

        IntPtr notif = objc_msgSend(objc_getClass("NSUserNotification"), sel_registerName("alloc"));
        notif = objc_msgSend(notif, sel_registerName("init"));

        IntPtr titleString = NSStringFromString(title);
        IntPtr messageString = NSStringFromString(message);

        objc_msgSend(notif, sel_registerName("setTitle:"), titleString);
        objc_msgSend(notif, sel_registerName("setInformativeText:"), messageString);

        objc_msgSend(notifCenter, sel_registerName("deliverNotification:"), notif);
    }

    static void ShowUNUserNotification()
    {
        IntPtr center = GetNotificationCenter();
        ulong options = 0x4 | 0x1 | 0x2; // UNAuthorizationOptionAlert | UNAuthorizationOptionBadge | UNAuthorizationOptionSound

        // Request permission
        IntPtr requestAuthorizationWithCompletionHandlerSelector = sel_registerName("requestAuthorizationWithOptions:completionHandler:");
        objc_msgSend(center, requestAuthorizationWithCompletionHandlerSelector, options, HandleAuthorizationCompletedPtr);
        //objc_msgSend(center, requestAuthorizationWithCompletionHandlerSelector, options, IntPtr.Zero);
        RequestNotification();
    }

    private static IntPtr GetNotificationCenter()
    {
        // Schedule the notification
        IntPtr centerClass = objc_getClass("UNUserNotificationCenter");

        // This crashes if there is no Info.plist next to the exe.
        return objc_msgSend(centerClass, sel_registerName("currentNotificationCenter"));
    }

    private static void HandleAuthorizationCompleted(IntPtr self, IntPtr cmd, bool granted, IntPtr error)
    {
        Console.WriteLine($"Authorization granted: {granted}");
        if (error != IntPtr.Zero)
        {
            Console.WriteLine("Error occurred during authorization request.");
            return;
        }
        
        RequestNotification();
    }

    private static void RequestNotification()
    {
        // Initialize UNMutableNotificationContent
        IntPtr contentClass = objc_getClass("UNMutableNotificationContent");
        IntPtr content = objc_msgSend(contentClass, sel_registerName("alloc"));
        content = objc_msgSend(content, sel_registerName("init"));

        // Set the title and body
        IntPtr title = NSStringFromString("Hello!");
        IntPtr body = NSStringFromString("Hello_message_body");

        objc_msgSend(content, sel_registerName("setTitle:"), title);
        objc_msgSend(content, sel_registerName("setBody:"), body);

        // Set the sound
        IntPtr defaultSound = objc_msgSend(objc_getClass("UNNotificationSound"), sel_registerName("defaultSound"));
        objc_msgSend(content, sel_registerName("setSound:"), defaultSound);

        // Create the trigger
        IntPtr trigger = objc_msgSend(objc_getClass("UNTimeIntervalNotificationTrigger"),
            sel_registerName("triggerWithTimeInterval:repeats:"), 5, false);

        // Create the request
        IntPtr identifier = NSStringFromString("FiveSecond");
        IntPtr request = objc_msgSend(objc_getClass("UNNotificationRequest"),
            sel_registerName("requestWithIdentifier:content:trigger:"), identifier, content, trigger);

        IntPtr center = GetNotificationCenter();
        objc_msgSend(center, sel_registerName("addNotificationRequest:withCompletionHandler:"), request, nint.Zero);
    }

    // P/Invoke signatures
    [LibraryImport(LibObjC, StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint objc_getClass(string className);

    [LibraryImport(LibObjC, StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint sel_registerName(string name);

    [LibraryImport(LibObjC)]
    private static partial nint objc_msgSend(nint receiver, nint selector);

    [LibraryImport(LibObjC)]
    private static partial nint objc_msgSend(nint receiver, nint selector, [MarshalAs(UnmanagedType.R8)] double arg1);
    
    [LibraryImport(LibObjC)]
    private static partial nint objc_msgSend(nint receiver, nint selector, nint arg1);

    [LibraryImport(LibObjC)]
    private static partial nint objc_msgSend(nint receiver, nint selector, nint arg1, nint arg2);
    
    [LibraryImport(LibObjC)]
    private static partial void objc_msgSend(nint receiver, nint selector, ulong arg1, nint arg2);

    [LibraryImport(LibObjC)]
    private static partial nint objc_msgSend(nint receiver, nint selector, nint arg1, nint arg2, nint arg3);
   
    [LibraryImport(LibObjC)]
    private static partial nint objc_msgSend(nint receiver, nint selector, nint arg1, ulong arg2, int arg3, [MarshalAs(UnmanagedType.Bool)] bool arg4);
    
    [LibraryImport(LibObjC)]
    private static partial IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, 
        [MarshalAs(UnmanagedType.R8)] double arg1, 
        [MarshalAs(UnmanagedType.Bool)] bool arg2);
    
    [LibraryImport(CoreFoundation)]
    private static partial int CFUserNotificationDisplayNotice(
        [MarshalAs(UnmanagedType.R8)] double timeout,
        uint flags,
        IntPtr iconURL,
        IntPtr soundURL,
        IntPtr localizationURL,
        IntPtr alertHeader,
        IntPtr alertMessage,
        IntPtr defaultButtonTitle
    );
    
    [LibraryImport(CoreFoundation, StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr CFStringCreateWithCString(IntPtr alloc, string str, uint encoding);
    
    [LibraryImport(LibObjC)]
    private static partial IntPtr class_getInstanceMethod(IntPtr cls, IntPtr selector);

    [LibraryImport(LibObjC, StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr method_getImplementation(IntPtr method);

    [LibraryImport(LibObjC, StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr method_getTypeEncoding(IntPtr method);

    [LibraryImport(LibObjC, StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr imp_implementationWithBlock(IntPtr block);
    
    [LibraryImport(LibObjC, StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr class_replaceMethod(IntPtr cls, IntPtr selector, IntPtr imp, IntPtr types);
    
    [LibraryImport(LibObjC, StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr objc_allocateClassPair(IntPtr superclass, string name, int extraBytes);

    [LibraryImport(LibObjC)]
    private static partial void objc_registerClassPair(IntPtr cls);

    [LibraryImport(LibObjC, StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.Bool)] 
    private static partial bool class_addMethod(IntPtr cls, IntPtr name, IntPtr imp, string types); 
    
    [LibraryImport(LibObjC)]
    public static partial void method_exchangeImplementations(IntPtr method1, IntPtr method2);
        
    [LibraryImport(LibSystem, StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr dlopen(string path, int mode);
    
    private const int RTLD_NOW = 2;
    private const uint kCFStringEncodingUTF8 = 0x08000100;
    
    private static uint kCFUserNotificationNoDefaultButtonFlag = 1 << 5;
    private static uint kCFUserNotificationUseRadioButtonsFlag = 1 << 6;
    
    private const string LibObjC = "/usr/lib/libobjc.dylib";
    private const string LibSystem = "/usr/lib/libSystem.dylib";
    private const string Foundation = "/System/Library/Frameworks/Foundation.framework/Foundation";
    private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    private const string UserNotifications = "/System/Library/Frameworks/UserNotifications.framework/UserNotifications";
    private const string AppKit = "/System/Library/Frameworks/AppKit.framework/AppKit";
    
    // Helper method to convert a C# string to an NSString
    private static IntPtr NSStringFromString(string str)
    {
        IntPtr nsString = objc_getClass("NSString");
        IntPtr alloc = sel_registerName("alloc");
        IntPtr initWithUTF8String = sel_registerName("initWithUTF8String:");

        IntPtr nsStringInstance = objc_msgSend(nsString, alloc);
        IntPtr utf8Ptr = Marshal.StringToHGlobalAuto(str);
        nsStringInstance = objc_msgSend(nsStringInstance, initWithUTF8String, utf8Ptr);
        Marshal.FreeHGlobal(utf8Ptr);

        return nsStringInstance;
    }
   
    private static void Swizzle()
    {
        // Test: invoke the BundleIdentifierPtr
        TwoArgWithRetDelegate del = Marshal.GetDelegateForFunctionPointer<TwoArgWithRetDelegate>(BundleIdentifierPtr);
        del.Invoke(IntPtr.Zero, IntPtr.Zero);
        
        SwizzleBundleIdentifier(); 
        //SwizzleBundleUrl();
    }

    static void SwizzleBundleIdentifier()
    {
        IntPtr bundleClass = objc_getClass("NSBundle");
        IntPtr selector = sel_registerName("bundleIdentifier");
        IntPtr originalMethod = class_getInstanceMethod(bundleClass, selector);
        IntPtr originalImp = method_getImplementation(originalMethod);
       
        // Create a block for the method implementation
        IntPtr block = imp_implementationWithBlock(BundleIdentifierPtr);
        
        // Test: print the original method signature 
        IntPtr encoding = method_getTypeEncoding(originalMethod);
        string? encodingStr = Marshal.PtrToStringAnsi(encoding);
        
        //class_replaceMethod(bundleClass, selector, block, encoding);
        class_replaceMethod(bundleClass, selector, block, IntPtr.Zero);
    }
    
    static void SwizzleBundleUrl()
    {
        IntPtr bundleClass = objc_getClass("NSBundle");
        IntPtr selector = sel_registerName("bundleURL");
        IntPtr originalMethod = class_getInstanceMethod(bundleClass, selector);
       
        // Create a block for the method implementation
        IntPtr block = imp_implementationWithBlock(BundleUrlPtr);
        IntPtr encoding = method_getTypeEncoding(originalMethod);
        
        class_replaceMethod(bundleClass, selector, block, encoding);
    }
}
