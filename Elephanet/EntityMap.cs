using System;

public class EntityMap
{

    public EntityMap(Type type, string tableName, string temporaryTableName)
    {
        EntityType = type;
        TableName = tableName;
        TemporaryTableName = temporaryTableName;
    }

    public Type EntityType { get; set; }
    public string TableName { get; set; }
    public string TemporaryTableName { get; set; }
}