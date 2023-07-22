// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.Utilities
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Text;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    ///     IOHelpers
    /// </summary>
    public static class IOHelpers
    {
        public static void DisplaySuccessResult<T>(T result)
        {
            Console.WriteLine("Result:");
            Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented, new StringEnumConverter()));
        }

        public static char GetUserInputCharacter(string display)
        {
            Console.WriteLine(display);

            return GetUserInputCharacter();
        }

        public static char GetUserInputCharacter()
        {
            string input = null;
            do
            {
                input = Console.ReadLine();
            } while (!IsValidChar(input));
            Console.WriteLine();

            return input[0];
        }

        public static int GetUserInputInt(string display)
        {
            int result;
            string input;

            Console.WriteLine(display);
            do
            {
                input = Console.ReadLine();
            } while (!IsValidInt(input, out result));
            Console.WriteLine();

            return result;
        }

        [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "long")]
        public static long GetUserInputLong(string display)
        {
            long result;
            string input;

            Console.WriteLine(display);
            do
            {
                input = Console.ReadLine();
            } while (!IsValidLong(input, out result));
            Console.WriteLine();

            return result;
        }

        public static string GetUserInputString(string display, bool allowNull = false)
        {
            string input;

            Console.WriteLine(display);
            do
            {
                input = Console.ReadLine();
                if (allowNull && string.IsNullOrWhiteSpace(input))
                {
                    Console.WriteLine();
                    return null;
                }
            } while (!IsValidString(input));
            Console.WriteLine();

            return input;
        }

        public static string GetUserInputStringPrivate(string display)
        {
            string input;

            Console.WriteLine(display);
            do
            {
                input = ReadLineHidden();
            } while (!IsValidString(input));
            Console.WriteLine();

            return input;
        }

        public static char GetValidUserInputCharacter(params char[] validChoices)
        {
            char input;

            do
            {
                input = GetUserInputCharacter();
            } while (!IsValidChoice(input, validChoices));
            Console.WriteLine();

            return input;
        }

        public static string ReadLineHidden()
        {
            var line = new StringBuilder();

            while (true)
            {
                ConsoleKeyInfo input = Console.ReadKey(intercept: true);
                if (input.Key == ConsoleKey.Enter)
                {
                    break;
                }
                if (input.Key == ConsoleKey.Backspace)
                {
                    if (line.Length > 0)
                    {
                        line.Remove(line.Length - 1, 1);
                        Console.Write("\b \b");
                    }
                }
                else
                {
                    line.Append(input.KeyChar);
                    Console.Write("*");
                }
            }

            return line.ToString();
        }

        public static void SaveSuccessResult<T>(T result, string fileName)
        {
            Console.WriteLine("Saved Result to {0}", fileName);
            File.WriteAllText(fileName, JsonConvert.SerializeObject(result, Formatting.Indented, new StringEnumConverter()));
        }

        private static bool IsValidChar(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                Console.WriteLine("Please enter a choice");
                return false;
            }

            if (value.Length > 1)
            {
                Console.WriteLine("Please enter a single character");
                return false;
            }

            return true;
        }

        private static bool IsValidChoice(char value, params char[] validChoices)
        {
            if (!validChoices.Contains(value))
            {
                Console.WriteLine("Please enter one of the choices shown");
                return false;
            }

            return true;
        }

        private static bool IsValidInt(string value, out int result)
        {
            result = default(int);

            if (string.IsNullOrEmpty(value) || !int.TryParse(value, out result))
            {
                Console.WriteLine("Please enter a valid long type");
                return false;
            }

            return true;
        }

        private static bool IsValidLong(string value, out long result)
        {
            result = default(long);

            if (string.IsNullOrEmpty(value) || !long.TryParse(value, out result))
            {
                Console.WriteLine("Please enter a valid long type");
                return false;
            }

            return true;
        }

        private static bool IsValidString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                Console.WriteLine("Please enter a string");
                return false;
            }

            return true;
        }
    }
}
