using System.Collections;
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
    public class ApiControllerTests
    {
        private readonly Mock<WebDbContext> _contextMock;
        private readonly StatisticsApiController _statisticsController;
        private readonly ProductsApiController _productsController;

        // SETUP у конструкторі (mock DbContext)
        public ApiControllerTests()
        {
            var options = new DbContextOptions<WebDbContext>();
            _contextMock = new Mock<WebDbContext>(options);

            _statisticsController = new StatisticsApiController(_contextMock.Object);
            _productsController = new ProductsApiController(_contextMock.Object);
        }

        // 1. Assert на рядок (статистика текстом). Тестуємо StatisticsApiController
        [Fact]
        public async Task GetProductSalesData_NoSales_ReturnsNotFoundString()
        {
            // Arrange
            var emptyData = new List<OrderItem>().AsQueryable();
            var mockSet = CreateMockDbSet(emptyData);
            _contextMock.Setup(c => c.OrderItems).Returns(mockSet.Object);

            // Act
            var result = await _statisticsController.GetProductSalesData();

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Немає даних про замовлення.", notFoundResult.Value);
        }

        // 2. Параметризований тест з [MemberData]. Тестуємо ProductsApiController.GetProduct
        [Theory]
        [MemberData(nameof(GetProductTestData))]
        public async Task GetProduct_VariousIds_ReturnsExpectedResult(int productId, bool expectedFound)
        {
            // Arrange
            var products = new List<Product>
            {
                new Product { id = 1, name = "Apple", price = 10 },
                new Product { id = 2, name = "Banana", price = 20 }
            }.AsQueryable();

            var mockSet = CreateMockDbSet(products);
            _contextMock.Setup(c => c.Products).Returns(mockSet.Object);

            // Act
            var result = await _productsController.GetProduct(productId);

            // Assert
            if (expectedFound)
            {
                // Для знайденого продукту перевіряємо, що ID збігається
                Assert.Equal(productId, result.Value.id);
            }
            else
            {
                // Для неіснуючого продукту перевіряємо, що повернувся статус 404
                Assert.IsType<NotFoundResult>(result.Result);
            }
        }

        // Статичний метод для [MemberData]
        public static IEnumerable<object[]> GetProductTestData()
        {
            yield return new object[] { 1, true };   // Продукт існує
            yield return new object[] { 99, false }; // Продукту немає
        }

        // 3. Параметризований тест з [ClassData]. Тестуємо ProductsApiController.CreateProduct
        [Theory]
        [ClassData(typeof(ProductTestData))]
        public async Task CreateProduct_ValidProduct_ReturnsCreatedAtAction(Product newProduct)
        {
            // Arrange
            var mockSet = new Mock<DbSet<Product>>();
            _contextMock.Setup(c => c.Products).Returns(mockSet.Object);

            // Act
            var result = await _productsController.CreateProduct(newProduct);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returnedProduct = Assert.IsType<Product>(createdAtActionResult.Value);

            Assert.Equal(newProduct.name, returnedProduct.name);
            mockSet.Verify(m => m.Add(It.IsAny<Product>()), Times.Once); // Перевіряємо виклик Add
            _contextMock.Verify(m => m.SaveChangesAsync(default), Times.Once); // Перевіряємо виклик SaveChanges
        }

        // 4. Складний assert для колекцій (Assert.Collection). Тестуємо ProductsApiController.GetProducts
        [Fact]
        public async Task GetProducts_ReturnsExpectedCollection()
        {
            // Arrange
            var products = new List<Product>
            {
                new Product { id = 1, name = "Laptop", price = 1500 },
                new Product { id = 2, name = "Mouse", price = 25 }
            }.AsQueryable();

            var mockSet = CreateMockDbSet(products);
            _contextMock.Setup(c => c.Products).Returns(mockSet.Object);

            // Act
            var result = await _productsController.GetProducts(1, 10);
            var items = result.Value.ToList();

            // Assert: Складний ассерт для колекцій
            Assert.Collection(items,
                item =>
                {
                    Assert.Equal(1, item.id);
                    Assert.Equal("Laptop", item.name);
                },
                item =>
                {
                    Assert.Equal(2, item.id);
                    Assert.Equal("Mouse", item.name);
                }
            );
        }

        #region Допоміжні класи для мокання Entity Framework Core Async

        // Метод для створення асинхронного Mock DbSet
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

        internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
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

        internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
        {
            public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
            public TestAsyncEnumerable(Expression expression) : base(expression) { }
            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
                new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
            IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
        }

        internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
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
        // 5. Динамічний пропуск на основі [Theory] (якщо параметр == 0 - Skip)
        [Theory]
        [InlineData(1)]
        [InlineData(0)]
        public async Task GetProduct_DynamicSkip_IfIdIsZero(int productId)
        {
            // Якщо параметр дорівнює 0 — динамічно пропускаємо тест
            if (productId == 0)
            {
                Assert.Skip("Тест пропущено: параметр productId дорівнює 0.");
            }

            // Arrange (налаштування)
            var products = new List<Product> { new Product { id = 1, name = "Test" } }.AsQueryable();
            var mockSet = CreateMockDbSet(products);
            _contextMock.Setup(c => c.Products).Returns(mockSet.Object);

            // Act
            var result = await _productsController.GetProduct(productId);

            // Assert
            Assert.IsType<ActionResult<Product>>(result);
        }

        #endregion
    }

    // Окремий DataClass для [ClassData] тесту
    public class ProductTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { new Product { id = 10, name = "Keyboard", price = 50 } };
            yield return new object[] { new Product { id = 11, name = "Monitor", price = 300 } };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

}