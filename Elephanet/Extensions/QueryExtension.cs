﻿using System;
using Npgsql;
using NpgsqlTypes;
using System.Data;

namespace Elephanet.Extensions
{
    public static class CommandExtensions
        {
            public static NpgsqlParameter AddParameter(this NpgsqlCommand command, object value)
            {
                var name = "arg" + command.Parameters.Count;

                var parameter = command.CreateParameter();
                parameter.ParameterName = name;
                parameter.Value = value ?? DBNull.Value;
                command.Parameters.Add(parameter);

                return parameter;
            }

            public static NpgsqlParameter AddParameter(this NpgsqlCommand command, string name, object value)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = name;
                parameter.Value = value ?? DBNull.Value;
                command.Parameters.Add(parameter);

                return parameter;
            }

            public static NpgsqlCommand WithParameter(this NpgsqlCommand command, string name, object value)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = name;
                parameter.Value = value ?? DBNull.Value;
                command.Parameters.Add(parameter);

                return command;
            }

            public static NpgsqlCommand AsSproc(this NpgsqlCommand command)
            {
                command.CommandType = CommandType.StoredProcedure;

                return command;
            }

            public static NpgsqlCommand WithJsonParameter(this NpgsqlCommand command, string name, string json)
            {
                command.Parameters.Add("doc", NpgsqlDbType.Json).Value = json;

                return command;
            }
        }
}
