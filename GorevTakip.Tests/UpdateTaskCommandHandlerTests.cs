using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using GorevTakip.Business.Features.Tasks.Commands;
using GorevTakip.DataAccess.Repositories;
using GorevTakip.Entities;
using GorevTakip.Entities.DTOs;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using Xunit;
using GorevTakip.Business.Exceptions;

namespace GorevTakip.Tests
{
    public class UpdateTaskCommandHandlerTests
    {
        private readonly Mock<ITaskRepository> _taskRepositoryMock;
        private readonly Mock<IGenericRepository<User>> _userRepositoryMock;
        private readonly Mock<ITaskHistoryRepository> _historyRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IDistributedCache> _cacheMock;

        public UpdateTaskCommandHandlerTests()
        {
            _taskRepositoryMock = new Mock<ITaskRepository>();
            _userRepositoryMock = new Mock<IGenericRepository<User>>();
            _historyRepositoryMock = new Mock<ITaskHistoryRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _cacheMock = new Mock<IDistributedCache>();
        }

        [Fact]
        public async Task Handle_TaskDoesNotExist_ThrowsException()
        {
            // Arrange
            var command = new UpdateTaskCommand
            {
                TaskDto = new TaskUpdateDto { Id = 1, AssignedUserId = 1 }
            };

            _taskRepositoryMock.Setup(repo => repo.GetByIdAsync(1))
                .ReturnsAsync((TaskItem?)null);

            var handler = new UpdateTaskCommandHandler(
                _taskRepositoryMock.Object,
                _userRepositoryMock.Object,
                _historyRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _cacheMock.Object
            );

            // Act & Assert
            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>().WithMessage("Güncellenecek görev bulunamadı.");
        }

        [Fact]
        public async Task Handle_AssignedUserDoesNotExist_ThrowsException()
        {
            // Arrange
            var command = new UpdateTaskCommand
            {
                TaskDto = new TaskUpdateDto { Id = 1, AssignedUserId = 99 }
            };

            var existingTask = new TaskItem { Id = 1, AssignedUserId = 1, Category = TaskCategory.Backend };
            
            _taskRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(existingTask);
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(99)).ReturnsAsync((User?)null);

            var handler = new UpdateTaskCommandHandler(
                _taskRepositoryMock.Object,
                _userRepositoryMock.Object,
                _historyRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _cacheMock.Object
            );

            // Act & Assert
            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>().WithMessage("Atanan kullanıcı bulunamadı!");
        }

        [Fact]
        public async Task Handle_ValidRequest_UpdatesTaskAndInvalidatesCache()
        {
            // Arrange
            var taskDto = new TaskUpdateDto 
            { 
                Id = 1, 
                AssignedUserId = 2, 
                Title = "Updated Title", 
                Description = "Updated Desc",
                Category = TaskCategory.Frontend,
                Status = WorkStatus.InProgress
            };
            
            var command = new UpdateTaskCommand { TaskDto = taskDto };

            var existingTask = new TaskItem { Id = 1, AssignedUserId = 1, Category = TaskCategory.Backend };
            var newUser = new User { Id = 2 };

            _taskRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(existingTask);
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(2)).ReturnsAsync(newUser);

            var handler = new UpdateTaskCommandHandler(
                _taskRepositoryMock.Object,
                _userRepositoryMock.Object,
                _historyRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _cacheMock.Object
            );

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            existingTask.Title.Should().Be("Updated Title");
            existingTask.Description.Should().Be("Updated Desc");
            existingTask.AssignedUserId.Should().Be(2);
            existingTask.Category.Should().Be(TaskCategory.Frontend);
            existingTask.Status.Should().Be(WorkStatus.InProgress);

            _taskRepositoryMock.Verify(r => r.Update(existingTask), Times.Once);
            _historyRepositoryMock.Verify(r => r.AddAsync(It.IsAny<TaskHistory>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
            _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), default), Times.AtLeastOnce);
        }
    }
}
