﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Xunit;
using Shouldly;
using Ploeh.AutoFixture;
using NSubstitute;


namespace Elephanet.Tests
{
    public class LinqTests : IDisposable
    {
        private IDocumentStore _store;

        public LinqTests()
        {
            _store = new TestStore(); 
            CreateDummyCars();
        }

        public void CreateDummyCars()
        {

            var dummyCars = new Fixture().Build<Car>()
               .With(x => x.Make, "Subaru")
               .CreateMany();

            var lowerCars = new Fixture().Build<Car>()
                .With(x => x.Make, "SAAB")
                .CreateMany();

            using (var session = _store.OpenSession())
            {
                foreach (var car in dummyCars)
                {
                    session.Store<Car>(car);
                }

                foreach (var car in lowerCars)
                {
                    session.Store<Car>(car);
                }
                session.SaveChanges();
            }

        }

        [Fact]
        public void WherePredicate_Should_Build()
        {
            using (var session = _store.OpenSession())
            {
                var results = session.Query<Car>().Where(x => x.Make == "Subaru").ToList();
                results.ShouldNotBeEmpty();
            }
        }

        [Fact]
        public void WhereExpression_Should_ReturnWhereQuery()
        {
            using (var session = _store.OpenSession())
            {
                var results = session.Query<Car>().Where(c => c.Make == "Subaru");
                var cars = results.ToList();
                cars.Count.ShouldBe(3);
                cars.ShouldBeOfType<List<Car>>();
                cars.ForEach(c => c.Make.ShouldBe("Subaru"));
              
            }
        }

        [Fact]
        public void WhereExpression_Should_HandleExpressionSubtrees()
        {
            string make = "Subaru";
            using (var session = _store.OpenSession())
            {
                var results = session.Query<Car>().Where(c => c.Make == make);
                var cars = results.ToList();
                cars.Count.ShouldBe(3);
                cars.ShouldBeOfType<List<Car>>();
                cars.ForEach(c => c.Make.ShouldBe("Subaru"));
            }

        }

        [Fact]
        public void WhereExpression_Should_HandleExtensionMethodsInSubtrees()
        {
            using (var session = _store.OpenSession())
            {
                var results = session.Query<Car>().Where(c => c.Make == "saab".ToUpper());
                var cars = results.ToList();
                cars.Count.ShouldBe(3);
                cars.ShouldBeOfType<List<Car>>();
                cars.ForEach(c => c.Make.ShouldBe("SAAB"));
            }

        }

        [Fact]
        public void IQueryable_Should_ImplementTakeMethod()
        {
            using (var session = _store.OpenSession())
            {
                var results = session.Query<Car>().Where(c => c.Make == "SAAB").Take(2);
                var cars = results.ToList();
                cars.Count.ShouldBe(2);
                cars.ShouldBeOfType<List<Car>>();
            }
        }

        [Fact]
        public void IQueryable_Should_ImplementSkipMethod()
        {
            using (var session = _store.OpenSession())
            {
                var results = session.Query<Car>().Where(c => c.Make == "SAAB").Skip(2);
                var cars = results.ToList();
                cars.Count.ShouldBe(1);
                cars.ShouldBeOfType<List<Car>>();
            }

        }

        public void Dispose()
        {
            using (var session = _store.OpenSession())
            {
                session.DeleteAll<Car>();
            }
        }
    }
}
