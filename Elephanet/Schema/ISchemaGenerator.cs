using System;

public interface ISchemaGenerator
{
    void GetOrCreateTable(Type type);
}
