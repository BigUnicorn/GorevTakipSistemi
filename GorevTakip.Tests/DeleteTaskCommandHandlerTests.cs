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

namespace GorevTakip.Tests
{
    public class DeleteTaskCommandHandlerTests
    {
        private readonly Mock<ITaskRepository> _taskRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IDistributedCache> _cacheMock;

        public DeleteTaskCommandHandlerTests()
        {
            _taskRepositoryMock = new Mock<ITaskRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _cacheMock = new Mock<IDistributedCache>();
        }

        [Fact]
        public async Task Handle_TaskExists_DeletesTaskAndInvalidatesCache()
        {
            // Arrange
            var command = new DeleteTaskCommand { Id = 1 };
            var existingTask = new TaskItem { Id = 1, AssignedUserId = 1, Category = TaskCategory.Backend };

            _taskRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(existingTask);

            var handler = new DeleteTaskCommandHandler(
                _taskRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _cacheMock.Object
            );

            // Act
            await handler.Handle(command, CancellationToken.None);

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

            _taskRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync((TaskItem)null);

            var handler = new DeleteTaskCommandHandler(
                _taskRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _cacheMock.Object
            );

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            _taskRepositoryMock.Verify(r => r.Delete(It.IsAny<TaskItem>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
            _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), default), Times.Never);
        }
    }
}
