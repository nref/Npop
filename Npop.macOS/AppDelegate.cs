using System;
using System.Threading.Tasks;
using AppKit;
using Foundation;
using UserNotifications;

namespace Npop.macOS;

public class AppDelegate : NSApplicationDelegate
{
    public override void DidFinishLaunching(NSNotification notification)
    {
        Console.WriteLine(nameof(DidFinishLaunching));
        
        // Set the delegate for notification center
        UNUserNotificationCenter.Current.Delegate = new UserNotificationCenterDelegate();
        
        _ = Task.Run(async () =>
        {
            // Get current permissions
            var status = await UNUserNotificationCenter.Current.GetNotificationSettingsAsync();
            Console.WriteLine($"Status: {status.AuthorizationStatus}");
            
            if (status.AuthorizationStatus == UNAuthorizationStatus.Authorized)
            {
                ScheduleNotification();
                return;
            }
            
            // Request permission to show notifications
            // UNUserNotificationCenter requires the app to be codesigned,
            // else an error is returned: "The operation couldn’t be completed." (UNErrorDomain error 1,
            // i.e. UNErrorDomain.NotificationsNotAllowed
            // See https://github.com/xamarin/xamarin-macios/blob/main/src/usernotifications.cs
            Console.WriteLine($"RequestAuthorizationAsync");
            (bool approved, NSError? err) = await UNUserNotificationCenter.Current.RequestAuthorizationAsync(
                UNAuthorizationOptions.Alert | UNAuthorizationOptions.Sound);
            
            Console.WriteLine($"  Approved?: {approved}");
            if (err is not null)
            {
                Console.WriteLine($"  Err: {err?.LocalizedDescription}");
            }
            
            if (approved)
            {
                ScheduleNotification();
            }
        });
    }

    private void ScheduleNotification()
    {
        Console.WriteLine(nameof(ScheduleNotification));
        
        var content = new UNMutableNotificationContent
        {
            Title = "This is a title",
            Subtitle = "This is a subtitle",
            Body = "This is a body",
            Sound = UNNotificationSound.Default
        };

        // Trigger the notification after 5 seconds
        var trigger = UNTimeIntervalNotificationTrigger.CreateTrigger(5, false);

        var request = UNNotificationRequest.FromIdentifier("notificationId", content, trigger);

        UNUserNotificationCenter.Current.AddNotificationRequest(request, (err) =>
        {
            if (err != null)
            {
                // Handle error
            }
        });
    }

    public override void WillTerminate(NSNotification notification)
    {
        // Insert code here to tear down your application
    }
}