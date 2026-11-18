namespace Donations.Domain.Exceptions;

/// <summary>Violación de una regla de negocio del dominio. La Api la traduce a 400 Bad Request.</summary>
public class BussinesRuleException : Exception
{
    public BussinesRuleException(string message) : base(message)
    {
    }
}
