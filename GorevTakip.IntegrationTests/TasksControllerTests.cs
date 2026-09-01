using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using GorevTakip.Entities.DTOs;
using GorevTakip.Entities;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using GorevTakip.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace GorevTakip.IntegrationTests;

public class TasksControllerTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public TasksControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        // Custom Auth scheme'i kullanması için header ekliyoruz
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("TestScheme");
    }

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Tasks.RemoveRange(await db.Tasks.IgnoreQueryFilters().ToListAsync());
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetTasks_ShouldReturnSuccess_AndEmptyList_WhenNoTasksExist()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/Tasks");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var returnedTasks = await response.Content.ReadFromJsonAsync<PagedResponseDto<TaskResponseDto>>();
        returnedTasks.Should().NotBeNull();
        returnedTasks!.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateTask_ShouldReturnCreated_AndTaskDetails()
    {
        // Arrange
        var newTask = new TaskCreateDto
        {
            Title = "Integration Test Task",
            Description = "This task was created by an integration test",
            AssignedUserId = 1,
            Category = TaskCategory.Backend
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/Tasks", newTask);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var message = await response.Content.ReadAsStringAsync();
        message.Should().Be("Görev başarıyla oluşturuldu.");
    }
}
