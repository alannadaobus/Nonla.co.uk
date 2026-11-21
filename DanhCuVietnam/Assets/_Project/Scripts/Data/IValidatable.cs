using System.Collections.Generic;

namespace DanhCu.Data
{
    /// <summary>Data assets that can report authoring mistakes. Checked by EditMode tests and the setup tool.</summary>
    public interface IValidatable
    {
        /// <summary>Appends a message per problem found; appends nothing when the asset is valid.</summary>
        void Validate(List<string> errors);
    }
}
