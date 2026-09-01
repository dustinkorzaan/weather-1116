using Microsoft.Extensions.DependencyInjection;

namespace CQMediator.Tests;

public class MediatorTests
{
    [Fact]
    public async Task Send_ResolvesHandlerForRequestWithResponse()
    {
        var mediator = BuildMediator();

        var response = await mediator.Send(new PingEvent { Message = "hi" });

        Assert.Equal("pong: hi", response.Reply);
    }

    [Fact]
    public async Task Send_ResolvesHandlerForRequestWithoutResponse()
    {
        var services = BuildServices();
        var recorder = new Recorder();
        services.AddSingleton(recorder);

        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();
        await mediator.Send(new RecordEvent { Value = "written" });

        Assert.Equal("written", recorder.Value);
    }

    [Fact]
    public async Task Send_PassesCancellationTokenToHandler()
    {
        var mediator = BuildMediator();
        using var cts = new CancellationTokenSource();

        var response = await mediator.Send(new PingEvent { Message = "hi" }, cts.Token);

        Assert.Equal(cts.Token, response.Token);
    }

    [Fact]
    public async Task Send_ThrowsWhenNoHandlerIsRegistered()
    {
        var mediator = new ServiceCollection()
            .AddCQMediator(cfg => cfg.RegisterServicesFromAssemblyContaining<MediatorTests>())
            .BuildServiceProvider()
            .GetRequiredService<IMediator>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.Send(new UnhandledEvent()));

        Assert.Contains(nameof(UnhandledEvent), exception.Message);
    }

    [Fact]
    public async Task Send_DoesNotWrapHandlerExceptions()
    {
        var mediator = BuildMediator();

        await Assert.ThrowsAsync<InvalidTimeZoneException>(() => mediator.Send(new ThrowingEvent()));
    }

    [Fact]
    public void AddCQMediator_RegistersMediatorAsTransient()
    {
        var descriptor = Assert.Single(
            BuildServices(),
            d => d.ServiceType == typeof(IMediator));

        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
    }

    [Fact]
    public void AddCQMediator_RegisteringSameAssemblyTwiceDoesNotDuplicateHandlers()
    {
        var services = new ServiceCollection().AddCQMediator(cfg => cfg
            .RegisterServicesFromAssemblyContaining<PingHandler>()
            .RegisterServicesFromAssemblyContaining<PingHandler>());

        Assert.Single(services, d => d.ServiceType == typeof(IRequestHandler<PingEvent, PingResponse>));
    }

    [Fact]
    public async Task Send_ResolvesHandlerFromTheCallingScope()
    {
        var services = BuildServices();
        services.AddScoped<ScopedDependency>();
        var provider = services.BuildServiceProvider(validateScopes: true);

        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var response = await mediator.Send(new ScopedEvent());

        Assert.Equal(scope.ServiceProvider.GetRequiredService<ScopedDependency>().Id.ToString(), response.Reply);
    }

    [Fact]
    public async Task Send_CachesWrappersSeparatelyForTypedAndVoidOverloads()
    {
        var services = BuildServices();
        var recorder = new Recorder();
        services.AddSingleton(recorder);
        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        var dual = new DualEvent { Message = "dual" };

        await mediator.Send((IRequest)dual);
        Assert.Equal("void: dual", recorder.Value);

        var response = await mediator.Send((IRequest<PingResponse>)dual);
        Assert.Equal("typed: dual", response.Reply);

        await mediator.Send((IRequest)dual);
        Assert.Equal("void: dual", recorder.Value);
    }

    [Fact]
    public async Task Send_CachesWrappersSeparatelyForCovariantResponseTypes()
    {
        var mediator = BuildMediator();
        var evt = new PingEvent { Message = "hi" };

        var response = await mediator.Send(evt);
        Assert.Equal("pong: hi", response.Reply);

        // Covariance makes PingEvent usable as IRequest<object>; that must not
        // reuse the PingResponse wrapper entry (which would InvalidCastException).
        IRequest<object> asObject = evt;
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Send(asObject));
        Assert.Contains(nameof(PingEvent), exception.Message);

        response = await mediator.Send(evt);
        Assert.Equal("pong: hi", response.Reply);
    }

    private static IMediator BuildMediator()
        => BuildServices().BuildServiceProvider().GetRequiredService<IMediator>();

    private static IServiceCollection BuildServices()
        => new ServiceCollection().AddCQMediator(cfg => cfg.RegisterServicesFromAssemblyContaining<PingHandler>());
}

public sealed class PingEvent : IRequest<PingResponse>
{
    public required string Message { get; init; }
}

public sealed class PingResponse
{
    public required string Reply { get; init; }
    public CancellationToken Token { get; init; }
}

public sealed class PingHandler : IRequestHandler<PingEvent, PingResponse>
{
    public Task<PingResponse> Handle(PingEvent request, CancellationToken cancellationToken)
        => Task.FromResult(new PingResponse { Reply = $"pong: {request.Message}", Token = cancellationToken });
}

public sealed class UnhandledEvent : IRequest<PingResponse>;

public sealed class ThrowingEvent : IRequest<PingResponse>;

public sealed class ThrowingHandler : IRequestHandler<ThrowingEvent, PingResponse>
{
    public Task<PingResponse> Handle(ThrowingEvent request, CancellationToken cancellationToken)
        => throw new InvalidTimeZoneException("handler failed");
}

public sealed class RecordEvent : IRequest
{
    public required string Value { get; init; }
}

public sealed class Recorder
{
    public string? Value { get; set; }
}

public sealed class RecordHandler(Recorder recorder) : IRequestHandler<RecordEvent>
{
    public Task Handle(RecordEvent request, CancellationToken cancellationToken)
    {
        recorder.Value = request.Value;
        return Task.CompletedTask;
    }
}

public sealed class ScopedDependency
{
    public Guid Id { get; } = Guid.NewGuid();
}

public sealed class ScopedEvent : IRequest<PingResponse>;

public sealed class ScopedHandler(ScopedDependency dependency) : IRequestHandler<ScopedEvent, PingResponse>
{
    public Task<PingResponse> Handle(ScopedEvent request, CancellationToken cancellationToken)
        => Task.FromResult(new PingResponse { Reply = dependency.Id.ToString(), Token = cancellationToken });
}

public sealed class DualEvent : IRequest, IRequest<PingResponse>
{
    public required string Message { get; init; }
}

public sealed class DualVoidHandler(Recorder recorder) : IRequestHandler<DualEvent>
{
    public Task Handle(DualEvent request, CancellationToken cancellationToken)
    {
        recorder.Value = $"void: {request.Message}";
        return Task.CompletedTask;
    }
}

public sealed class DualTypedHandler : IRequestHandler<DualEvent, PingResponse>
{
    public Task<PingResponse> Handle(DualEvent request, CancellationToken cancellationToken)
        => Task.FromResult(new PingResponse { Reply = $"typed: {request.Message}", Token = cancellationToken });
}
