﻿using System.ComponentModel.DataAnnotations;
using System.Linq;
using PDGToolkitCore.API;

namespace PDGToolkitCLI.Validation
{
    internal class IsValidGeneratorName : ValidationAttribute
    {
        private static readonly string CustomErrorMessage = ResponseFormatter.RespondWithCollection(
            "The value for {0} must be one of available generators, currently supported ones are: ",
            InternalComponentService.GetAllGenerators());
        
        public IsValidGeneratorName() : base(CustomErrorMessage) { }

        protected override ValidationResult IsValid(object value, ValidationContext context)
        {
            var generators = InternalComponentService.GetAllGenerators();
            if (value == null || !generators.Contains(value))
                return new ValidationResult(FormatErrorMessage(context.DisplayName));

            return ValidationResult.Success;
        }
    }
}
