using System.Net;
using System.Net.Http.Json;
using CheckPoint.Api.Dtos;
using CheckPoint.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CheckPoint.Api.Tests;

public sealed class FavoritesAndPopularityTests : ApiTestBase
{
    [Fact]
    public async Task Favorite_AddAndRemove_AreIdempotentAndReflectedPerUser()
    {
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);
        var resource = await UploadAsync(admin.Client, university.Id, course.Id);
        var first = await CreateUserAsync();
        var second = await CreateUserAsync();

        Assert.Equal(HttpStatusCode.NoContent, (await first.Client.PutAsync($"/api/resources/{resource.Id}/favorite", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await first.Client.PutAsync($"/api/resources/{resource.Id}/favorite", null)).StatusCode);

        Assert.True((await GetResourceAsync(first, resource.Id)).IsFavorite);
        Assert.False((await GetResourceAsync(second, resource.Id)).IsFavorite);
        var list = await first.Client.GetFromJsonAsync<PagedResponse<ResourceResponse>>("/api/resources", JsonOptions);
        Assert.True(Assert.Single(list!.Items).IsFavorite);

        Assert.Equal(HttpStatusCode.NoContent, (await first.Client.DeleteAsync($"/api/resources/{resource.Id}/favorite")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await first.Client.DeleteAsync($"/api/resources/{resource.Id}/favorite")).StatusCode);
        Assert.False((await GetResourceAsync(first, resource.Id)).IsFavorite);
    }

    [Fact]
    public async Task MyFavorites_ReturnsOnlyOwnFavoritesNewestFirst()
    {
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);
        var older = await UploadAsync(admin.Client, university.Id, course.Id, title: "Birinci");
        var newer = await UploadAsync(admin.Client, university.Id, course.Id, title: "İkinci");
        await UploadAsync(admin.Client, university.Id, course.Id, title: "Favori değil");
        var user = await CreateUserAsync();
        var other = await CreateUserAsync();

        await user.Client.PutAsync($"/api/resources/{older.Id}/favorite", null);
        await user.Client.PutAsync($"/api/resources/{newer.Id}/favorite", null);
        await other.Client.PutAsync($"/api/resources/{older.Id}/favorite", null);

        var favorites = await user.Client.GetFromJsonAsync<PagedResponse<ResourceResponse>>(
            "/api/users/me/favorites", JsonOptions);
        Assert.Equal(2, favorites!.TotalCount);
        Assert.Equal([newer.Id, older.Id], favorites.Items.Select(item => item.Id));
        Assert.All(favorites.Items, item => Assert.True(item.IsFavorite));
        await AssertProblemAsync(await user.Client.GetAsync("/api/users/me/favorites?pageSize=101"), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Favorite_OnMissingResource_Returns404()
    {
        var user = await CreateUserAsync();

        await AssertProblemAsync(await user.Client.PutAsync("/api/resources/999999/favorite", null), HttpStatusCode.NotFound);
        await AssertProblemAsync(await user.Client.DeleteAsync("/api/resources/999999/favorite"), HttpStatusCode.NotFound);
        await AssertProblemAsync(await CreateClient().PutAsync("/api/resources/1/favorite", null), HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeletingResource_RemovesItsFavorites()
    {
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);
        var resource = await UploadAsync(admin.Client, university.Id, course.Id);
        var user = await CreateUserAsync();
        await user.Client.PutAsync($"/api/resources/{resource.Id}/favorite", null);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.Client.DeleteAsync($"/api/resources/{resource.Id}")).StatusCode);

        await using var db = Factory.CreateDbContext();
        Assert.False(await db.Favorites.AnyAsync());
        var favorites = await user.Client.GetFromJsonAsync<PagedResponse<ResourceResponse>>(
            "/api/users/me/favorites", JsonOptions);
        Assert.Equal(0, favorites!.TotalCount);
    }

    [Fact]
    public async Task RecordView_IncrementsViewCountButReadingDoesNot()
    {
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);
        var resource = await UploadAsync(admin.Client, university.Id, course.Id);
        Assert.Equal(0, resource.ViewCount);

        await admin.Client.GetAsync("/api/resources");
        Assert.Equal(0, (await GetResourceAsync(admin, resource.Id)).ViewCount);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.Client.PostAsync($"/api/resources/{resource.Id}/view", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.Client.PostAsync($"/api/resources/{resource.Id}/view", null)).StatusCode);

        Assert.Equal(2, (await GetResourceAsync(admin, resource.Id)).ViewCount);
        var list = await admin.Client.GetFromJsonAsync<PagedResponse<ResourceResponse>>("/api/resources", JsonOptions);
        Assert.Equal(2, Assert.Single(list!.Items).ViewCount);
    }

    [Fact]
    public async Task RecordView_ReturnsNotFoundForMissingResourceAndRequiresAuth()
    {
        var admin = await CreateAdminAsync();
        await AssertProblemAsync(await admin.Client.PostAsync("/api/resources/999999/view", null), HttpStatusCode.NotFound);

        await AssertProblemAsync(await CreateClient().PostAsync("/api/resources/1/view", null), HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PopularCourses_AreOrderedByViewsThenResourceCount()
    {
        var admin = await CreateAdminAsync();
        var university = await CreateUniversityAsync(admin.Client);
        var quiet = await CreateCourseAsync(admin.Client, university.Id, "SESSIZ1");
        var popular = await CreateCourseAsync(admin.Client, university.Id, "POPULER1");
        var busy = await CreateCourseAsync(admin.Client, university.Id, "KALABALIK1");
        await CreateCourseAsync(admin.Client, university.Id, "BOS1");

        await UploadAsync(admin.Client, university.Id, quiet.Id);
        var viewed = await UploadAsync(admin.Client, university.Id, popular.Id);
        await UploadAsync(admin.Client, university.Id, busy.Id);
        await UploadAsync(admin.Client, university.Id, busy.Id);
        for (var view = 0; view < 3; view++)
            await admin.Client.PostAsync($"/api/resources/{viewed.Id}/view", null);

        var result = (await admin.Client.GetFromJsonAsync<List<PopularCourseResponse>>(
            "/api/courses/popular?take=3", JsonOptions))!;

        Assert.Equal(["POPULER1", "KALABALIK1", "SESSIZ1"], result.Select(course => course.Code));
        Assert.Equal((1, 3), (result[0].ResourceCount, result[0].ViewCount));
        Assert.Equal((2, 0), (result[1].ResourceCount, result[1].ViewCount));
        Assert.Equal(university.Name, result[0].UniversityName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public async Task PopularCourses_RejectsInvalidTake(int take)
    {
        var user = await CreateUserAsync();

        await AssertValidationErrorAsync(
            await user.Client.GetAsync($"/api/courses/popular?take={take}"),
            "Gösterilecek ders sayısı 1 ile 20 arasında olmalıdır.");
    }

    [Fact]
    public async Task Universities_IncludeResourceCounts()
    {
        var admin = await CreateAdminAsync();
        var (busy, course) = await CreateCatalogAsync(admin);
        var empty = await CreateUniversityAsync(admin.Client);
        await UploadAsync(admin.Client, busy.Id, course.Id);
        await UploadAsync(admin.Client, busy.Id, course.Id);

        var list = await admin.Client.GetFromJsonAsync<PagedResponse<UniversityResponse>>(
            "/api/universities", JsonOptions);
        var single = await admin.Client.GetFromJsonAsync<UniversityResponse>(
            $"/api/universities/{busy.Id}", JsonOptions);

        Assert.Equal(2, list!.Items.Single(u => u.Id == busy.Id).ResourceCount);
        Assert.Equal(0, list.Items.Single(u => u.Id == empty.Id).ResourceCount);
        Assert.Equal(2, single!.ResourceCount);
    }

    private static async Task<ResourceResponse> GetResourceAsync(TestUser user, int id) =>
        (await user.Client.GetFromJsonAsync<ResourceResponse>($"/api/resources/{id}", JsonOptions))!;
}
