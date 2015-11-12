using Elephanet.Tests.Entities;
using Ploeh.AutoFixture;
using Shouldly;
using System.Linq;
using Xunit;
using System.Collections.Generic;

namespace Elephanet.Tests
{
    public class BatchCommanderTests
    {
        int batchSize;
        List<object> updates;

        public BatchCommanderTests()
        {
            updates = new Fixture().Build<EntityForBatchingTests>()
                .CreateMany(10000).ToList<object>();
            batchSize = 1000;
        }

        [Fact]
        public void BatchSize_Should_BeSetCorrectly()
        {
            var commander = new Commander(updates, batchSize );
            commander.BatchSize.ShouldBe(1000);
        }

        [Fact]
        public void CommandBatch_Should_ReturnAListOfCommandsOfBatchSize()
        {
            var commander = new Commander(updates, batchSize );
            var commands = commander.Batch();
            commands[0].ShouldBeOfType<BatchedEntities>();
            commands[0].Entities.Count().ShouldBe(batchSize);
            commands[1].Entities.Count().ShouldBe(batchSize);
            commands[2].Entities.Count().ShouldBe(batchSize);
        }

        [Fact]
        public void CommandBatch_Should_FillIfLessThanBatchSize()
        {
            updates = new Fixture().Build<EntityForBatchingTests>()
             .CreateMany(2).ToList<object>();

            var commander = new Commander(updates, 1000);
            var commands = commander.Batch();
            commands[0].ShouldBeOfType<BatchedEntities>();
            commands[0].Entities.Count().ShouldBe(2);
        }
    }
}
