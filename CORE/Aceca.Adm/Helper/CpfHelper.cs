namespace Aceca.Adm.Helper
{
    // Validação de CPF (formato + dígito verificador) - só matemática local, sem chamada
    // externa. Não confirma que o CPF é de uma pessoa viva/real (isso exigiria consulta à
    // Receita Federal, que tem rate limit e pede data de nascimento) - só filtra números
    // obviamente inválidos/inventados (dígito errado, sequências repetidas como 111.111.111-11).
    public static class CpfHelper
    {
        public static string SomenteDigitos(string? cpf) =>
            new string((cpf ?? string.Empty).Where(char.IsDigit).ToArray());

        public static bool EhValido(string? cpf)
        {
            var digitos = SomenteDigitos(cpf);

            if (digitos.Length != 11)
                return false;

            // Sequências com todos os dígitos iguais passam a fórmula do dígito verificador
            // matematicamente, mas nunca são CPFs reais emitidos - bloqueadas explicitamente.
            if (digitos.Distinct().Count() == 1)
                return false;

            var numeros = digitos.Select(c => c - '0').ToArray();

            int CalcularDigito(int quantidade)
            {
                int soma = 0;
                int peso = quantidade + 1;

                for (int i = 0; i < quantidade; i++)
                    soma += numeros[i] * peso--;

                int resto = soma % 11;
                return resto < 2 ? 0 : 11 - resto;
            }

            return CalcularDigito(9) == numeros[9] && CalcularDigito(10) == numeros[10];
        }

        public static string Formatar(string? cpf)
        {
            var digitos = SomenteDigitos(cpf);
            return digitos.Length == 11
                ? $"{digitos[..3]}.{digitos[3..6]}.{digitos[6..9]}-{digitos[9..]}"
                : digitos;
        }
    }
}
