using Microsoft.AspNetCore.SignalR;

namespace Menuz.Api.Hubs;

/// <summary>
/// Hub de tempo real (em configuração nesta etapa).
/// Próxima sprint: o endpoint de pedidos chamará
/// IHubContext&lt;OrderHub&gt;.Clients.Group("kitchen").SendAsync("NewOrder", pedido)
/// e o KDS escutará o evento "NewOrder".
/// </summary>
public class OrderHub : Hub
{
    public const string KitchenGroup = "kitchen";

    public Task JoinKitchen() => Groups.AddToGroupAsync(Context.ConnectionId, KitchenGroup);

    public Task LeaveKitchen() => Groups.RemoveFromGroupAsync(Context.ConnectionId, KitchenGroup);
}
