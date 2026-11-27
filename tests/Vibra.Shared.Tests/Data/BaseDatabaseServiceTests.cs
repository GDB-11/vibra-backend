using BindSharp;

namespace Vibra.Shared.Tests.Data;

public sealed class BaseDatabaseServiceTests : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private readonly ProbeRepository _repository;

    public BaseDatabaseServiceTests(PostgresFixture postgres)
    {
        _postgres = postgres;
        _repository = new(postgres.DataSource);
    }

    public Task InitializeAsync() => _postgres.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Queries_map_columns_by_name()
    {
        AssertSuccess(await _repository.AddAsync(1, 1, default));

        Assert.Equal(new ItemRow(1, "item"), AssertSuccess(await _repository.FindAsync(1, default)));
        Assert.Equal([new ItemRow(1, "item")], AssertSuccess(await _repository.ListAsync(default)));
        Assert.Null(AssertSuccess(await _repository.FindAsync(99, default)));
    }

    [Fact]
    public async Task Unique_violation_exposes_the_constraint()
    {
        AssertSuccess(await _repository.AddAsync(1, 1, default));

        var result = await _repository.AddAsync(1, 1, default);

        Assert.True(result.IsFailure, result.ToString());
        Assert.Equal(new DuplicateItemError("pk_item"), result.Error);
    }

    [Fact]
    public async Task Foreign_key_violation_exposes_the_constraint()
    {
        var result = await _repository.AddAsync(1, 404, default);

        Assert.True(result.IsFailure, result.ToString());
        Assert.Equal(new MissingOwnerError("fk_item_owner_id"), result.Error);
    }

    [Fact]
    public async Task Successful_transaction_is_committed()
    {
        AssertSuccess(await _repository.AddInTransactionAsync(1, () => 1, default));

        Assert.Equal(1L, AssertSuccess(await _repository.CountAsync(default)));
    }

    [Fact]
    public async Task Failed_result_rolls_the_transaction_back()
    {
        var result = await _repository.AddInTransactionAsync(1, () => new ItemRejectedError(), default);

        Assert.True(result.IsFailure, result.ToString());
        Assert.IsType<ItemRejectedError>(result.Error);
        Assert.Equal(0L, AssertSuccess(await _repository.CountAsync(default)));
    }

    [Fact]
    public async Task Exception_rolls_back_and_becomes_an_error()
    {
        var result = await _repository.AddInTransactionAsync(
            1, () => throw new InvalidOperationException("boom"), default);

        Assert.True(result.IsFailure, result.ToString());
        Assert.IsType<ItemDataError>(result.Error);
        Assert.Equal(0L, AssertSuccess(await _repository.CountAsync(default)));
    }

    [Fact]
    public async Task Cancellation_before_commit_rolls_back_and_propagates()
    {
        using var source = new CancellationTokenSource();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _repository.AddInTransactionAsync(
            1,
            () =>
            {
                source.Cancel();
                return 1;
            },
            source.Token));

        Assert.Equal(0L, AssertSuccess(await _repository.CountAsync(default)));
    }

    [Fact]
    public async Task Already_cancelled_token_is_not_a_data_error() =>
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _repository.ListAsync(new CancellationToken(canceled: true)));

    [Fact]
    public async Task Running_query_can_be_cancelled()
    {
        using var source = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        var started = TimeProvider.System.GetTimestamp();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _repository.SleepAsync(source.Token));

        Assert.True(TimeProvider.System.GetElapsedTime(started) < TimeSpan.FromSeconds(10));
    }

    private static T AssertSuccess<T>(Result<T, ItemError> result)
    {
        Assert.True(result.IsSuccess, result.ToString());
        return result.Value;
    }
}
