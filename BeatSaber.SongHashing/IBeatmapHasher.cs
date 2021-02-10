using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BeatSaber.SongHashing
{
    public interface IBeatmapHasher
    {
        /// <summary>
        /// Generates a hash for the song and assigns it to the SongHash field. Returns null if info.dat doesn't exist.
        /// TODO: Handle/document exceptions (such as if the files no longer exist when this is called).
        /// </summary>
        /// <returns>Hash of the song files. Null if the info.dat file doesn't exist</returns>
        /// <exception cref="DirectoryNotFoundException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="JsonException"></exception>
        string? HashDirectory(string songDirectory);

        /// <summary>
        /// Generates a quick hash to determine if the song directory or it's files has changed.
        /// Does NOT equate to a Beat Saver hash.
        /// </summary>
        /// <param name="songDirectory"></param>
        /// <returns></returns>
        long QuickDirectoryHash(string songDirectory);
    }
}
