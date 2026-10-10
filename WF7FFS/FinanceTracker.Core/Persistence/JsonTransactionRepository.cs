using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

using FinanceTracker.Core.Exceptions;
using FinanceTracker.Core.Models;

namespace FinanceTracker.Core.Persistence;

// Tranzakciók egyetlen JSON fájlban (System.Text.Json, aszinkron I/O).
// Mentéskor előbb egy ideiglenes fájlba ír, csak sikeres írást követően cseréli le a célfájlt (redundancia)
// Az exceptionöket "DataAccessException" rakja.
public sealed class JsonTransactionRepository : ITransactionRepository
{
    private const int BufferSize = 4096;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        // UTF 8 kódolás, olvasható magyarul.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        // enum használata, számok helyett szöveges érték, így emberileg értelmezhető.
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _filePath;

    public JsonTransactionRepository(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = Path.GetFullPath(filePath);
    }

    public async Task<IReadOnlyList<Transaction>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            // első indítás: nincs mentett adat, üres lista
            return new List<Transaction>();
        }

        try
        {
            await using FileStream stream = new(
                _filePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true);

            List<Transaction>? transactions =
                await JsonSerializer.DeserializeAsync<List<Transaction>>(stream, SerializerOptions, cancellationToken);

            return transactions ?? new List<Transaction>();
        }
        catch (JsonException ex)
        {
            throw new DataAccessException(
                $"A(z) '{_filePath}' fájl sérült vagy hibás formátumú, nem olvasható be.", ex);
        }
        catch (InvalidTransactionException ex)
        {
            throw new DataAccessException(
                $"A(z) '{_filePath}' fájl érvénytelen tranzakciót tartalmaz: {ex.Message}", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new DataAccessException($"Nincs jogosultság a(z) '{_filePath}' fájl olvasásához.", ex);
        }
        catch (IOException ex)
        {
            throw new DataAccessException($"A(z) '{_filePath}' fájl olvasása sikertelen: {ex.Message}", ex);
        }
    }

    public async Task SaveAsync(IEnumerable<Transaction> transactions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transactions);

        string tempPath = _filePath + ".tmp";

        try
        {
            string? directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using (FileStream stream = new(
                tempPath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, useAsync: true))
            {
                await JsonSerializer.SerializeAsync(stream, transactions, SerializerOptions, cancellationToken);
            }

            File.Move(tempPath, _filePath, overwrite: true);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new DataAccessException($"Nincs jogosultság a(z) '{_filePath}' fájl írásához.", ex);
        }
        catch (IOException ex)
        {
            throw new DataAccessException($"A(z) '{_filePath}' fájl mentése sikertelen: {ex.Message}", ex);
        }
    }
}
