using System;

namespace HexMate
{
    /// <summary>
    /// Specifies whether relevant methods produce uppercase or lowercase letters and whether line breaks are inserted in their output.
    /// </summary>
    [Flags]
    public enum HexFormattingOptions
    {
        /// <summary>
        /// Produces only uppercase characters and does not insert line breaks after every 72 characters in the string representation.
        /// </summary>
        None = 0,

        /// <summary>
        /// Inserts line breaks after every 72 characters in the string representation.
        /// </summary>
        InsertLineBreaks = 1,

        /// <summary>
        /// Produces only lowercase characters in the string representation.
        /// </summary>
        Lowercase = 2
    }
}