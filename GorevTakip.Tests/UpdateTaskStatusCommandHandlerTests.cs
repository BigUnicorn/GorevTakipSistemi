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

namespace GorevTakip.Tests
{
    public class UpdateTaskStatusCommandHandlerTests
    {
        private readonly Mock<ITaskRepository> _taskRepositoryMock;
        private readonly Mock<ITaskHistoryRepository> _historyRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IDistributedCache> _cacheMock;
        private readonly Mock<IOutboxRepository> _outboxRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;

        public UpdateTaskStatusCommandHandlerTests()
        {
            _taskRepositoryMock = new Mock<ITaskRepository>();
            _historyRepositoryMock = new Mock<ITaskHistoryRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _cacheMock = new Mock<IDistributedCache>();
            _outboxRepositoryMock = new Mock<IOutboxRepository>();
            _mapperMock = new Mock<IMapper>();
        }

        [Fact]
        public async Task Handle_TaskDoesNotExist_ThrowsException()
        {
            // Arrange
            var command = new UpdateTaskStatusCommand { Id = 1, NewStatus = WorkStatus.Done };

            _taskRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync((TaskItem?)null);

            var handler = new UpdateTaskStatusCommandHandler(
                _taskRepositoryMock.Object,
                _historyRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _cacheMock.Object,
                _outboxRepositoryMock.Object,
                _mapperMock.Object
            );

            // Act & Assert
            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>().WithMessage("Görev bulunamadı.");
        }

        [Fact]
        public async Task Handle_TaskExists_UpdatesStatusAndInvalidatesCache()
        {
            // Arrange
            var command = new UpdateTaskStatusCommand { Id = 1, NewStatus = WorkStatus.Done };
            var existingTask = new TaskItem { Id = 1, AssignedUserId = 1, Category = TaskCategory.Backend, Status = WorkStatus.Todo };

            _taskRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(existingTask);

            var handler = new UpdateTaskStatusCommandHandler(
                _taskRepositoryMock.Object,
                _historyRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _cacheMock.Object,
                _outboxRepositoryMock.Object,
                _mapperMock.Object
            );

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            existingTask.Status.Should().Be(WorkStatus.Done);
            _taskRepositoryMock.Verify(r => r.Update(existingTask), Times.Once);
            _historyRepositoryMock.Verify(r => r.AddAsync(It.IsAny<TaskHistory>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
            _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), default), Times.AtLeastOnce);
        }
    }
}
