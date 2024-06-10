using ServiceStack.OrmLite;
using System.Data;
using System.Data.SqlClient;
using System.Linq.Expressions;

namespace OrmLiteTest
{
    public class Program
    {
        static void Main(string[] args)
        {
            OrmLiteConfig.DialectProvider = SqlServer2012Dialect.Provider;
            OrmLiteConfig.DialectProvider.RegisterConverter<DataTable>(new SqlServerDataTableConverter());

            var table = new DataTable();
            table.Columns.Add("Id", typeof(int));
            Enumerable.Range(1, 5).Select(x => table.Rows.Add(x));

            var subQuery = OrmLiteConfig.DialectProvider.SqlExpression<Owner>()
                .JoinToList(x => x.Id, table);

            Console.WriteLine("SubQuery Parameters:");
            foreach (var param in subQuery.Params)
            {
                if (param is SqlParameter sqlParam)
                {
                    Console.WriteLine();
                    Console.WriteLine($"DbType: {sqlParam.DbType}");
                    Console.WriteLine($"SqlDbType: {sqlParam.SqlDbType}");
                    Console.WriteLine($"TypeName: {sqlParam.TypeName}");
                }
            }

            int petType = (int)PetType.Dog;

            var query = OrmLiteConfig.DialectProvider.SqlExpression<Pet>()
                .Where(x => x.Type == petType)
                .And(x => Sql.In(x.OwnerId, subQuery));

            Console.WriteLine();
            Console.WriteLine("Main Query Parameters:");
            foreach (var param in query.Params)
            {
                if (param is SqlParameter sqlParam)
                {
                    Console.WriteLine();
                    Console.WriteLine($"DbType: {sqlParam.DbType}");
                    Console.WriteLine($"SqlDbType: {sqlParam.SqlDbType}");
                    Console.WriteLine($"TypeName: {sqlParam.TypeName}");
                }                
            }
        }
    }

    public static class SqlExpressionExtensions
    {
        public static SqlExpression<T> JoinToList<T>(this SqlExpression<T> self, Expression<Func<T, int>> expression, DataTable table)
        {
            var sourceDefinition = ModelDefinition<T>.Definition;

            var property = self.Visit(expression);
            var parameter = self.ConvertToParam(table);

            // Expected SQL: INNER JOIN @0 ON "Parent"."EvaluatedExpression"= "@0".Id
            var onExpression = $@"ON ({self.SqlTable(sourceDefinition)}.{self.SqlColumn(property.ToString())} = ""{parameter}"".""Id"")";
            var customSql = $"INNER JOIN {parameter} {onExpression}";
            self.CustomJoin(customSql);

            return self;
        }
    }

    public class SqlServerDataTableConverter : OrmLiteConverter
    {
        public override string ColumnDefinition
            => throw new NotImplementedException("Only used to pass a list of Ids as a parameter.");

        public override void InitDbParam(IDbDataParameter p, Type fieldType)
        {
            if (p is SqlParameter)
            {
                var sqlParameter = p as SqlParameter;
                sqlParameter.SqlDbType = SqlDbType.Structured;
                sqlParameter.TypeName = "dbo.Integer_Table_Type";
            }
        }
    }

    public class Owner
    {
        public int Id { get; set; }
    }

    public class Pet
    {
        public int Id { get; set; }

        public int OwnerId { get; set; }

        public int Type { get; set; }
    }

    public enum PetType
    {
        Dog = 1,
        Cat = 2
    }
}
