using FinanceTracker.Core.Exceptions;
using FinanceTracker.Core.Models;
using FinanceTracker.Core.Persistence;

using NUnit.Framework;

namespace FinanceTracker.Tests.Persistence;

[TestFixture]
public class JsonTransactionRepositoryTests
{
    private string _directory = string.Empty;
    private string _filePath = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), "FinanceTrackerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _filePath = Path.Combine(_directory, "transactions.json");
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private static List<Transaction> CreateSampleTransactions()
    {
        return new List<Transaction>
        {
            Transaction.Create(new DateOnly(2026, 3, 1), 450000m, TransactionType.Income, "Fizetés", "Márciusi bér"),
            Transaction.Create(new DateOnly(2026, 3, 5), 1234.56m, TransactionType.Expense, "Étkezés", "Heti bevásárlás"),
            Transaction.Create(new DateOnly(2026, 3, 9), 9990m, TransactionType.Expense, "Szórakozás")
        };
    }

    // Betöltés

    [Test]
    public async Task LoadAsync_FileDoesNotExist_ReturnsEmptyList()
    {
        JsonTransactionRepository repository = new(_filePath);

        IReadOnlyList<Transaction> loaded = await repository.LoadAsync();

        Assert.That(loaded, Is.Empty);
    }

    [Test]
    public async Task LoadAsync_JsonNullContent_ReturnsEmptyList()
    {
        await File.WriteAllTextAsync(_filePath, "null");
        JsonTransactionRepository repository = new(_filePath);

        IReadOnlyList<Transaction> loaded = await repository.LoadAsync();

        Assert.That(loaded, Is.Empty);
    }

    [Test]
    public async Task LoadAsync_CorruptJson_ThrowsDataAccessExceptionWithInnerException()
    {
        await File.WriteAllTextAsync(_filePath, "{ ez nem json ");
        JsonTransactionRepository repository = new(_filePath);

        DataAccessException ex = Assert.ThrowsAsync<DataAccessException>(async () => await repository.LoadAsync());

        Assert.Multiple(() =>
        {
            Assert.That(ex.Message, Does.Contain("sérült"));
            Assert.That(ex.InnerException, Is.Not.Null);
        });
    }

    [Test]
    public async Task LoadAsync_EmptyFile_ThrowsDataAccessException()
    {
        await File.WriteAllTextAsync(_filePath, string.Empty);
        JsonTransactionRepository repository = new(_filePath);

        Assert.ThrowsAsync<DataAccessException>(async () => await repository.LoadAsync());
    }

    [Test]
    public async Task LoadAsync_NonPositiveAmountInFile_ThrowsDataAccessException()
    {
        const string json = """
            [
              {
                "Id": "3f2b8c1e-5a4d-4e6f-9b0a-1c2d3e4f5a6b",
                "Date": "2026-03-15",
                "Amount": -5,
                "Type": "Expense",
                "Category": "Étkezés",
                "Description": ""
              }
            ]
            """;
        await File.WriteAllTextAsync(_filePath, json);
        JsonTransactionRepository repository = new(_filePath);

        DataAccessException ex = Assert.ThrowsAsync<DataAccessException>(async () => await repository.LoadAsync());

        Assert.That(ex.Message, Does.Contain("pozitív"));
    }

    [Test]
    public async Task LoadAsync_MissingRequiredProperty_ThrowsDataAccessException()
    {
        const string json = """
            [
              {
                "Id": "3f2b8c1e-5a4d-4e6f-9b0a-1c2d3e4f5a6b",
                "Date": "2026-03-15",
                "Amount": 500,
                "Type": "Expense"
              }
            ]
            """;
        await File.WriteAllTextAsync(_filePath, json);
        JsonTransactionRepository repository = new(_filePath);

        Assert.ThrowsAsync<DataAccessException>(async () => await repository.LoadAsync());
    }

    // Mentés

    [Test]
    public async Task SaveAsync_ThenLoadAsync_RoundTripPreservesAllTransactions()
    {
        List<Transaction> original = CreateSampleTransactions();
        JsonTransactionRepository repository = new(_filePath);

        await repository.SaveAsync(original);
        IReadOnlyList<Transaction> loaded = await repository.LoadAsync();

        Assert.That(loaded, Is.EqualTo(original));
    }

    [Test]
    public async Task SaveAsync_WritesReadableTextWithEnumNamesAndAccents()
    {
        JsonTransactionRepository repository = new(_filePath);

        await repository.SaveAsync(CreateSampleTransactions());
        string text = await File.ReadAllTextAsync(_filePath);

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.Contain("Étkezés"));
            Assert.That(text, Does.Contain("\"Expense\""));
            Assert.That(text, Does.Contain("2026-03-05"));
        });
    }

    [Test]
    public async Task SaveAsync_OverwritesPreviousContent()
    {
        List<Transaction> original = CreateSampleTransactions();
        JsonTransactionRepository repository = new(_filePath);
        await repository.SaveAsync(original);

        await repository.SaveAsync(original.Take(1));
        IReadOnlyList<Transaction> loaded = await repository.LoadAsync();

        Assert.That(loaded, Is.EqualTo(new[] { original[0] }));
    }

    [Test]
    public async Task SaveAsync_EmptyCollection_SavesAndLoadsEmptyList()
    {
        JsonTransactionRepository repository = new(_filePath);

        await repository.SaveAsync(new List<Transaction>());
        IReadOnlyList<Transaction> loaded = await repository.LoadAsync();

        Assert.That(loaded, Is.Empty);
    }

    [Test]
    public async Task SaveAsync_MissingDirectory_CreatesItAndSaves()
    {
        string nestedPath = Path.Combine(_directory, "almappa", "adatok", "transactions.json");
        JsonTransactionRepository repository = new(nestedPath);

        await repository.SaveAsync(CreateSampleTransactions());

        Assert.That(File.Exists(nestedPath), Is.True);
    }

    [Test]
    public async Task SaveAsync_Success_LeavesNoTemporaryFileBehind()
    {
        JsonTransactionRepository repository = new(_filePath);

        await repository.SaveAsync(CreateSampleTransactions());

        Assert.That(File.Exists(_filePath + ".tmp"), Is.False);
    }

    [Test]
    public async Task SaveAsync_ParentPathIsAFile_ThrowsDataAccessException()
    {
        string blocker = Path.Combine(_directory, "blocker");
        await File.WriteAllTextAsync(blocker, "ez egy fájl, nem mappa");
        JsonTransactionRepository repository = new(Path.Combine(blocker, "transactions.json"));

        Assert.ThrowsAsync<DataAccessException>(async () => await repository.SaveAsync(CreateSampleTransactions()));
    }

    [Test]
    public void SaveAsync_NullTransactions_ThrowsArgumentNullException()
    {
        JsonTransactionRepository repository = new(_filePath);

        Assert.ThrowsAsync<ArgumentNullException>(async () => await repository.SaveAsync(null!));
    }

    // Konstruktor

    [TestCase("")]
    [TestCase("   ")]
    public void Constructor_BlankPath_ThrowsArgumentException(string path)
    {
        Assert.Throws<ArgumentException>(() => new JsonTransactionRepository(path));
    }
}
