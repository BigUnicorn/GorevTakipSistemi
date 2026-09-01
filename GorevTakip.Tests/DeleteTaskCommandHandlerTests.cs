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
using GorevTakip.Business.Services;

namespace GorevTakip.Tests
{
    public class DeleteTaskCommandHandlerTests
    {
        private readonly Mock<ITaskRepository> _taskRepositoryMock;
        private readonly Mock<IGenericRepository<User>> _userRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IDistributedCache> _cacheMock;
        private readonly Mock<IOutboxRepository> _outboxRepositoryMock;
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly DeleteTaskCommandHandler _handler;

        public DeleteTaskCommandHandlerTests()
        {
            _taskRepositoryMock = new Mock<ITaskRepository>();
            _userRepositoryMock = new Mock<IGenericRepository<User>>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _cacheMock = new Mock<IDistributedCache>();
            _outboxRepositoryMock = new Mock<IOutboxRepository>();
            _notificationServiceMock = new Mock<INotificationService>();

            _handler = new DeleteTaskCommandHandler(
                _taskRepositoryMock.Object,
                _userRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _cacheMock.Object,
                _outboxRepositoryMock.Object,
                _notificationServiceMock.Object
            );
        }

        [Fact]
        public async Task Handle_TaskExists_DeletesTaskAndInvalidatesCache()
        {
            // Arrange
            var command = new DeleteTaskCommand { Id = 1 };
            var existingTask = new TaskItem { Id = 1, AssignedUserId = 1, Category = TaskCategory.Backend };

            _taskRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(existingTask);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _taskRepositoryMock.Verify(r => r.Delete(existingTask), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
            _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), default), Times.AtLeastOnce);
        }

        [Fact]
        public async Task Handle_TaskDoesNotExist_DoesNothing()
        {
            // Arrange
            var command = new DeleteTaskCommand { Id = 1 };

            _taskRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync((TaskItem?)null);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _taskRepositoryMock.Verify(r => r.Delete(It.IsAny<TaskItem>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
            _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), default), Times.Never);
        }
    }
}
