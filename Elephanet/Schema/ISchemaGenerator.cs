using System;
using System.Collections.Generic;

public interface ISchemaGenerator
{
    void GetOrCreateTable(Type type);
    HashSet<EntityMap> MatchEntityToFinalTableAndTemporaryTable(Dictionary<Guid, object> entities);
}
