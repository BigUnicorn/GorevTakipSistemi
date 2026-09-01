using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GorevTakip.Business.Features.Tasks.Commands;
using GorevTakip.DataAccess.Repositories;
using GorevTakip.Entities;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using Xunit;
using GorevTakip.Business.Exceptions;
using AutoMapper;
using GorevTakip.Business.Services;

namespace GorevTakip.Tests
{
    public class UpdateTaskStatusCommandHandlerTests
    {
        private readonly Mock<ITaskRepository> _taskRepositoryMock;
        private readonly Mock<IGenericRepository<User>> _userRepositoryMock;
        private readonly Mock<ITaskHistoryRepository> _historyRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IDistributedCache> _cacheMock;
        private readonly Mock<IOutboxRepository> _outboxRepositoryMock;
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly UpdateTaskStatusCommandHandler _handler;

        public UpdateTaskStatusCommandHandlerTests()
        {
            _taskRepositoryMock = new Mock<ITaskRepository>();
            _userRepositoryMock = new Mock<IGenericRepository<User>>();
            _historyRepositoryMock = new Mock<ITaskHistoryRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _cacheMock = new Mock<IDistributedCache>();
            _outboxRepositoryMock = new Mock<IOutboxRepository>();
            _notificationServiceMock = new Mock<INotificationService>();
            _mapperMock = new Mock<IMapper>();

            _handler = new UpdateTaskStatusCommandHandler(
                _taskRepositoryMock.Object,
                _userRepositoryMock.Object,
                _historyRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _cacheMock.Object,
                _outboxRepositoryMock.Object,
                _notificationServiceMock.Object,
                _mapperMock.Object
            );
        }

        [Fact]
        public async Task Handle_TaskDoesNotExist_ThrowsException()
        {
            // Arrange
            var command = new UpdateTaskStatusCommand { Id = 1, NewStatus = WorkStatus.Done };

            _taskRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync((TaskItem?)null);

            // Act & Assert
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>().WithMessage("Görev bulunamadı.");
        }

        [Fact]
        public async Task Handle_TaskExists_UpdatesStatusAndInvalidatesCache()
        {
            // Arrange
            var command = new UpdateTaskStatusCommand { Id = 1, NewStatus = WorkStatus.Done };
            var existingTask = new TaskItem { Id = 1, AssignedUserId = 1, Category = TaskCategory.Backend, Status = WorkStatus.Todo };

            _taskRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(existingTask);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            existingTask.Status.Should().Be(WorkStatus.Done);
            _taskRepositoryMock.Verify(r => r.Update(existingTask), Times.Once);
            _historyRepositoryMock.Verify(r => r.AddAsync(It.IsAny<TaskHistory>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
            _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), default), Times.AtLeastOnce);
        }
    }
}
