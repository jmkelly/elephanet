using Elephanet.Tests.Entities;
using Xunit;
using Shouldly;

namespace Elephanet.Tests
{
    public class ConventionTests
    {
        [Fact]
        public void TableInfo_GivenNoSchema_ShouldReturnSchemaOfPublic()
        {
            var tableInfo = new TableInfo();
            tableInfo.Schema.ShouldBe("public");
        }

        [Fact]
        public void TableInfo_GivenASchema_ShouldReturnThatSchema()
        {
            var tableInfo = new TableInfo("aschema");
            tableInfo.Schema.ShouldBe("aschema");
        }

        [Fact]
        public void TableInfo_GivenAType_ShouldReturn_TheCorrectTableNameWithoutSchema()
        {
            var tableInfo = new TableInfo("aschema");
            tableInfo.TableNameWithoutSchema(typeof(EntityForSchemaConventionsTest)).ShouldBe("elephanet_tests_entities_entityforschemaconventionstest");
 
        }

        [Fact]
        public void TableInfo_GivenAType_ShouldReturn_TheCorrectTableNameWithSchema()
        {
            var tableInfo = new TableInfo("aschema");
            tableInfo.TableNameWithSchema(typeof(EntityForSchemaConventionsTest)).ShouldBe("aschema.elephanet_tests_entities_entityforschemaconventionstest");

        }
    }
}
