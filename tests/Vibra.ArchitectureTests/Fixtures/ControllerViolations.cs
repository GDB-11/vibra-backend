using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Npgsql;
using Vibra.Shared.Api;

namespace Vibra.ArchitectureTests.Fixtures.Controllers;

public sealed class PlainController : ControllerBase;

[ApiController]
public sealed class RepeatedAttributeController : FunctionalController
{
    public RepeatedAttributeController(ILogger<FunctionalController> logger)
        : base(logger)
    {
    }
}

public sealed class DataAccessController : FunctionalController
{
    private readonly NpgsqlDataSource _dataSource;

    public DataAccessController(ILogger<FunctionalController> logger, NpgsqlDataSource dataSource)
        : base(logger)
    {
        _dataSource = dataSource;
    }

    public NpgsqlDataSource DataSource => _dataSource;
}

public sealed class ValidController : FunctionalController
{
    public ValidController(ILogger<FunctionalController> logger)
        : base(logger)
    {
    }
}
