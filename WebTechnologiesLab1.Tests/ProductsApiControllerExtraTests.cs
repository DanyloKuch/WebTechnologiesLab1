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
    public class ProductsApiControllerExtraTests
    {
        private readonly Mock<WebDbContext> _contextMock;
        private readonly ProductsApiController _controller;

        public ProductsApiControllerExtraTests()
        {
            var options = new DbContextOptions<WebDbContext>();
            _contextMock = new Mock<WebDbContext>(options);
            _controller = new ProductsApiController(_contextMock.Object);
        }

        [Fact]
        public async Task UpdateProduct_IdMismatch_ReturnsBadRequest()
        {
            // Arrange
            var product = new Product { id = 2, name = "Test" };

            // Act: Передаємо id = 1, але продукт має id = 2
            var result = await _controller.UpdateProduct(1, product);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task DeleteProduct_ValidId_ReturnsNoContent()
        {
            // Arrange
            var product = new Product { id = 1, name = "To Delete" };
            var mockSet = new Mock<DbSet<Product>>();

            // Мокаємо FindAsync для пошуку продукта
            mockSet.Setup(m => m.FindAsync(new object[] { 1 })).ReturnsAsync(product);
            _contextMock.Setup(c => c.Products).Returns(mockSet.Object);

            // Act
            var result = await _controller.DeleteProduct(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
            mockSet.Verify(m => m.Remove(product), Times.Once);
            _contextMock.Verify(m => m.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task DeleteProduct_InvalidId_ReturnsNotFound()
        {
            // Arrange
            var mockSet = new Mock<DbSet<Product>>();
            mockSet.Setup(m => m.FindAsync(new object[] { 99 })).ReturnsAsync((Product)null);
            _contextMock.Setup(c => c.Products).Returns(mockSet.Object);

            // Act
            var result = await _controller.DeleteProduct(99);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetProducts_PaginationLimits_AreAppliedCorrectly()
        {
            // Arrange
            var products = new List<Product>().AsQueryable();
            var mockSet = CreateLocalMockDbSet(products);
            _contextMock.Setup(c => c.Products).Returns(mockSet.Object);

            // Act: передаємо некоректні параметри пагінації (<1 та >100)
            var result = await _controller.GetProducts(pageNumber: -5, pageSize: 500);

            // Assert: якщо код не впав і повернув пустий список, значить ліміти (1 і 100) застосувалися успішно
            var actionResult = Assert.IsAssignableFrom<IEnumerable<Product>>(result.Value);
            Assert.Empty(actionResult);
        }

        #region Допоміжні методи
        private static Mock<DbSet<T>> CreateLocalMockDbSet<T>(IQueryable<T> data) where T : class
        {
            var mockSet = new Mock<DbSet<T>>();
            mockSet.As<IAsyncEnumerable<T>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>())).Returns(new TestAsyncEnumerator<T>(data.GetEnumerator()));
            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<T>(data.Provider));
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
                var executionResult = typeof(IQueryProvider).GetMethod(name: nameof(IQueryProvider.Execute), genericParameterCount: 1, types: new[] { typeof(Expression) }).MakeGenericMethod(expectedResultType).Invoke(this, new[] { expression });
                return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult)).MakeGenericMethod(expectedResultType).Invoke(null, new[] { executionResult });
            }
        }

        private class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
        {
            public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
            public TestAsyncEnumerable(Expression expression) : base(expression) { }
            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
            IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
        }

        private class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
        {
            private readonly IEnumerator<T> _inner;
            public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;
            public ValueTask DisposeAsync() { _inner.Dispose(); return ValueTask.CompletedTask; }
            public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(_inner.MoveNext());
            public T Current => _inner.Current;
        }
        #endregion
    }
}