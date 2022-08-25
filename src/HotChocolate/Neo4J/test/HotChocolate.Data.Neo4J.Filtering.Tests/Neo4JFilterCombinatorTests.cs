using CookieCrumble;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Neo4J.Testing;
using HotChocolate.Execution;

namespace HotChocolate.Data.Neo4J.Filtering.Tests;

[Collection(Neo4JDatabaseCollectionFixture.DefinitionName)]
public class Neo4JFilterCombinatorTests : IClassFixture<Neo4JFixture>
{
    private readonly Neo4JDatabase _database;
    private readonly Neo4JFixture _fixture;

    public Neo4JFilterCombinatorTests(Neo4JDatabase database, Neo4JFixture fixture)
    {
        _database = database;
        _fixture = fixture;
    }

    private const string _fooEntitiesCypher =
        @"CREATE (:FooBool {Bar: true}), (:FooBool {Bar: false})";

    [Fact]
    public async Task Create_Empty_Expression()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooBool, FooBoolFilterType>(_database, _fooEntitiesCypher);

        // act
        // assert
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { }){ bar }}")
                .Create());

        await SnapshotExtensions.AddResult(
                Snapshot.Create(), res1)
            .MatchAsync();
    }

    public class FooBool
    {
        public bool Bar { get; set; }
    }

    public class FooBoolFilterType : FilterInputType<FooBool>
    {
    }
}