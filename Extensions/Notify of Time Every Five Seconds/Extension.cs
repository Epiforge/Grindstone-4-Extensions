using System;
using System.Threading;

var notificationDisplayExtensionId = Guid.Parse("{F58D6D8F-ABE6-476F-9C0D-8BC765853B8B}");

Timer timer;

void NotificationClicked()
{
    timer?.Dispose();
    timer = null;
}

void TimerCallback(object state) =>
    Extension.PostMessage(notificationDisplayExtensionId, new {
        Title = "Time Update",
        Text = $"It's {DateTime.Now}! Click me to stop these...",
        AutoCloseMilliseconds = 4000,
        OnClick = (Action)NotificationClicked
    });

timer = new Timer(TimerCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));