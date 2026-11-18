using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation.Results;

namespace Donations.Application.Exceptions;

/// <summary>Los datos de entrada no cumplen las validaciones. La Api la traduce a 400 Bad Request.</summary>
public class CustomValidationException : Exception
{
    public List<string> Errors { get; set; } = [];

    public CustomValidationException(ValidationResult validationResult)
    {
        Errors.AddRange(validationResult.Errors.Select(error => error.ErrorMessage));
    }

    public CustomValidationException(string errorMessage)
    {
        Errors.Add(errorMessage);
    }
}
