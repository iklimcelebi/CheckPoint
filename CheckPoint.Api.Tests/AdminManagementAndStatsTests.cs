using System.Net;
using System.Net.Http.Json;
using CheckPoint.Api.Dtos;
using CheckPoint.Api.Models;
using CheckPoint.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CheckPoint.Api.Tests;

public sealed class AdminManagementAndStatsTests : ApiTestBase
{
    [Fact]
    public async Task MyStats_CountsOwnResourcesViewsAndFavorites()
    {
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);
        var owner = await CreateUserAsync();
        var other = await CreateUserAsync();

        var popular = await UploadAsync(owner.Client, university.Id, course.Id, title: "Popüler not");
        var exam = await UploadAsync(owner.Client, university.Id, course.Id, title: "Sınav", type: "PastExamQuestion");
        await UploadAsync(owner.Client, university.Id, course.Id, title: "Görülmemiş");
        var othersResource = await UploadAsync(other.Client, university.Id, course.Id);

        for (var view = 0; view < 3; view++)
            await other.Client.PostAsync($"/api/resources/{popular.Id}/view", null);
        await other.Client.PostAsync($"/api/resources/{exam.Id}/view", null);
        await other.Client.PostAsync($"/api/resources/{othersResource.Id}/view", null);
        await other.Client.PutAsync($"/api/resources/{popular.Id}/favorite", null);
        await admin.Client.PutAsync($"/api/resources/{popular.Id}/favorite", null);
        await owner.Client.PutAsync($"/api/resources/{othersResource.Id}/favorite", null);

        var stats = (await owner.Client.GetFromJsonAsync<UserStatsResponse>("/api/users/me/stats", JsonOptions))!;

        Assert.Equal((3, 4, 2, 1), (stats.ResourceCount, stats.TotalViews, stats.FavoritesReceived, stats.FavoriteCount));
        Assert.Equal(
            [(ResourceType.LectureNote, 2), (ResourceType.PastExamQuestion, 1)],
            stats.ResourcesByType.Select(group => (group.Type, group.Count)));
        Assert.Equal(["Popüler not", "Sınav"], stats.TopResources.Select(resource => resource.Title));
    }

    [Fact]
    public async Task MyStats_ForNewUser_IsEmpty()
    {
        var user = await CreateUserAsync();
        var stats = (await user.Client.GetFromJsonAsync<UserStatsResponse>("/api/users/me/stats", JsonOptions))!;

        Assert.Equal((0, 0, 0, 0), (stats.ResourceCount, stats.TotalViews, stats.FavoritesReceived, stats.FavoriteCount));
        Assert.Empty(stats.ResourcesByType);
        Assert.Empty(stats.TopResources);
    }

    [Fact]
    public async Task ListUsers_IsAdminOnlyAndSearchable()
    {
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);
        var user = await CreateUserAsync("Şebnem");
        await CreateUserAsync("baska");
        await UploadAsync(user.Client, university.Id, course.Id);

        await AssertProblemAsync(await user.Client.GetAsync("/api/users"), HttpStatusCode.Forbidden);

        var all = (await admin.Client.GetFromJsonAsync<PagedResponse<AdminUserResponse>>("/api/users", JsonOptions))!;
        Assert.Equal(3, all.TotalCount);

        var found = (await admin.Client.GetFromJsonAsync<PagedResponse<AdminUserResponse>>(
            "/api/users?q=sebnem", JsonOptions))!;
        var single = Assert.Single(found.Items);
        Assert.Equal(("Şebnem", "User", 1), (single.Username, single.Role, single.ResourceCount));
    }

    [Fact]
    public async Task DeleteUser_RemovesAccountResourcesFilesAndFavorites()
    {
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);
        var target = await CreateUserAsync();
        var bystander = await CreateUserAsync();
        var targetResource = await UploadAsync(target.Client, university.Id, course.Id);
        var bystanderResource = await UploadAsync(bystander.Client, university.Id, course.Id);
        await bystander.Client.PutAsync($"/api/resources/{targetResource.Id}/favorite", null);
        await target.Client.PutAsync($"/api/resources/{bystanderResource.Id}/favorite", null);
        Assert.Equal(2, StoredFiles().Length);

        var response = await admin.Client.DeleteAsync($"/api/users/{target.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using var db = Factory.CreateDbContext();
        Assert.False(await db.Users.AnyAsync(user => user.Id == target.Id));
        Assert.Equal([bystanderResource.Id], await db.EducationalResources.Select(resource => resource.Id).ToListAsync());
        Assert.False(await db.Favorites.AnyAsync());
        Assert.Single(StoredFiles());
        await AssertProblemAsync(await target.Client.GetAsync("/api/users/me"), HttpStatusCode.Unauthorized);
        await AssertProblemAsync(await admin.Client.DeleteAsync($"/api/users/{target.Id}"), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteUser_RejectsSelfAdminsAndRegularUsers()
    {
        var admin = await CreateAdminAsync();
        var user = await CreateUserAsync();
        var other = await CreateUserAsync();

        var self = await AssertProblemAsync(await admin.Client.DeleteAsync($"/api/users/{admin.Id}"), HttpStatusCode.Conflict);
        Assert.Equal("Kendi hesabınızı silemezsiniz.", self.Detail);

        await using (var db = Factory.CreateDbContext())
        {
            var promoted = await db.Users.SingleAsync(candidate => candidate.Id == other.Id);
            promoted.Role = "Admin";
            await db.SaveChangesAsync();
        }

        var otherAdmin = await AssertProblemAsync(await admin.Client.DeleteAsync($"/api/users/{other.Id}"), HttpStatusCode.Conflict);
        Assert.Equal("Yönetici hesapları silinemez.", otherAdmin.Detail);

        await AssertProblemAsync(await user.Client.DeleteAsync($"/api/users/{admin.Id}"), HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteCourse_IsAdminOnlyAndBlockedWhenResourcesExist()
    {
        var admin = await CreateAdminAsync();
        var (university, used) = await CreateCatalogAsync(admin);
        var empty = await CreateCourseAsync(admin.Client, university.Id);
        var user = await CreateUserAsync();
        await UploadAsync(user.Client, university.Id, used.Id);

        await AssertProblemAsync(await user.Client.DeleteAsync($"/api/courses/{empty.Id}"), HttpStatusCode.Forbidden);

        var conflict = await AssertProblemAsync(await admin.Client.DeleteAsync($"/api/courses/{used.Id}"), HttpStatusCode.Conflict);
        Assert.Equal("Bu derse bağlı kaynaklar olduğu için silinemez.", conflict.Detail);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.Client.DeleteAsync($"/api/courses/{empty.Id}")).StatusCode);
        await AssertProblemAsync(await admin.Client.GetAsync($"/api/courses/{empty.Id}"), HttpStatusCode.NotFound);
        await AssertProblemAsync(await admin.Client.DeleteAsync($"/api/courses/{empty.Id}"), HttpStatusCode.NotFound);
    }
}
