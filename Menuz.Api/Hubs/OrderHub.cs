using Menuz.Api.Data;
using Menuz.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Menuz.Api.Hubs;

/// <summary>
/// Hub de tempo real entre salão e equipe (cozinha e gerência). Exige login: os pedidos não ficam expostos ao público.
/// Servidor → equipe: "NewOrder" (pedido criado) e "OrderUpdated" (status alterado), enviados pelo OrdersController.
/// Equipe → servidor: JoinKitchen/LeaveKitchen e AcknowledgeOrder (confirma que o pedido apareceu na tela).
/// </summary>
[Authorize(Roles = Roles.Staff)]
public class OrderHub(MenuzDbContext db, ILogger<OrderHub> logger) : Hub
{
    public const string KitchenGroup = "kitchen";

    // Nomes dos eventos (o app precisa usar os mesmos).
    public const string NewOrderEvent = "NewOrder";
    public const string OrderUpdatedEvent = "OrderUpdated";

    public Task JoinKitchen() => Groups.AddToGroupAsync(Context.ConnectionId, KitchenGroup);

    public Task LeaveKitchen() => Groups.RemoveFromGroupAsync(Context.ConnectionId, KitchenGroup);

    /// <summary>
    /// Chamado pela tela da cozinha assim que exibe um pedido novo.
    /// Registra o horário da primeira confirmação (REQ. 5) e grava a medição no log.
    /// </summary>
    public async Task AcknowledgeOrder(int orderId)
    {
        var now = DateTime.UtcNow;

        // Atualização condicional: só a primeira tela que confirmar grava o horário.
        var updated = await db.Orders
            .Where(o => o.Id == orderId && o.KitchenReceivedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.KitchenReceivedAt, now));

        if (updated == 0) return;

        var order = await db.Orders.AsNoTracking()
            .Where(o => o.Id == orderId)
            .Select(o => new { o.CreatedAt, o.ClientSentAt })
            .FirstAsync();

        var serverToKitchenMs = (now - order.CreatedAt).TotalMilliseconds;
        double? endToEndMs = order.ClientSentAt is { } sent ? (now - sent).TotalMilliseconds : null;

        logger.LogInformation(
            "Latência pedido {OrderId}: servidor→cozinha {ServerToKitchenMs:F0} ms; tablet→cozinha {EndToEndMs} ms",
            orderId, serverToKitchenMs, endToEndMs?.ToString("F0") ?? "n/d");
    }
}
