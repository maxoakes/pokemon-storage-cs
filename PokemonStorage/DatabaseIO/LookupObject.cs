using PokemonStorage;

namespace UtilityLibCore.DatabaseIO
{
    /// <summary>
    /// A DbObject that can be used in EditMangers. Typically this type of object is created or modified only via EditManger-used forms.
    /// </summary>
    public abstract class LookupObject : DbObject
    {
        public LookupObject() { }

        /// <summary>
        /// Determines if a different object is similar enough to another. If they are similar enough, it can be said that the two objects represent
        /// the same data, even if they are different objects in memory.
        /// </summary>
        /// <param name="test">Object to test similarity</param>
        /// <returns>True if the objects are effectively the same, false if they are objectively different.</returns>
        public virtual bool IsSameContent(LookupObject test)
        {
            if (test == null) { return false; }
            return
                PrimaryKey == test.PrimaryKey;
        }

        /// <summary>
        /// Determines if certain required properties are non-blank. Required properties are ones that are typically important to the end user for describing the object.
        /// </summary>
        /// <returns>True if required properties are not null or whitespace, false if they are null or whitespace</returns>
        public virtual bool AreRequiredPropertiesFilled()
        {
            return true;
        }
    }
}
