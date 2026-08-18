using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Data;

namespace Aceca.Adm.Helper
{
    /// <summary>
    /// Checagens de esquema (coluna/índice já existe?) via INFORMATION_SCHEMA, usadas para
    /// tornar os ALTER TABLE de inicialização idempotentes sem depender de "IF NOT EXISTS"/
    /// "IF EXISTS" em ADD COLUMN/ADD INDEX — sintaxe que o MySQL do servidor em uso rejeita
    /// (só é aceita a partir do MySQL 8.0.29, e nem toda hospedagem/versão suporta), o que
    /// antes fazia essas rotinas falharem com erro de sintaxe em todo restart da aplicação.
    /// </summary>
    public static class DbSchemaHelper
    {
        public static Task<bool> ColunaExisteAsync(DatabaseFacade database, string tabela, string coluna) =>
            ExisteAsync(database,
                "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = @tabela AND column_name = @nome",
                tabela, coluna);

        public static Task<bool> IndiceExisteAsync(DatabaseFacade database, string tabela, string indice) =>
            ExisteAsync(database,
                "SELECT COUNT(*) FROM information_schema.statistics WHERE table_schema = DATABASE() AND table_name = @tabela AND index_name = @nome",
                tabela, indice);

        private static async Task<bool> ExisteAsync(DatabaseFacade database, string sql, string tabela, string nome)
        {
            var conn = database.GetDbConnection();

            var precisaAbrir = conn.State != ConnectionState.Open;
            if (precisaAbrir)
                await conn.OpenAsync();

            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;

                var pTabela = cmd.CreateParameter();
                pTabela.ParameterName = "@tabela";
                pTabela.Value = tabela;
                cmd.Parameters.Add(pTabela);

                var pNome = cmd.CreateParameter();
                pNome.ParameterName = "@nome";
                pNome.Value = nome;
                cmd.Parameters.Add(pNome);

                var resultado = await cmd.ExecuteScalarAsync();
                return Convert.ToInt32(resultado) > 0;
            }
            finally
            {
                if (precisaAbrir)
                    await conn.CloseAsync();
            }
        }
    }
}
