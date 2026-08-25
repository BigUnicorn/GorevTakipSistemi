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
    public class CreateTaskCommandHandlerTests
    {
        private readonly Mock<ITaskRepository> _taskRepositoryMock;
        private readonly Mock<IGenericRepository<User>> _userRepositoryMock;
        private readonly Mock<ITaskHistoryRepository> _historyRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IDistributedCache> _cacheMock;

        public CreateTaskCommandHandlerTests()
        {
            _taskRepositoryMock = new Mock<ITaskRepository>();
            _userRepositoryMock = new Mock<IGenericRepository<User>>();
            _historyRepositoryMock = new Mock<ITaskHistoryRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();
            _cacheMock = new Mock<IDistributedCache>();
        }

        [Fact]
        public async Task Handle_AssignedUserDoesNotExist_ThrowsException()
        {
            // Arrange
            var command = new CreateTaskCommand
            {
                TaskDto = new TaskCreateDto { AssignedUserId = 99 }
            };

            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(99))
                .ReturnsAsync((User?)null); // Kullanıcı yok

            var handler = new CreateTaskCommandHandler(
                _taskRepositoryMock.Object,
                _userRepositoryMock.Object,
                _historyRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _mapperMock.Object,
                _cacheMock.Object
            );

            // Act & Assert
            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>().WithMessage("Atanan kullanıcı bulunamadı!");
        }

        [Fact]
        public async Task Handle_ValidRequest_CreatesTaskAndReturnsDto()
        {
            // Arrange
            var taskDto = new TaskCreateDto { AssignedUserId = 1, Title = "Test Task" };
            var command = new CreateTaskCommand { TaskDto = taskDto };
            
            var user = new User { Id = 1, FirstName = "Test" };
            var taskItem = new TaskItem { Id = 1, Title = "Test Task", Category = TaskCategory.Backend };
            var taskResponse = new TaskResponseDto { Id = 1, Title = "Test Task" };

            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(user);
            _mapperMock.Setup(m => m.Map<TaskItem>(taskDto)).Returns(taskItem);
            _mapperMock.Setup(m => m.Map<TaskResponseDto>(taskItem)).Returns(taskResponse);

            var handler = new CreateTaskCommandHandler(
                _taskRepositoryMock.Object,
                _userRepositoryMock.Object,
                _historyRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _mapperMock.Object,
                _cacheMock.Object
            );

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be("Test Task");

            _taskRepositoryMock.Verify(r => r.AddAsync(taskItem), Times.Once);
            _historyRepositoryMock.Verify(r => r.AddAsync(It.IsAny<TaskHistory>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
            _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), default), Times.AtLeastOnce);
        }
    }
}
