using Microsoft.EntityFrameworkCore;
using Pickups.Api.Data;

namespace Pickups.Tests.Fakes;

/// <summary>
/// DataContext cuyo guardado falla las primeras veces con DbUpdateConcurrencyException,
/// como cuando otra operación cambia la recogida al mismo tiempo. Nada de ese intento queda guardado.
/// </summary>
public sealed class FailingDataContext : DataContext
{
    private readonly Func<Task>? _beforeFailure;
    private int _failuresLeft;

    /// <param name="beforeFailure">Acción opcional que se ejecuta justo antes de fallar, para simular la otra operación.</param>
    public FailingDataContext(DbContextOptions<DataContext> options, int failures = 1, Func<Task>? beforeFailure = null)
        : base(options)
    {
        _failuresLeft = failures;
        _beforeFailure = beforeFailure;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_failuresLeft > 0)
        {
            _failuresLeft--;

            if (_beforeFailure is not null)
            {
                await _beforeFailure();
            }

            throw new DbUpdateConcurrencyException("Fallo simulado al guardar la recogida.");
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
