using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Elephanet
{
    

    public class SchemaGenerator : ISchemaGenerator
    {
        NpgsqlConnection _connection;
        IStoreInfo _storeInfo;
        ITableInfo _tableInfo;

        public SchemaGenerator(IDocumentStore store, NpgsqlConnection connection)
        {
            _connection = connection; 
            _storeInfo = store.StoreInfo;
            _tableInfo = store.Conventions.TableInfo;
            _connection = connection;
        }

        bool IndexDoesNotExist(Type type)
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandType = CommandType.Text;
                command.CommandText = string.Format(@"select count(*)
                from pg_indexes
                where schemaname = '{0}'
                and tablename = '{1}'
                and indexname = 'idx_{1}_body';", _tableInfo.Schema, _tableInfo.TableNameWithoutSchema(type));
                var indexCount = (Int64)command.ExecuteScalar();
                return indexCount == 0;
            }

        }

        public HashSet<EntityMap> MatchEntityToFinalTableAndTemporaryTable(Dictionary<Guid, object> entities)
        {
            var typeToTableMap = new HashSet<EntityMap>();

            var types = entities.Values.Select(v => v.GetType()).Distinct();
            foreach (Type type in types)
            {
                typeToTableMap.Add(new EntityMap(type, _tableInfo.TableNameWithSchema(type), Guid.NewGuid().ToString()));
            }

            return typeToTableMap;
        }

        void CreateIndex(Type type)
        {
            if (IndexDoesNotExist(type))
            {
                using (var command = _connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText = string.Format(@"CREATE INDEX idx_{0}_body ON {0} USING gin (body);", _tableInfo.TableNameWithoutSchema(type));
                    command.ExecuteNonQuery();
                }
            }
        }

        public void GetOrCreateTable(Type type)
        {
           
            if (!_storeInfo.Tables.Contains(_tableInfo.TableNameWithSchema(type)))
            {
                _storeInfo.Tables.Add(_tableInfo.TableNameWithSchema(type));
                try
                {
                    using (var command = _connection.CreateCommand())
                    {
                        command.CommandType = CommandType.Text;
                        command.CommandText = string.Format(@"
                            CREATE TABLE IF NOT EXISTS {0}
                            (
                                id uuid NOT NULL, 
                                body jsonb NOT NULL, 
                                created timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                                CONSTRAINT pk_{1} PRIMARY KEY (id)
                            );", _tableInfo.TableNameWithSchema(type), _tableInfo.TableNameWithoutSchema(type));
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception exception)
                {
                    throw new Exception(string.Format("Could not create table {0}; see the inner exception for more information.", _tableInfo.TableNameWithSchema(type)), exception);
                }
                try
                {
                    CreateIndex(type);
                }
                catch (Exception exception)
                {
                    throw new Exception(string.Format("Could not create index on table {0}; see the inner exception for more information.", _tableInfo.TableNameWithSchema(type)), exception);
                }
            }
        }
    }
}
