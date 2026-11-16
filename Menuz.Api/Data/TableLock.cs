using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Menuz.Api.Data;

public static class TableLock
{
    /// <summary>
    /// Trava (advisory lock do PostgreSQL) por número de mesa, válida até o fim da transação atual.
    /// Quem criar pedido ou fechar a conta da mesma mesa espera a outra operação terminar,
    /// então o estado da conta lido depois da trava é sempre o mais recente.
    /// Precisa ser chamada dentro de uma transação.
    /// </summary>
    public static Task LockTableAsync(this DatabaseFacade database, int tableNumber, CancellationToken ct) =>
        database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({(long)tableNumber})", ct);
}
