cd \r
func init DemoFuncApp --worker-runtime dotnet-isolated --target-framework net10.0
cd DemoFuncApp
func new --template "HTTP trigger" --name HttpExample
dotnet build
func start

<PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.Mcp" Version="1.6.0" />

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;

namespace WeatherMcpSrvFuncApp;

/// <summary>
/// MCP tool that resolves a location name to ranked latitude/longitude matches via Core/CQMediator.
/// </summary>
public class MyDemoTool()
{
	[Function(nameof(MyDemo))]
	public async Task<int> MyDemo(
		[McpToolTrigger(
			"MyDemo",
			"Demo Tool.")]
		ToolInvocationContext context,
		[McpToolProperty(
			"demoProperty",
			"Demo property for the MyDemo tool.",
			true)]
		string demoProperty)
	{
		return demoProperty.Length;
	}
}

>Azure Functions: Initialize Project for Use with VS Code
>Azurite: Start

dotnet restore
dotnet build
