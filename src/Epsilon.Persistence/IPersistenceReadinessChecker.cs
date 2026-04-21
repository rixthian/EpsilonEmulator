namespace Epsilon.Persistence;

public interface IPersistenceReadinessChecker
{
    PersistenceReadinessReport Check();
}

