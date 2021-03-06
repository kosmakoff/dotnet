﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using NUnit.Framework;
using StackExchange.Profiling.SqlFormatters;

namespace StackExchange.Profiling.Tests
{
    [TestFixture]
    public class SqlFormatterTest
    {
        private SqlServerFormatter _formatter;
        private string _commandText;
        private SqlCommand _dbCommand;
        private static Dictionary<RuntimeTypeHandle, DbType> _dbTypeMap;

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            CreateDbTypeMap();
        }

        [SetUp]
        public void TestSetup()
        {
            _formatter = new SqlServerFormatter();
        }

        private void CreateDbCommand(CommandType commandType)
        {
            var sqlConnection = new SqlConnection("Initial Catalog=TestDatabase");
            _dbCommand = new SqlCommand(_commandText, sqlConnection);
            _dbCommand.CommandType = commandType;
        }

        private string GenerateOutput()
        {
            var sqlParameters = SqlTiming.GetCommandParameters(_dbCommand);
            var output = _formatter.FormatSql(_commandText, sqlParameters, _dbCommand);
            return output;
        }

        private void AddDbParameter<T>(string name, object value, ParameterDirection parameterDirection = ParameterDirection.Input)
        {
            var parameter = _dbCommand.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            parameter.Direction = parameterDirection;
            parameter.DbType = GetDbType(typeof(T));
            _dbCommand.Parameters.Add(parameter);
        }

        private static void CreateDbTypeMap()
        {
            #region copied from dapper
            _dbTypeMap = new Dictionary<RuntimeTypeHandle, DbType>();
            _dbTypeMap[typeof(byte).TypeHandle] = DbType.Byte;
            _dbTypeMap[typeof(sbyte).TypeHandle] = DbType.SByte;
            _dbTypeMap[typeof(short).TypeHandle] = DbType.Int16;
            _dbTypeMap[typeof(ushort).TypeHandle] = DbType.UInt16;
            _dbTypeMap[typeof(int).TypeHandle] = DbType.Int32;
            _dbTypeMap[typeof(uint).TypeHandle] = DbType.UInt32;
            _dbTypeMap[typeof(long).TypeHandle] = DbType.Int64;
            _dbTypeMap[typeof(ulong).TypeHandle] = DbType.UInt64;
            _dbTypeMap[typeof(float).TypeHandle] = DbType.Single;
            _dbTypeMap[typeof(double).TypeHandle] = DbType.Double;
            _dbTypeMap[typeof(decimal).TypeHandle] = DbType.Decimal;
            _dbTypeMap[typeof(bool).TypeHandle] = DbType.Boolean;
            _dbTypeMap[typeof(string).TypeHandle] = DbType.String;
            _dbTypeMap[typeof(char).TypeHandle] = DbType.StringFixedLength;
            _dbTypeMap[typeof(Guid).TypeHandle] = DbType.Guid;
            _dbTypeMap[typeof(DateTime).TypeHandle] = DbType.DateTime;
            _dbTypeMap[typeof(DateTimeOffset).TypeHandle] = DbType.DateTimeOffset;
            _dbTypeMap[typeof(byte[]).TypeHandle] = DbType.Binary;
            _dbTypeMap[typeof(byte?).TypeHandle] = DbType.Byte;
            _dbTypeMap[typeof(sbyte?).TypeHandle] = DbType.SByte;
            _dbTypeMap[typeof(short?).TypeHandle] = DbType.Int16;
            _dbTypeMap[typeof(ushort?).TypeHandle] = DbType.UInt16;
            _dbTypeMap[typeof(int?).TypeHandle] = DbType.Int32;
            _dbTypeMap[typeof(uint?).TypeHandle] = DbType.UInt32;
            _dbTypeMap[typeof(long?).TypeHandle] = DbType.Int64;
            _dbTypeMap[typeof(ulong?).TypeHandle] = DbType.UInt64;
            _dbTypeMap[typeof(float?).TypeHandle] = DbType.Single;
            _dbTypeMap[typeof(double?).TypeHandle] = DbType.Double;
            _dbTypeMap[typeof(decimal?).TypeHandle] = DbType.Decimal;
            _dbTypeMap[typeof(bool?).TypeHandle] = DbType.Boolean;
            _dbTypeMap[typeof(char?).TypeHandle] = DbType.StringFixedLength;
            _dbTypeMap[typeof(Guid?).TypeHandle] = DbType.Guid;
            _dbTypeMap[typeof(DateTime?).TypeHandle] = DbType.DateTime;
            _dbTypeMap[typeof(DateTimeOffset?).TypeHandle] = DbType.DateTimeOffset;
            #endregion
        }

        private static DbType GetDbType(Type type)
        {
            return _dbTypeMap[type.TypeHandle];
        }

        [Test]
        public void EnsureVerboseSqlServerFormatterAddsOnlyInformation()
        {
            // arrange
			// overwrite the formatter
	        _formatter = new VerboseSqlServerFormatter();
            _commandText = "select 1";
            const string expectedOutput = "-- Command Type: Text\r\n-- Database: TestDatabase\r\n\r\nselect 1;";
            CreateDbCommand(CommandType.Text);

            // act
            var actualOutput = GenerateOutput();

            // assert
            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void TabelQueryWithoutParameters()
        {
            // arrange
            _commandText = "select 1";
            const string expectedOutput = "select 1;";
            CreateDbCommand(CommandType.Text);

            // act
            var actualOutput = GenerateOutput();

            // assert
            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void TableQueryWithOneParameters()
        {
            // arrange
            _commandText = "select 1 from dbo.Table where x = @a";
            const string expectedOutput = "DECLARE @a int = 123;\r\n\r\nselect 1 from dbo.Table where x = @a;";
            CreateDbCommand(CommandType.Text);
            AddDbParameter<int>("a", 123);

            // act
            var actualOutput = GenerateOutput();

            // assert
            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void TableQueryWithTwoParameters()
        {
            // arrange
            _commandText = "select 1 from dbo.Table where x = @x, y = @y";
            const string expectedOutput = "DECLARE @x int = 123,\r\n        @y bigint = 123;\r\n\r\nselect 1 from dbo.Table where x = @x, y = @y;";
            CreateDbCommand(CommandType.Text);
            AddDbParameter<int>("x", 123);
            AddDbParameter<long>("y", 123);

            // act
            var actualOutput = GenerateOutput();

            // assert
            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void StoredProcedureCallWithoutParameters()
        {
            // arrange
            _commandText = "dbo.SOMEPROCEDURE";
            const string expectedOutput = "EXEC dbo.SOMEPROCEDURE;";
            CreateDbCommand(CommandType.StoredProcedure);

            // act
            var actualOutput = GenerateOutput();

            // assert
            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void StoredProcedureCallWithOneParameter()
        {
            // arrange
            _commandText = "dbo.SOMEPROCEDURE";
            const string expectedOutput = "DECLARE @x int = 123;\r\n\r\nEXEC dbo.SOMEPROCEDURE @x = @x;";
            CreateDbCommand(CommandType.StoredProcedure);
            AddDbParameter<int>("x", 123, ParameterDirection.Input);

            // act
            var actualOutput = GenerateOutput();

            // assert
            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void StoredProcedureCallWithTwoParameter()
        {
            // arrange
            _commandText = "dbo.SOMEPROCEDURE";
            const string expectedOutput = "DECLARE @x int = 123,\r\n        @y bigint = 123;\r\n\r\nEXEC dbo.SOMEPROCEDURE @x = @x, @y = @y;";
            CreateDbCommand(CommandType.StoredProcedure);
            AddDbParameter<int>("x", 123, ParameterDirection.Input);
            AddDbParameter<long>("y", 123, ParameterDirection.Input);

            // act
            var actualOutput = GenerateOutput();

            // assert
            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void StoredProcedureCallWithOneReturnParameter()
        {
            // arrange
            _commandText = "dbo.SOMEPROCEDURE";
            const string expectedOutput = "DECLARE @retval int;\r\n\r\nEXEC @retval = dbo.SOMEPROCEDURE;\r\nSELECT @retval AS ReturnValue;";
            CreateDbCommand(CommandType.StoredProcedure);
            AddDbParameter<int>("retval", null, ParameterDirection.ReturnValue);

            // act
            var actualOutput = GenerateOutput();

            // assert
            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void StoredProcedureCallWithNormalAndReturnParameter()
        {
            // arrange
            _commandText = "dbo.SOMEPROCEDURE";
            const string expectedOutput = "DECLARE @x int = 123,\r\n        @retval int;\r\n\r\nEXEC @retval = dbo.SOMEPROCEDURE @x = @x;\r\nSELECT @retval AS ReturnValue;";
            CreateDbCommand(CommandType.StoredProcedure);
            AddDbParameter<int>("x", 123, ParameterDirection.Input);
            AddDbParameter<int>("retval", null, ParameterDirection.ReturnValue);

            // act
            var actualOutput = GenerateOutput();

            // assert
            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void StoredProcedureCallWithOutputParameter()
        {
            // arrange
            _commandText = "dbo.SOMEPROCEDURE";
            const string expectedOutput = "DECLARE @x int = 123;\r\n\r\nEXEC dbo.SOMEPROCEDURE @x = @x OUTPUT;";
            CreateDbCommand(CommandType.StoredProcedure);
            // note: since the sql-OUTPUT parameters can be read within the procedure, we need to support setting the value
            AddDbParameter<int>("x", 123, ParameterDirection.Output);

            // act
            var actualOutput = GenerateOutput();

            // assert
            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void StoredProcedureCallWithInOutputParameter()
        {
            // arrange
            _commandText = "dbo.SOMEPROCEDURE";
            const string expectedOutput = "DECLARE @x int = 123;\r\n\r\nEXEC dbo.SOMEPROCEDURE @x = @x OUTPUT;";
            CreateDbCommand(CommandType.StoredProcedure);
            // note: since the sql-OUTPUT parameters can be read within the procedure, we need to support setting the value
            AddDbParameter<int>("x", 123, ParameterDirection.InputOutput);

            // act
            var actualOutput = GenerateOutput();

            // assert
            Assert.AreEqual(expectedOutput, actualOutput);
        }
    }
}