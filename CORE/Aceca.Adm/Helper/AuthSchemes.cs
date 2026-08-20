namespace Aceca.Adm.Helper
{
    // Nomes de esquema de autenticação compartilhados entre Program.cs (registro) e os
    // controllers (uso em Challenge/AuthenticateAsync/SignOutAsync) - evita string literal
    // duplicada e o risco de um lado renomear sem o outro acompanhar.
    public static class AuthSchemes
    {
        // Esquema temporário só pra guardar o ticket do Google entre o callback do OAuth e
        // a decisão de negócio (checar se o e-mail já é sócio, pedir CPF etc.) - nunca
        // autentica a sessão real da aplicação. Ver AuthController.GoogleLogin/GoogleCallback.
        public const string ExternalGoogle = "ExternalGoogle";
    }
}
