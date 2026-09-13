using Core.Agent.Events;
using Core.Agent.Handlers;
using Core.Data;
using Core.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace Core.Tests.Agent.Handlers;

public class LogAgentActivityHandlerTests
{
    private static AgentActivityDbContext CreateDbContext(string dbName) =>
        new(new DbContextOptionsBuilder<AgentActivityDbContext>().UseInMemoryDatabase(dbName).Options);

    private sealed class FakeContextProvider(string? context) : IAgentActivityContextProvider
    {
        public string? GetContext() => context;
    }

    [Fact]
    public async Task Handle_RequestRow_DefaultsCorrelationIdToItsOwnId()
    {
        using var dbContext = CreateDbContext(nameof(Handle_RequestRow_DefaultsCorrelationIdToItsOwnId));
        var handler = new LogAgentActivityHandler(dbContext, new AgentActivityHostProvider(AgentActivityHost.Api));
        var runId = Guid.NewGuid();

        var id = await handler.Handle(new LogAgentActivityEvent
        {
            Direction = AgentActivityDirection.Request,
            RunId = runId,
            Feature = "Chat1a",
            FeatureCategory = AgentActivityFeatureCategory.ModelDirect,
            SessionId = "session-1",
            Content = "hello",
        }, CancellationToken.None);

        var row = await dbContext.AgentActivity.SingleAsync();
        Assert.Equal(id, row.Id);
        Assert.Equal(id, row.CorrelationId);
        Assert.Equal(runId, row.RunId);
        Assert.Equal(AgentActivityHost.Api, row.Host);
        Assert.Equal("hello", row.Content);
        Assert.Equal(AgentActivityDirection.Request, row.Direction);
    }

    [Fact]
    public async Task Handle_ResponseRow_UsesProvidedCorrelationIdRatherThanItsOwnId()
    {
        using var dbContext = CreateDbContext(nameof(Handle_ResponseRow_UsesProvidedCorrelationIdRatherThanItsOwnId));
        var handler = new LogAgentActivityHandler(dbContext, new AgentActivityHostProvider(AgentActivityHost.Api));
        var correlationId = Guid.NewGuid();

        var id = await handler.Handle(new LogAgentActivityEvent
        {
            Direction = AgentActivityDirection.Response,
            RunId = Guid.NewGuid(),
            CorrelationId = correlationId,
            Feature = "Chat1a",
            FeatureCategory = AgentActivityFeatureCategory.ModelDirect,
            SessionId = "session-1",
            Content = "hi there",
        }, CancellationToken.None);

        var row = await dbContext.AgentActivity.SingleAsync(a => a.Id == id);
        Assert.Equal(correlationId, row.CorrelationId);
        Assert.NotEqual(correlationId, row.Id);
    }

    [Fact]
    public async Task Handle_ContextProviderRegistered_PopulatesContextColumn()
    {
        using var dbContext = CreateDbContext(nameof(Handle_ContextProviderRegistered_PopulatesContextColumn));
        var handler = new LogAgentActivityHandler(
            dbContext,
            new AgentActivityHostProvider(AgentActivityHost.Api),
            new FakeContextProvider("""{"path":"/Chat1a/messages"}"""));

        await handler.Handle(new LogAgentActivityEvent
        {
            Direction = AgentActivityDirection.Request,
            RunId = Guid.NewGuid(),
            Feature = "Chat1a",
            FeatureCategory = AgentActivityFeatureCategory.ModelDirect,
            SessionId = "session-1",
            Content = "hello",
        }, CancellationToken.None);

        var row = await dbContext.AgentActivity.SingleAsync();
        Assert.Equal("""{"path":"/Chat1a/messages"}""", row.Context);
    }

    [Fact]
    public async Task Handle_NoContextProviderRegistered_LeavesContextNull()
    {
        using var dbContext = CreateDbContext(nameof(Handle_NoContextProviderRegistered_LeavesContextNull));
        var handler = new LogAgentActivityHandler(dbContext, new AgentActivityHostProvider(AgentActivityHost.Worker));

        await handler.Handle(new LogAgentActivityEvent
        {
            Direction = AgentActivityDirection.Request,
            RunId = Guid.NewGuid(),
            Feature = "AIWeatherV3",
            FeatureCategory = AgentActivityFeatureCategory.ModelDirect,
            SessionId = "session-1",
            Content = "hello",
        }, CancellationToken.None);

        var row = await dbContext.AgentActivity.SingleAsync();
        Assert.Null(row.Context);
    }

    [Fact]
    public async Task Handle_NoDbContextOrHostProviderRegistered_NoOpsInsteadOfThrowing()
    {
        // Mirrors mcp-srv-app-service/mcp-srv-func-app: AddStandardCoreServices() registers this
        // handler via CQMediator's assembly scan, but those hosts never register
        // AgentActivityDbContext/IAgentActivityHostProvider.
        var handler = new LogAgentActivityHandler();

        var id = await handler.Handle(new LogAgentActivityEvent
        {
            Direction = AgentActivityDirection.Request,
            RunId = Guid.NewGuid(),
            Feature = "Chat1a",
            FeatureCategory = AgentActivityFeatureCategory.ModelDirect,
            SessionId = "session-1",
            Content = "hello",
        }, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
    }
}
