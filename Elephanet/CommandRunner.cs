using Elephanet.Extensions;
using Elephanet.Serialization;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Elephanet
{
    public class CommandRunner
    {
        readonly NpgsqlConnection _connection;
        readonly Dictionary<Guid, object> _entities;
        readonly ITableInfo _tableInfo;
        ISchemaGenerator _schemaGenerator;
        private IJsonConverter _jsonConverter;

        public CommandRunner(NpgsqlConnection connection, Dictionary<Guid, object> entities,ISchemaGenerator schemaGenerator, ITableInfo tableInfo, IJsonConverter jsonConverter, int batchSize = 500)
        {
            if (jsonConverter == null)
                throw new ArgumentNullException(nameof(jsonConverter));
            if (schemaGenerator == null)
                throw new ArgumentNullException(nameof(schemaGenerator));
          
            if (tableInfo == null)
                throw new ArgumentNullException(nameof(tableInfo));
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));
            if (batchSize >= 5000)
            {
                throw new ArgumentOutOfRangeException(nameof(batchSize), "Maximum batchsize is 5000, reduce in your your store setup");
            }

            _entities = entities;
            _connection = connection;
            _tableInfo = tableInfo;
            _schemaGenerator = schemaGenerator;
            _jsonConverter = jsonConverter;
        }

       

        public void Execute()
        {

            var sb = new StringBuilder();

            HashSet<EntityMap> matches = _schemaGenerator.MatchEntityToFinalTableAndTemporaryTable(_entities);
            List<object> updates = new List<object>();

            foreach (var item in _entities)
            {
                //make sure we have tables for all types
                _schemaGenerator.GetOrCreateTable(item.Value.GetType());
                updates.Add(item.Value);
            }

            var commander = new Commander(updates, 5000);

            sb.Append("BEGIN;");

            foreach (var match in matches)
            {
                sb.Append(string.Format("CREATE TEMPORARY TABLE IF NOT EXISTS {0} (id uuid, body jsonb);", match.TemporaryTableName.SurroundWithDoubleQuotes()));
            }


            foreach (var batch in commander.Batch())
            {
                List<string> temporaryTableName = new List<string>();
               
                foreach (var item in batch.Entities)
                {
                    var IdProperty = "Id";
                    var propertyInfo = item.GetType().GetProperty(IdProperty);
                    var id = propertyInfo.GetValue(item, null);
                    sb.Append(string.Format("INSERT INTO {0} (id, body) VALUES ('{1}', '{2}');", matches.Where(c => c.EntityType == item.GetType()).Select(j => j.TemporaryTableName).First().SurroundWithDoubleQuotes(), id, _jsonConverter.Serialize(item).EscapeQuotes()));
                }

                foreach (var match in matches)
                {
                    sb.Append(string.Format("LOCK TABLE {0} IN EXCLUSIVE MODE;", match.TableName));
                    sb.Append(string.Format("UPDATE {0} SET body = tmp.body from {1} tmp where tmp.id = {0}.id;", match.TableName, match.TemporaryTableName.SurroundWithDoubleQuotes()));
                    sb.Append(string.Format("INSERT INTO {0} SELECT tmp.id, tmp.body from {1} tmp LEFT OUTER JOIN {0} ON ({0}.id = tmp.id) where {0}.id IS NULL;", match.TableName, match.TemporaryTableName.SurroundWithDoubleQuotes()));
                }

                sb.Append("COMMIT;");

                using (var command = _connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText = sb.ToString(); 
                    command.ExecuteNonQuery();
                }
            }
        }
    }

    
}
