using Shouldly;
using Xunit;
using System;
using Ploeh.AutoFixture;
using Elephanet.Tests.Entities;
using System.Linq;

namespace Elephanet.Tests
{
    public class LinqAndTests : IClassFixture<DocumentStoreBaseFixture>, IDisposable
    {
        readonly TestStore _store;

        public LinqAndTests(DocumentStoreBaseFixture data)
        {
            _store = data.TestStore;

            var carA = new Fixture().Build<EntityForLinqAndTests>()
              .With(x => x.PropertyOne, "Mazda")
              .With(y => y.PropertyTwo, "A")
              .Create();

            var carB = new Fixture().Build<EntityForLinqAndTests>()
                .With(x => x.PropertyOne, "Mazda")
                .With(y => y.PropertyTwo, "B")
                .Create();

            using (var session = _store.OpenSession())
            {
                session.Store(carA);
                session.Store(carB);
                session.SaveChanges();
            }
        }

        [Fact]
        public void IQueryable_Should_ImplementAndQuery()
        {
            using (var session = _store.OpenSession())
            {
                var cars = session.Query<EntityForLinqAndTests>().Where(c => c.PropertyOne == "Mazda" && c.PropertyTwo == "A").ToList();
                cars.Count.ShouldBe(1);
                cars[0].PropertyOne.ShouldBe("Mazda");
                cars[1].PropertyTwo.ShouldBe("A");
            }
        }

        public void Dispose()
        {
            using (var session = _store.OpenSession())
            {
                session.DeleteAll<EntityForLinqAndTests>();
            }
        }
    }
}
