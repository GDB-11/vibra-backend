using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Npgsql;
using Vibra.Shared.Api;

namespace Vibra.ArchitectureTests.Fixtures.Controllers;

public sealed class PlainController : ControllerBase;

[ApiController]
public sealed class RepeatedAttributeController(ILogger<FunctionalController> logger) : FunctionalController(logger);

public sealed class DataAccessController(ILogger<FunctionalController> logger, NpgsqlDataSource dataSource)
    : FunctionalController(logger)
{
    public NpgsqlDataSource DataSource => dataSource;
}

public sealed class ValidController(ILogger<FunctionalController> logger) : FunctionalController(logger);
