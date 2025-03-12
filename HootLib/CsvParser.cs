using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using HootLib.Interfaces;
using HootLib.Objects.Exceptions;
using UnityEngine;

namespace HootLib
{
    /// <summary>
    /// Create a parser to read CSV files with. The file to be read <em>must</em> contain a header line with names that
    /// correspond to parameter names of a constructor of the type the lines will be parsed to.
    /// </summary>
    public class CsvParser : ICsvParser
    {
        protected bool _disposed = false;
        protected StreamReader _stream;
        protected readonly Dictionary<Type, Blueprint> _blueprints;
        public CultureInfo Culture = CultureInfo.InvariantCulture; // Best for parsing independently of user's locale.
        public string[] Header { get; }
        /// This character separates individual cells.
        public char Separator;
        /// This character separates list elements within a cell.
        public char ArraySeparator;
        
        public CsvParser(string filePath, char separator = ',', char arraySeparator = ';')
        {
            _blueprints = new Dictionary<Type, Blueprint>();
            _stream = new StreamReader(filePath);
            Header = Split(_stream.ReadLine());
            Separator = separator;
            ArraySeparator = arraySeparator;
        }

        public CsvParser(Stream fileStream, char separator = ',', char arraySeparator = ';')
        {
            _blueprints = new Dictionary<Type, Blueprint>();
            _stream = new StreamReader(fileStream);
            Header = Split(_stream.ReadLine());
            Separator = separator;
            ArraySeparator = arraySeparator;
        }

        /// <summary>
        /// Construct an instance of T using information gathered from the header and the current line.
        /// </summary>
        protected virtual T Create<T>(string line)
        {
            if (!_blueprints.TryGetValue(typeof(T), out Blueprint blueprint))
            {
                blueprint = CreateBlueprint<T>();
                // Cache the result so we only have to do this once per target type per file.
                _blueprints.Add(typeof(T), blueprint);
            }

            string[] cells = Split(line, Separator);
            object[] parsedCells = ParseAllCells(blueprint, cells);
            if (blueprint.ConstructorParams.Length != parsedCells.Length)
            {
                throw new ParsingException($"Number of parsed cells ({parsedCells}) does not match expected"
                                           + $"parameter count {blueprint.ConstructorParams.Length}!");
            }

            return (T)blueprint.Constructor.Invoke(parsedCells);
        }

        /// <summary>
        /// Creates a mapping from the order of cells in the csv file to the order of parameters in the constructor.
        /// </summary>
        protected virtual Blueprint CreateBlueprint<T>()
        {
            List<ConstructorInfo> constructors = GetConstructors<T>();
            if (constructors.Count == 0)
                throw new ParsingException($"The target type '{typeof(T)}' contains no constructors with parameters!");
            
            // Choose a constructor with the same parameters as the header of the csv file.
            ConstructorInfo constructorInfo = constructors
                .Where(c => c.GetParameters().Length == Header.Length)
                .FirstOrDefault(c => c.GetParameters().All(p =>
                    Header.AsEnumerable().Contains(p.Name, StringComparer.InvariantCultureIgnoreCase)));
            if (constructorInfo is null)
                throw new ParsingException($"The target type '{typeof(T)}' contains no constructors with "
                                           + $"parameters that match the header line of the given file: "
                                           + $"{constructors.ElementsToString()} vs {Header.ElementsToString()}");

            ParameterInfo[] paramInfo = constructorInfo.GetParameters();
            // Figure out which column corresponds to which parameter.
            int[] paramMapping = new int[paramInfo.Length];
            for (int idx = 0; idx < paramMapping.Length; idx++)
            {
                paramMapping[idx] = Header.IndexOf(paramInfo[idx].Name, StringComparer.InvariantCultureIgnoreCase);
            }

            return new Blueprint(typeof(T), constructorInfo, paramInfo.Select(pi => pi.ParameterType).ToArray(), paramMapping);
        }

        /// <summary>
        /// Get all constructors for Type T which specify parameters (parameterless constructors are skipped).
        /// </summary>
        protected List<ConstructorInfo> GetConstructors<T>()
        {
            return AccessTools.GetDeclaredConstructors(typeof(T))
                .Where(c => c.GetParameters().Length > 0)
                .ToList();
        }

        /// <summary>
        /// Parse one cell's string contents into a different type based on the blueprint.
        /// </summary>
        protected virtual object ParseCell(Type type, string cell)
        {
            if (type == typeof(string) && !type.IsAssignableFrom(typeof(IEnumerable<string>)))
                return cell;
            
            // Handle enumerables with recursion.
            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                Type innerType = type.GetElementType();
                string[] array = Split(cell, ArraySeparator);
                return array.Select(elem => ParseCell(innerType, elem)).ToList();
            }
            
            // Try to handle enums first.
            if (type.IsEnum)
                return Hootils.ParseEnum(type, cell);

            // Not exactly elegant but typeof() cannot be used here since it is not a constant.
            object parsed = type.Name.ToLower() switch
            {
                "double" => double.Parse(cell, Culture),
                "float" => float.Parse(cell, Culture),
                "int32" => int.Parse(cell, Culture),
                "single" => float.Parse(cell, Culture),
                _ => null
            };
            
            if (parsed is null)
                Debug.LogError($"{nameof(CsvParser)} failed to parse '{cell}' to {type}");
            return parsed;
        }

        /// <summary>
        /// Parse a line of cells to instantiated types.
        /// </summary>
        /// <param name="blueprint">The blueprint to follow during instantiation.</param>
        /// <param name="cells">All cells of the current line.</param>
        /// <returns>An array of instantiated objects corresponding to the cells, in the same order.</returns>
        protected virtual object[] ParseAllCells(Blueprint blueprint, string[] cells)
        {
            object[] parsedCells = new object[cells.Length];

            for (int idx = 0; idx < cells.Length; idx++)
            {
                Type type = blueprint.ConstructorParams[idx];
                parsedCells[idx] = ParseCell(type, cells[idx]);
            }

            return parsedCells;
        }

        /// <summary>
        /// Parse a line of the file into an object of type T.
        /// </summary>
        /// <typeparam name="T">The target object type.</typeparam>
        /// <returns>An instantiated object of type T.</returns>
        public T ParseLine<T>()
        {
            string line = _stream.ReadLine();
            return Create<T>(line);
        }

        /// <inheritdoc cref="ParseLine{T}"/>
        public async Task<T> ParseLineAsync<T>()
        {
            string line = await _stream.ReadLineAsync();
            return Create<T>(line);
        }

        /// <summary>
        /// Parse all lines of the file into objects of type T.
        /// </summary>
        /// <typeparam name="T">The target object type.</typeparam>
        /// <returns>An instantiated object of type T.</returns>
        public IEnumerable<T> ParseAllLines<T>()
        {
            while (!_stream.EndOfStream)
            {
                yield return ParseLine<T>();
            }
        }

        /// <summary>
        /// Parse all lines of the file into objects of type T. Because each line runs async, the order of results is
        /// non-deterministic. 
        /// </summary>
        /// <typeparam name="T">The target object type.</typeparam>
        /// <returns>A list of instantiated objects of type T.</returns>
        public async Task<List<T>> ParseAllLinesAsync<T>()
        {
            // We're locked to .Net 4.7.2, so there is no IAsyncEnumerable and this is the best we can do.
            // Run each line async individually and then wait for them to sync at the end.
            List<T> results = new List<T>();
            while (!_stream.EndOfStream)
            {
                T line = await ParseLineAsync<T>();
                results.Add(line);
            }

            return results;
        }

        /// <summary>
        /// Split a line of the csv file into individual cells.
        /// </summary>
        /// <param name="line">A line from the csv file.</param>
        /// <param name="separator">The separator to split cells on.</param>
        /// <returns>An array of the separated cell contents.</returns>
        public static string[] Split(string line, char separator = ',')
        {
            int lastSeparatorIdx = 0;
            bool openApostrophe = false;
            bool openQuote = false;
            List<string> cellContents = new List<string>();

            for (int idx = 0; idx < line.Length; idx++)
            {
                char current = line[idx];
                // Handle escape sequences using quotes or apostrophes.
                switch (current)
                {
                    // Commented out because it caused issues with things like "Won't".
                    // case '\'':
                    // {
                    //     openApostrophe = !openApostrophe;
                    //     // If a quote was just closed, consider all "escape sequences" closed.
                    //     if (!openApostrophe)
                    //         openQuote = false;
                    //     break;
                    // }
                    case '"':
                    {
                        openQuote = !openQuote;
                        if (!openQuote)
                            openApostrophe = false;
                        break;
                    }
                }

                // Found a cell separator! Store the contents of the just-passed cell.
                if (current == separator && !openApostrophe && !openQuote)
                {
                    cellContents.Add(TrimSubstring(line, lastSeparatorIdx, idx - lastSeparatorIdx, separator));
                    // Skip the separator.
                    lastSeparatorIdx = idx + 1;
                }
                // On the last iteration, ensure everything worked out and finish the final cell.
                if (idx == line.Length - 1)
                {
                    if (openApostrophe || openQuote)
                        throw new ParsingException("Malformed line ended with unclosed quote: " + line);
                    cellContents.Add(TrimSubstring(line, lastSeparatorIdx, line.Length - lastSeparatorIdx, separator));
                }
            }

            return cellContents.Select(c => c.Trim()).ToArray();
        }

        private static string TrimSubstring(string source, int startIndex, int length, char separator)
        {
            return source.Substring(startIndex, length).Trim(' ', '"', '\'', separator);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // Only execute the cleanup code once.
            if (_disposed)
                return;
            
            if (disposing)
            {
                // Free managed objects.
            }
            // Free unmanaged objects.
            _stream.Dispose();
            _stream = null;
            
            _disposed = true;
        }

        ~CsvParser()
        {
            Dispose(false);
        }
    }

    public struct Blueprint
    {
        public Type TargetType;
        public ConstructorInfo Constructor;
        public Type[] ConstructorParams;
        public int[] ParamMapping;
        
        public Blueprint(Type targetType, ConstructorInfo constructor, Type[] constructorParams, int[] paramMapping)
        {
            TargetType = targetType;
            Constructor = constructor;
            ConstructorParams = constructorParams;
            ParamMapping = paramMapping;
        }
    }
}