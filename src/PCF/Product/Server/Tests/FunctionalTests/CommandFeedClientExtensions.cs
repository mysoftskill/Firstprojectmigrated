namespace PCF.FunctionalTests
{
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public static class CommandFeedClientExtensions
    {
        public static async Task<IEnumerable<DeleteCommand>> ReceiveNonWindowsDeleteCommandsByDeviceIdAsync(this ICommandFeedClient client, string deviceId)
        {
            DateTimeOffset startTime = DateTimeOffset.UtcNow;
            List<DeleteCommand> commandsList = new List<DeleteCommand>();

            while (DateTimeOffset.UtcNow - startTime <= TimeSpan.FromSeconds(300))
            {
                await Task.Delay(1000);
                var commands = await client.GetCommandsAsync(CancellationToken.None);

                foreach (var command in commands)
                {
                    if (!(command is DeleteCommand))
                    {
                        continue;
                    }

                    if (!(command.Subject is NonWindowsDeviceSubject))
                    {
                        continue;
                    }

                    if (string.Equals((command.Subject as NonWindowsDeviceSubject).AsimovMacOsPlatformDeviceId, deviceId))
                    {
                        commandsList.Add((DeleteCommand)command);
                    }
                }

                if (commandsList.Any())
                {
                    break;
                }
            }

            return commandsList;
        }

        /// <summary>
        /// Waits patiently to receive a commands list for specific subject.
        /// </summary>
        public static async Task<IEnumerable<TCommandType>> ReceiveCommandsBySubjectAsync<TCommandType, TSubjectType>(this ICommandFeedClient client)
            where TCommandType : IPrivacyCommand
            where TSubjectType : IPrivacySubject
        {
            DateTimeOffset startTime = DateTimeOffset.UtcNow;
            List<TCommandType> commandsList = new List<TCommandType>();

            while (DateTimeOffset.UtcNow - startTime <= TimeSpan.FromSeconds(300))
            {
                await Task.Delay(1000);
                var commands = await client.GetCommandsAsync(CancellationToken.None);

                foreach (var command in commands)
                {
                    if (!(command is TCommandType))
                    {
                        continue;
                    }

                    if (!(command.Subject is TSubjectType))
                    {
                        continue;
                    }

                    commandsList.Add((TCommandType)command);
                }
            }

            return commandsList;
        }

        /// <summary>
        /// Waits patiently to receive a command.
        /// </summary>
        public static async Task<T> ReceiveCommandAsync<T>(this ICommandFeedClient client, Guid expectedCommandId) where T : IPrivacyCommand
        {
            DateTimeOffset startTime = DateTimeOffset.UtcNow;
            T command = default(T);
            bool received = false;

            while (DateTimeOffset.UtcNow - startTime <= TimeSpan.FromSeconds(300))
            {
                await Task.Delay(1000);
                var commands = await client.GetCommandsAsync(CancellationToken.None);

                var privacyCommand = commands.Where(x => Guid.Parse(x.CommandId) == expectedCommandId).FirstOrDefault();
                if (privacyCommand != null)
                {
                    received = true;
                    command = (T)privacyCommand;
                    break;
                }
            }

            Assert.True(received);
            return command;
        }

        /// <summary>
        /// Waits patiently to receive a command
        /// Makes sure that only one command is received 
        /// </summary>
        public static async Task<T> ReceiveCommandWithFilteringAsync<T>(this ICommandFeedClient client, Guid expectedCommandId, Guid filteredCommandId) where T : IPrivacyCommand
        {
            DateTimeOffset startTime = DateTimeOffset.UtcNow;
            T command = default(T);
            bool received = false;

            while (DateTimeOffset.UtcNow - startTime <= TimeSpan.FromSeconds(300))
            {
                await Task.Delay(1000);
                var commands = await client.GetCommandsAsync(CancellationToken.None);

                var privacyCommand = commands.Where(x => Guid.Parse(x.CommandId) == expectedCommandId).FirstOrDefault();
                var others = commands.Where(x => Guid.Parse(x.CommandId) != expectedCommandId);

                if (privacyCommand != null)
                {
                    // make sure the command expected to be filtered by PCF is not here
                    Assert.Empty(others?.Where(x => Guid.Parse(x.CommandId) == filteredCommandId));

                    received = true;
                    command = (T)privacyCommand;
                    break;
                }
            }

            Assert.True(received);
            return command;
        }
    }
}
