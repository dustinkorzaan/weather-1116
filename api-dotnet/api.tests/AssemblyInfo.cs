using Xunit;

// Every WebApplicationFactory<Program>-based test class boots the real Program.cs, which calls
// Database.Migrate() against dbo.AgentActivity on startup. xUnit runs test classes in parallel
// by default, so without this, multiple factories racing to CREATE DATABASE the same target can
// throw "Database '...' already exists" (one wins the race, the other's create call loses it).
// Serializing test classes avoids the race entirely -- this assembly's suite is small enough
// that running it sequentially costs nothing worth trading correctness for.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
