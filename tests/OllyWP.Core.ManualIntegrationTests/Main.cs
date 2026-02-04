using OllyWP.Core;
using OllyWP.Core.Domain.Entities;
using OllyWP.Core.Domain.Extensions;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var vapidKeys = VapidHelper.GenerateKeys("mailto:development@andreiolaru.com");
Console.WriteLine($"VAPID Public: {vapidKeys.PublicKey}\n");

OllyWp.Initialize(vapidKeys);

app.UseStaticFiles();
app.MapGet("/", () => Results.Redirect("/test-notification.html"));
app.MapGet("/api/vapid-key", () => Results.Json(new { publicKey = vapidKeys.PublicKey }));

app.MapPost("/api/subscribe", async (SubscriptionRequest request) =>
{
    var response = await OllyWp.DoIt()
        .WithPayload(new OllyPayload
        {
            Title = "Notification",
            Message = "It works on my machine",
            Url = "https://github.com/andrei-olaru"
        })
        .WithRecipient(new OllyRecipient
        {
            Endpoint = request.Endpoint,
            P256dh = request.Keys.P256dh,
            Auth = request.Keys.Auth
        })
        .AndSendIt();

    return Results.Json(new { success = response.Success });
});

app.Run("http://localhost:5000");

record SubscriptionRequest(string Endpoint, SubscriptionKeys Keys);
record SubscriptionKeys(string P256dh, string Auth);