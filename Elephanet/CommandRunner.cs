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

        HashSet<Tuple<Type, string, string>> MatchEntityToFinalTableAndTemporaryTable(Dictionary<Guid, object> entities)
        {
            var typeToTableMap = new HashSet<Tuple<Type, string, string>>();

            var types = entities.Values.Select(v => v.GetType()).Distinct();
            foreach (Type type in types)
            {
                typeToTableMap.Add(new Tuple<Type, string, string>(type, _tableInfo.TableNameWithSchema(type), Guid.NewGuid().ToString()));
            }

            return typeToTableMap;
        }

        public void Execute()
        {

            var sb = new StringBuilder();

            HashSet<Tuple<Type, string, string>> matches = MatchEntityToFinalTableAndTemporaryTable(_entities);


            foreach (var item in _entities)
            {
                //make sure we have tables for all types
                _schemaGenerator.GetOrCreateTable(item.Value.GetType());
            }

            StringBuilder createTempTable = new StringBuilder();
            StringBuilder dropTempTable = new StringBuilder();

            sb.Append("BEGIN;");
            List<string> temporaryTableName = new List<string>();
            foreach (var match in matches)
            {
                createTempTable.Append(string.Format("CREATE TABLE {0} (id uuid, body jsonb);", match.Item3.SurroundWithDoubleQuotes()));
                dropTempTable.Append(string.Format("DROP TABLE {0};", match.Item3.SurroundWithDoubleQuotes()));
            }

            foreach (var item in _entities)
            {
                sb.Append(string.Format("INSERT INTO {0} (id, body) VALUES ('{1}', '{2}');", matches.Where(c => c.Item1 == item.Value.GetType()).Select(j => j.Item3).First().SurroundWithDoubleQuotes(), item.Key, _jsonConverter.Serialize(item.Value).EscapeQuotes()));
            }

            foreach (var match in matches)
            {
                sb.Append(string.Format("LOCK TABLE {0} IN EXCLUSIVE MODE;", match.Item2));
                sb.Append(string.Format("UPDATE {0} SET body = tmp.body from {1} tmp where tmp.id = {0}.id;", match.Item2, match.Item3.SurroundWithDoubleQuotes()));
                sb.Append(string.Format("INSERT INTO {0} SELECT tmp.id, tmp.body from {1} tmp LEFT OUTER JOIN {0} ON ({0}.id = tmp.id) where {0}.id IS NULL;", match.Item2, match.Item3.SurroundWithDoubleQuotes()));
            }


            sb.Append("COMMIT;");

            using (var command = _connection.CreateCommand())
            {
                command.CommandTimeout = 60;
                command.CommandType = CommandType.Text;
                command.CommandText = createTempTable.ToString();
                command.ExecuteNonQuery();
                command.CommandText = sb.ToString();
                command.ExecuteNonQuery();
                command.CommandText = dropTempTable.ToString();
                command.ExecuteNonQuery();
            }
        }
    }
}
