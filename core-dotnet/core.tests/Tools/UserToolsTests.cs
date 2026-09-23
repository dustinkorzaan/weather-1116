using System.Text.Json;
using Core.Tools;
using Core.Users.Events;
using Core.Users.Models;
using CQMediator;
using Microsoft.Extensions.AI;
using OpenAI.Responses;

namespace Core.Tests.Tools;

public class UserToolsTests
{
    private static readonly Guid PinId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void UserToolFunctions_ExposesExactlyTheThreeUserTools()
    {
        var names = new UserToolFunctions(new RecordingMediator()).CreateTools()
            .Select(tool => tool.Name)
            .Order()
            .ToArray();

        Assert.Equal(["AddUserPin", "DeleteUserPin", "GetUser"], names);
    }

    [Fact]
    public async Task UserToolFunctions_GetUser_ReturnsPinsWithIds()
    {
        var mediator = new RecordingMediator();
        var getUser = (AIFunction)new UserToolFunctions(mediator).CreateTools().Single(tool => tool.Name == "GetUser");

        var result = await getUser.InvokeAsync(new AIFunctionArguments());

        using var json = JsonDocument.Parse(result!.ToString()!);
        var pin = Assert.Single(json.RootElement.GetProperty("UserPins").EnumerateArray());
        Assert.Equal(PinId, pin.GetProperty("Id").GetGuid());
        Assert.Equal("Nashville, Tennessee", pin.GetProperty("LocationName").GetString());
    }

    [Fact]
    public async Task UserToolFunctions_AddAndDelete_SendTheUserEvents()
    {
        var mediator = new RecordingMediator();
        var tools = new UserToolFunctions(mediator).CreateTools().Cast<AIFunction>().ToDictionary(tool => tool.Name);

        await tools["AddUserPin"].InvokeAsync(new AIFunctionArguments
        {
            ["latitude"] = 30.2672,
            ["longitude"] = -97.7431,
            ["locationName"] = "Austin, Texas",
        });
        await tools["DeleteUserPin"].InvokeAsync(new AIFunctionArguments { ["userPinId"] = PinId });

        var added = Assert.IsType<AddUserPinEvent>(mediator.Sent[0]);
        Assert.Equal("Austin, Texas", added.LocationName);
        Assert.Equal(30.2672, added.Latitude);
        var deleted = Assert.IsType<DeleteUserPinEvent>(mediator.Sent[1]);
        Assert.Equal(PinId, deleted.UserPinId);
    }

    [Fact]
    public async Task WeatherToolExecutor_GetUser_ReturnsTheUserAsJson()
    {
        var executor = new WeatherToolExecutor(new RecordingMediator());
        var call = ResponseItem.CreateFunctionCallItem("call-1", "GetUser", BinaryData.FromString("{}"));

        var output = await executor.ExecuteAsync(call, CancellationToken.None);

        using var json = JsonDocument.Parse(output);
        Assert.Equal(PinId, json.RootElement.GetProperty("UserPins")[0].GetProperty("Id").GetGuid());
    }

    [Fact]
    public void WeatherToolDefinitions_GetUserTakesNoArguments()
    {
        var tool = WeatherToolDefinitions.CreateGetUserTool();

        Assert.Equal("GetUser", tool.FunctionName);
        using var schema = JsonDocument.Parse(tool.FunctionParameters);
        Assert.Empty(schema.RootElement.GetProperty("properties").EnumerateObject());
    }

    private sealed class RecordingMediator : IMediator
    {
        public List<object> Sent { get; } = [];

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            Sent.Add(request);
            if (request is GetUserEvent)
            {
                object user = new UserDTO
                {
                    FirstName = "Anonymous",
                    UserPins =
                    [
                        new UserPinDTO { Id = PinId, Latitude = 36.1627, Longitude = -86.7816, LocationName = "Nashville, Tennessee" },
                    ],
                };
                return Task.FromResult((TResponse)user);
            }

            throw new NotSupportedException(request.GetType().Name);
        }

        public Task Send(IRequest request, CancellationToken cancellationToken = default)
        {
            Sent.Add(request);
            return Task.CompletedTask;
        }
    }
}
