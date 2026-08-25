using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using GorevTakip.Entities.DTOs;
using GorevTakip.Entities;
using Xunit;

namespace GorevTakip.IntegrationTests;

public class TasksControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TasksControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        // Custom Auth scheme'i kullanması için header ekliyoruz
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("TestScheme");
    }

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
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var createdTask = await response.Content.ReadFromJsonAsync<TaskResponseDto>();
        createdTask.Should().NotBeNull();
        createdTask!.Title.Should().Be(newTask.Title);
        createdTask.Description.Should().Be(newTask.Description);
    }
}
