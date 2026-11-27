// Violaciones deliberadas: demuestran que las reglas de dependencias pueden fallar.
namespace Vibra.ArchitectureTests.Fixtures.Modules.Alpha.Infrastructure
{
    public sealed class AlphaRepository
    {
        public const string Name = "alpha";

        public static int Count() => 0;
    }
}

namespace Vibra.ArchitectureTests.Fixtures.Modules.Beta.Application
{
    using Alpha.Infrastructure;

    public sealed class UsesInConstructor
    {
        private readonly AlphaRepository _repository;

        public UsesInConstructor(AlphaRepository repository)
        {
            _repository = repository;
        }

        public AlphaRepository Repository => _repository;
    }

    public sealed class UsesInMethodBody
    {
        public int Run() => AlphaRepository.Count();
    }

    public sealed class UsesInAsyncMethod
    {
        public async Task<int> RunAsync()
        {
            await Task.Yield();
            return new AlphaRepository().GetHashCode();
        }
    }

    public sealed class Innocent;
}

namespace Vibra.ArchitectureTests.Fixtures.Modules.Beta.Domain
{
    public sealed class DomainUsingApplication
    {
        public object Create() => new Application.Innocent();
    }
}
