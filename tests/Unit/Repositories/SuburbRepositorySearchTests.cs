using FluentAssertions;
using GridMonitor.Domain.Repositories;
using GridMonitor.Infrastructure.Repositories;
using GridMonitor.Tests.Unit.Shared;

namespace GridMonitor.Tests.Unit.Repositories;

public class SuburbRepositorySearchTests : IDisposable
{
	private readonly SqliteDb _sqlite = new();
	private readonly ISuburbRepository _repo;

	public SuburbRepositorySearchTests() => _repo = new SuburbRepository(_sqlite.Ctx);
	public void Dispose() => _sqlite.Dispose();

	private async Task<int> SeedMuniAsync()
	{
		var p = await Seed.ProvinceAsync(_sqlite.Ctx);
		var m = await Seed.MunicipalityAsync(_sqlite.Ctx, p.Id);
		return m.Id;
	}

	[Fact]
	public async Task SearchAsync_CaseInsensitivePartialMatch_ReturnsMatches()
	{
		var muniId = await SeedMuniAsync();
		await Seed.SuburbAsync(_sqlite.Ctx, muniId, "Milnerton");
		await Seed.SuburbAsync(_sqlite.Ctx, muniId, "Milnerton North");
		await Seed.SuburbAsync(_sqlite.Ctx, muniId, "Bellville");

		var result = await _repo.SearchAsync("milner", 10);

		result.Should().HaveCount(2);
		result.Should().AllSatisfy(s =>
			s.Name.ToLower().Should().Contain("milner"));
	}

	[Fact]
	public async Task SearchAsync_NoMatch_ReturnsEmpty()
	{
		var muniId = await SeedMuniAsync();
		await Seed.SuburbAsync(_sqlite.Ctx, muniId, "Rondebosch");

		var result = await _repo.SearchAsync("zzznomatch", 10);

		result.Should().BeEmpty();
	}

	[Fact]
	public async Task SearchAsync_RespectsLimit()
	{
		var muniId = await SeedMuniAsync();
		for (var i = 0; i < 10; i++)
			await Seed.SuburbAsync(_sqlite.Ctx, muniId, $"Suburb Alpha {i}");

		var result = await _repo.SearchAsync("alpha", limit: 3);

		result.Should().HaveCount(3);
	}

	[Fact]
	public async Task SearchAsync_ResultsOrderedAlphabetically()
	{
		var muniId = await SeedMuniAsync();
		await Seed.SuburbAsync(_sqlite.Ctx, muniId, "Zonnebloem");
		await Seed.SuburbAsync(_sqlite.Ctx, muniId, "Athlone");
		await Seed.SuburbAsync(_sqlite.Ctx, muniId, "Maitland");

		var result = await _repo.SearchAsync("a", limit: 10);

		result.Select(s => s.Name).Should().BeInAscendingOrder();
	}
}
