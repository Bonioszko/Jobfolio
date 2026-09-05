namespace App.Application;

public interface IApplicationDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken);
}
