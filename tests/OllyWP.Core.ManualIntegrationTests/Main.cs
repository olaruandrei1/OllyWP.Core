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

app.MapPost("/api/send-single", async (SubscriptionRequest request) =>
{
    var response = await OllyWp.DoIt()
        .WithPayload(new OllyPayload
        {
            Title = "Single Notification",
            Message = "Testing single recipient mode",
            Url = "https://github.com/andrei-olaru",
            Tag = $"single-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
        })
        .WithRecipient(new OllyRecipient
        {
            Endpoint = request.Endpoint,
            P256dh = request.Keys.P256dh,
            Auth = request.Keys.Auth
        })
        .GibMeLogs()
        .AndSendIt();

    return Results.Json(new
    {
        success = response.Success,
        delivered = response.SuccessfulDeliveries,
        total = response.TotalRecipients
    });
});

app.MapPost("/api/send-multiple", async (SubscriptionRequest request) =>
{
    var recipients = new List<OllyRecipient>
    {
        new() { Endpoint = request.Endpoint, P256dh = request.Keys.P256dh, Auth = request.Keys.Auth },
        new() { Endpoint = request.Endpoint, P256dh = request.Keys.P256dh, Auth = request.Keys.Auth },
        new() { Endpoint = request.Endpoint, P256dh = request.Keys.P256dh, Auth = request.Keys.Auth }
    };

    var response = await OllyWp.DoIt()
        .WithPayload(new OllyPayload
        {
            Title = "Bulk Notification",
            Message = "Testing multiple recipients with same payload",
            Url = "https://github.com/andrei-olaru",
            Tag = $"bulk-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
        })
        .WithRecipients(recipients)
        .GibMeLogs()
        .AndSendIt();

    return Results.Json(new
    {
        success = response.Success,
        delivered = response.SuccessfulDeliveries,
        total = response.TotalRecipients
    });
});

app.MapPost("/api/send-batch", async (SubscriptionRequest request) =>
{
    var recipients = new List<OllyRecipient>
    {
        new() { Endpoint = request.Endpoint, P256dh = request.Keys.P256dh, Auth = request.Keys.Auth },
        new() { Endpoint = request.Endpoint, P256dh = request.Keys.P256dh, Auth = request.Keys.Auth }
    };

    var response = await OllyWp.DoIt()
        .WithBatch(new OllyPayload
        {
            Title = "Batch Notification",
            Message = "Testing batch mode",
            Url = "https://github.com/andrei-olaru",
            Tag = $"batch-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
        }, recipients)
        .GibMeLogs()
        .AndSendItToAll();

    return Results.Json(new
    {
        success = response.Success,
        delivered = response.SuccessfulDeliveries,
        total = response.TotalRecipients
    });
});

app.MapPost("/api/send-batches", async (SubscriptionRequest request) =>
{
    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    var recipient = new OllyRecipient
    {
        Endpoint = request.Endpoint,
        P256dh = request.Keys.P256dh,
        Auth = request.Keys.Auth
    };

    var response = await OllyWp.DoIt()
        .WithBatch(new OllyPayload
        {
            Title = "Batch 1",
            Message = "It worked",
            Url = "https://github.com/andrei-olaru",
            Tag = $"batch1-{timestamp}"
        }, recipient)
        .WithBatch(new OllyPayload
        {
            Title = "Batch 2",
            Message = "It worked",
            Url = "https://github.com/andrei-olaru",
            Tag = $"batch2-{timestamp}"
        }, recipient)
        .WithBatch(new OllyPayload
        {
            Title = "Batch 3",
            Message = "It worked",
            Url = "https://github.com/andrei-olaru",
            Tag = $"batch3-{timestamp}"
        }, recipient)
        .WithMaxParallelism(3)
        .WithContinueOnError(true)
        .GibMeLogs()
        .AndSendItToAll();

    return Results.Json(new
    {
        success = response.Success,
        delivered = response.SuccessfulDeliveries,
        total = response.TotalRecipients,
        batches = 3
    });
});

app.MapPost("/api/send-stress", async (SubscriptionRequest request) =>
{
    var builder = OllyWp.DoIt();

    for (int i = 1; i <= 10; i++)
    {
        builder.WithBatch(new OllyPayload
        {
            Title = $"Stress Test #{i}",
            Message = $"Testing notification {i}/10",
            Url = "https://github.com/andrei-olaru",
            Tag = $"stress-{i}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
        }, new OllyRecipient
        {
            Endpoint = request.Endpoint,
            P256dh = request.Keys.P256dh,
            Auth = request.Keys.Auth
        });
    }

    var response = await builder
        .WithMaxParallelism(5)
        .WithContinueOnError(true)
        .GibMeLogs()
        .AndSendItToAll();

    return Results.Json(new
    {
        success = response.Success,
        delivered = response.SuccessfulDeliveries,
        total = response.TotalRecipients
    });
});

app.Run("http://localhost:5000");

record SubscriptionRequest(string Endpoint, SubscriptionKeys Keys);
record SubscriptionKeys(string P256dh, string Auth);