namespace ProjetoIntegradorMenuz.Models
{
    // Espelham os DTOs de /api/tables/{n}/bill. Os valores numéricos do enum devem coincidir com os da API.
    public enum BillStatus { Open = 0, Paid = 1 }

    /// <summary>Conta aberta da mesa: todos os pedidos feitos desde o primeiro até o fechamento.</summary>
    public class BillDto
    {
        public int Id { get; set; }
        public int TableNumber { get; set; }
        public DateTime OpenedAt { get; set; }
        public BillStatus Status { get; set; }

        /// <summary>Total da conta (a API já exclui os pedidos cancelados).</summary>
        public decimal Total { get; set; }

        public List<OrderDto> Orders { get; set; } = new();

        /// <summary>Preenchidos só depois que a conta é paga.</summary>
        public DateTime? ClosedAt { get; set; }
        public decimal? TotalPaid { get; set; }
    }

    /// <summary>Fechamento da conta: envia o total que o cliente viu no resumo.</summary>
    public class CloseBillRequest
    {
        public decimal ExpectedTotal { get; set; }
    }
}
