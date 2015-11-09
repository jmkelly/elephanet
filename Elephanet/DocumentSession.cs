using System;
using System.Collections.Generic;
using Npgsql;
using System.Linq;
using System.Data;
using Elephanet.Serialization;
using Elephanet.Conventions;
using Elephanet.Extensions;


/*
 *implement fts on all string columns with a query like the following...
 * 
 * select body from elephanet_demo_toilet
where lower(cast(body as text)) like '%sandy%point%victoria%'
and similarity(lower(cast(body as text)), '%sandy%point%victoria%') > 0
order by similarity(lower(cast(body as text)), '%sandy%point%victoria%') desc
limit 20;
 * 
 * 
 * needs an index like
 * 
 * CREATE INDEX toilet_search_idx ON elephanet_demo_toilet USING gin (lower(cast(body as text))  gin_trgm_ops)
drop index toilet_search_idx;
 */

namespace Elephanet
{
    public class DocumentSession : IDocumentSession
    {
        readonly IDocumentStore _documentStore;
        readonly NpgsqlConnection _conn;
        protected readonly Dictionary<Guid, object> _entities = new Dictionary<Guid, object>();
        readonly IJsonConverter _jsonConverter;
        readonly JsonbQueryProvider _queryProvider;
        readonly ITableInfo _tableInfo;
        readonly ISchemaGenerator _schemaGenerator;
      


        //todo provide extra constructors for injecting these dependencies
        public DocumentSession(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
            _tableInfo = _documentStore.Conventions.TableInfo;
            _conn = new NpgsqlConnection(documentStore.ConnectionString);
            _conn.Open();
            _jsonConverter = documentStore.Conventions.JsonConverter;
            _queryProvider = new JsonbQueryProvider(_conn, _jsonConverter, _tableInfo);
            _schemaGenerator = new SchemaGenerator(documentStore, _conn);
        }

        public void Delete<T>(Guid id)
        {
            _schemaGenerator.GetOrCreateTable(typeof(T));
            using (var command = _conn.CreateCommand())
            {
                command.CommandType = CommandType.Text;
                command.CommandText = String.Format(@"Delete FROM {0} WHERE id = :id;", _tableInfo.TableNameWithSchema(typeof(T)));
                command.Parameters.AddWithValue(":id", id);
                command.ExecuteNonQuery();
            }
        }

        public void DeleteAll<T>()
        {
            _schemaGenerator.GetOrCreateTable(typeof(T));
            using (var command = _conn.CreateCommand())
            {
                command.CommandType = CommandType.Text;
                command.CommandText = String.Format(@"DELETE FROM {0};", _tableInfo.TableNameWithSchema(typeof(T)));
                command.ExecuteNonQuery();
            }

        }

        public T LoadInternal<T>(Guid id)
        {

            _schemaGenerator.GetOrCreateTable(typeof(T));
            using (var command = _conn.CreateCommand())
            {
                command.CommandType = CommandType.Text;
                command.CommandText = String.Format(@"SELECT body FROM {0} WHERE id = :id LIMIT 1;", _tableInfo.TableNameWithSchema(typeof(T)));

                command.Parameters.AddWithValue(":id", id);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return _jsonConverter.Deserialize<T>(reader.GetString(0));
                    }

                    return default(T);
                }
            }
        }

        public IJsonbQueryable<T> Query<T>()
        {
            var query = new JsonbQueryable<T>(new JsonbQueryProvider(_conn, _jsonConverter, _tableInfo));
            return query;
        }

        public IEnumerable<T> Query<T>(string sql, params object[] parameters)
        {

            if (!sql.Contains("select"))
            {
                string tableName = _tableInfo.TableNameWithSchema(typeof(T));
                sql = string.Format("select data from {0}", tableName);
            }


            var command = new NpgsqlCommand();
            foreach (var parameter in parameters)
            {
                
                var param = command.AddParameter(parameter);
                sql = sql.UseParameter(param);
            }


            command.CommandText = sql;

            Type elementType = TypeSystem.GetElementType(typeof(T));
            Type listType = typeof(IList<>).MakeGenericType(elementType);
            IList<T> list = (IList<T>)Activator.CreateInstance(listType);

            using (var reader = command.ExecuteReader())
            {
                foreach (var item in reader)
                {
                    object entity = _jsonConverter.Deserialize(reader.GetString(0), elementType);
                    list.Add((T)entity);
                }
            }

            return list;
        }

        public void SaveChanges()
        {
            //save the cache out to the db
            SaveInternal();
        }

       

        void SaveInternal()
        {
            var runner = new CommandRunner(_conn, _entities, _schemaGenerator, _tableInfo, _jsonConverter);
            runner.Execute();

            _entities.Clear();
        }
      

        public void Store<T>(T entity)
        {
            var id = IdentityFactory.SetEntityId(entity);
            _entities[id] = entity;
        }

       


       

        public void Dispose()
        {
            _conn.Close();
        }


        public void Delete<T>(T entity)
        {
            throw new NotImplementedException();
        }

        T IDocumentSession.GetById<T>(Guid id)
        {
            _schemaGenerator.GetOrCreateTable(typeof(T));
            //hit the db first, so we get most up-to-date
            var entity = LoadInternal<T>(id);
            //try the cache just incase hasn't been saved to db yet, but is in session
            if ((entity == null) && _entities.ContainsKey(id))
                entity = (T)_entities[id];

            if (entity == null)
            {
                if (_documentStore.Conventions.EntityNotFoundBehavior == EntityNotFoundBehavior.ReturnNull)
                {
                    return default(T);
                }

                throw new EntityNotFoundException(id, typeof(T));
            }
            return entity;
        }

        public IEnumerable<T> GetByIds<T>(IEnumerable<Guid> ids)
        {

            _schemaGenerator.GetOrCreateTable(typeof(T));
            using (var command = _conn.CreateCommand())
            {
                command.CommandType = CommandType.Text;
                command.CommandText = String.Format(@"SELECT body FROM {0} WHERE id in ({1});", _tableInfo.TableNameWithSchema(typeof(T)), JoinAndCommaSeperateAndSurroundWithSingleQuotes(ids));
                Console.WriteLine(command.CommandText);

                var entities = new List<T>();

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        T entity = _jsonConverter.Deserialize<T>(reader.GetString(0));
                        entities.Add(entity);
                    }
                }
                return entities;
            }
        }

        private string JoinAndCommaSeperateAndSurroundWithSingleQuotes<T>(IEnumerable<T> ids)
        {
            return string.Join(",", ids.Select(n => n.ToString().SurroundWithSingleQuote()).ToArray());
        }

        public IEnumerable<T> GetAll<T>()
        {
            _schemaGenerator.GetOrCreateTable(typeof(T));
            using (var command = _conn.CreateCommand())
            {
                command.CommandType = CommandType.Text;
                command.CommandText = String.Format(@"SELECT body FROM {0};", _tableInfo.TableNameWithSchema(typeof(T)));
                Console.WriteLine(command.CommandText);

                var entities = new List<T>();

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        T entity = _jsonConverter.Deserialize<T>(reader.GetString(0));
                        entities.Add(entity);
                    }
                }
                return entities;
            }
        }

       
    }
}
