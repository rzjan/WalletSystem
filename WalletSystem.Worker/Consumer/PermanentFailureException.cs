namespace WalletSystem.Worker.Consumer;

// Falla que no se arregla reintentando (mensaje malformado, datos inválidos).
// Va directo a la DLQ.
public sealed class PermanentFailureException : Exception
{
    public PermanentFailureException(string message) : base(message) { }
}
