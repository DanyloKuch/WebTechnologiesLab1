using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using WebTechnologiesLab1.ApiControllers;
using WebTechnologiesLab1.Data;
using WebTechnologiesLab1.Models;
using Xunit;

namespace WebTechnologiesLab1.Tests
{
    public class CategoriesApiControllerTests
    {
        private readonly Mock<WebDbContext> _contextMock;
        private readonly CategoriesApiController _controller;

        public CategoriesApiControllerTests()
        {
            var options = new DbContextOptions<WebDbContext>();
            _contextMock = new Mock<WebDbContext>(options);
            _controller = new CategoriesApiController(_contextMock.Object);
        }

        [Fact]
        public async Task GetCategories_ReturnsAllCategories()
        {
            var categories = new List<Category>
            {
                new Category { Id = 1, Name = "Electronics" },
                new Category { Id = 2, Name = "Books" }
            }.AsQueryable();

            var mockSet = CreateMockDbSet(categories);
            _contextMock.Setup(c => c.Categories).Returns(mockSet.Object);

            var result = await _controller.GetCategories();

            Assert.IsType<ActionResult<IEnumerable<Category>>>(result);

            var returnedCategories = Assert.IsAssignableFrom<IEnumerable<Category>>(result.Value).ToList();
            Assert.Equal(2, returnedCategories.Count);
        }

        [Fact]
        public async Task GetCategory_ValidId_ReturnsCategory()
        {
            var category = new Category { Id = 1, Name = "Laptops" };
            var mockSet = new Mock<DbSet<Category>>();

            mockSet.Setup(m => m.FindAsync(new object[] { 1 })).ReturnsAsync(category);
            _contextMock.Setup(c => c.Categories).Returns(mockSet.Object);

            var result = await _controller.GetCategory(1);

            var actionResult = Assert.IsType<ActionResult<Category>>(result);
            Assert.Equal(1, actionResult.Value.Id);
            Assert.Equal("Laptops", actionResult.Value.Name);
        }

        [Fact]
        public async Task GetCategory_InvalidId_ReturnsNotFound()
        {
            var mockSet = new Mock<DbSet<Category>>();
            mockSet.Setup(m => m.FindAsync(new object[] { 99 })).ReturnsAsync((Category)null);
            _contextMock.Setup(c => c.Categories).Returns(mockSet.Object);

            var result = await _controller.GetCategory(99);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task CreateCategory_ValidCategory_ReturnsCreatedAtAction()
        {
            var newCategory = new Category { Id = 3, Name = "Furniture" };
            var mockSet = new Mock<DbSet<Category>>();
            _contextMock.Setup(c => c.Categories).Returns(mockSet.Object);

            var result = await _controller.CreateCategory(newCategory);

            var createdAtResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returnedCategory = Assert.IsType<Category>(createdAtResult.Value);

            Assert.Equal("Furniture", returnedCategory.Name);

            mockSet.Verify(m => m.Add(It.IsAny<Category>()), Times.Once);
            _contextMock.Verify(m => m.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task DeleteCategory_ValidId_ReturnsNoContent()
        {
            var category = new Category { Id = 1, Name = "ToDelete" };
            var mockSet = new Mock<DbSet<Category>>();
            mockSet.Setup(m => m.FindAsync(new object[] { 1 })).ReturnsAsync(category);
            _contextMock.Setup(c => c.Categories).Returns(mockSet.Object);

            var result = await _controller.DeleteCategory(1);

            Assert.IsType<NoContentResult>(result);

            mockSet.Verify(m => m.Remove(category), Times.Once);
            _contextMock.Verify(m => m.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task UpdateCategory_IdMismatch_ReturnsBadRequest()
        {
            var category = new Category { Id = 2, Name = "Mismatch" };

            var result = await _controller.UpdateCategory(1, category);

            Assert.IsType<BadRequestResult>(result);
        }

        #region Допоміжні класи для локального мокання IAsyncEnumerable (ToListAsync)

        private static Mock<DbSet<T>> CreateMockDbSet<T>(IQueryable<T> data) where T : class
        {
            var mockSet = new Mock<DbSet<T>>();

            mockSet.As<IAsyncEnumerable<T>>()
                .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<T>(data.GetEnumerator()));

            mockSet.As<IQueryable<T>>()
                .Setup(m => m.Provider)
                .Returns(new TestAsyncQueryProvider<T>(data.Provider));

            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => data.GetEnumerator());

            return mockSet;
        }

        private class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
        {
            private readonly IQueryProvider _inner;
            public TestAsyncQueryProvider(IQueryProvider inner) => _inner = inner;
            public IQueryable CreateQuery(Expression expression) => new TestAsyncEnumerable<TEntity>(expression);
            public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new TestAsyncEnumerable<TElement>(expression);
            public object Execute(Expression expression) => _inner.Execute(expression);
            public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);
            public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
            {
                var expectedResultType = typeof(TResult).GetGenericArguments()[0];
                var executionResult = typeof(IQueryProvider)
                    .GetMethod(name: nameof(IQueryProvider.Execute), genericParameterCount: 1, types: new[] { typeof(Expression) })
                    .MakeGenericMethod(expectedResultType)
                    .Invoke(this, new[] { expression });
                return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))
                    .MakeGenericMethod(expectedResultType)
                    .Invoke(null, new[] { executionResult });
            }
        }

        private class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
        {
            public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
            public TestAsyncEnumerable(Expression expression) : base(expression) { }
            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
                new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
            IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
        }

        private class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
        {
            private readonly IEnumerator<T> _inner;
            public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;
            public ValueTask DisposeAsync()
            {
                _inner.Dispose();
                return ValueTask.CompletedTask;
            }
            public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(_inner.MoveNext());
            public T Current => _inner.Current;
        }

        #endregion
    }
}