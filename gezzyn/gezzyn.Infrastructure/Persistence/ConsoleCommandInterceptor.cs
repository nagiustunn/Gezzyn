using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace gezzyn.Infrastructure.Persistence
{
    public class ConsoleCommandInterceptor : DbCommandInterceptor
    {
        public override InterceptionResult<int> NonQueryExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result)
        {
            WriteCommand(command);
            return base.NonQueryExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            WriteCommand(command);
            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }

        private void WriteCommand(DbCommand command)
        {
            Console.WriteLine("SQL Command:");
            Console.WriteLine(command.CommandText);

            if (command.Parameters.Count > 0)
            {
                Console.WriteLine("Parameters:");
                foreach (DbParameter p in command.Parameters)
                {
                    Console.WriteLine($"  {p.ParameterName} = {p.Value} ({p.DbType})");
                }
            }

            Console.WriteLine("--------------------------------------------------");
        }
    }
}
