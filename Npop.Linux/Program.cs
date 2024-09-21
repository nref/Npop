using System.Reflection;
using Tmds.DBus;

// get app icon "sample.png" in exe dir
var dir = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location ?? "");
Console.WriteLine(dir);

var connection = new Connection(Address.Session);
await connection.ConnectAsync();

var notifier = connection.CreateProxy<INotification>("org.freedesktop.Notifications", "/org/freedesktop/Notifications");

await notifier.NotifyAsync("appName", 0, $"{dir}/sample.png", "summary", "body", [], new Dictionary<string, object>() {}, 5000);

await Task.Delay(TimeSpan.FromSeconds(10));
connection.Dispose();

[DBusInterface("org.freedesktop.Notifications")]
public interface INotification : IDBusObject
{
   Task<uint> NotifyAsync(string appName, uint replacesId, string appIcon, string summary, string body,
      string[] actions, IDictionary<string, object> hints, int expireTimeout);
}