namespace ProjetoIntegradorMenuz.Services
{
    /// <summary>A API recusou o pedido (HTTP 400). A mensagem vem do servidor e pode ser exibida ao cliente.</summary>
    public class OrderRejectedException(string message) : Exception(message);
}
