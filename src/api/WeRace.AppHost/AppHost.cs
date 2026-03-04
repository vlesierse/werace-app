var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .WithDataVolume("werace-pgdata");

var db = postgres.AddDatabase("werace");

var redis = builder.AddRedis("redis")
    .WithRedisInsight();

builder.AddProject<Projects.WeRace_Api>("api")
    .WithReference(db)
    .WithReference(redis)
    .WaitFor(postgres)
    .WaitFor(redis);

// TODO: Integrate WeRace.DataImport as an Aspire resource for automated dev seeding.
// Once Aspire supports one-shot "run to completion" project references, add:
//   builder.AddProject<Projects.WeRace_DataImport>("data-import")
//       .WithReference(db)
//       .WaitFor(postgres);
// For now, run the import CLI manually:
//   dotnet run --project src/api/WeRace.DataImport -- --source db/seed/jolpica-dump.sql --connection <conn> --mode full

builder.Build().Run();
