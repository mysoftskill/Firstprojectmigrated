// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.CommandFeed.Client.Test
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.Policy;

    public class ExportTestAgent : IPrivacyDataAgent
    {
        private readonly Dictionary<DataTypeId, List<Tuple<DateTimeOffset, string, object>>> data;

        private readonly MemoryExportDestination destination;

        private readonly CommandFeedLogger logger;

        public ExportTestAgent(CommandFeedLogger logger, MemoryExportDestination destination, Dictionary<DataTypeId, List<Tuple<DateTimeOffset, string, object>>> data)
        {
            this.logger = logger;
            this.destination = destination;
            this.data = data;
        }

        public Task ProcessAccountClosedAsync(IAccountCloseCommand command)
        {
            throw new NotSupportedException();
        }

        public Task ProcessAgeOutAsync(IAgeOutCommand command)
        {
            throw new NotSupportedException();
        }

        public Task ProcessDeleteAsync(IDeleteCommand command)
        {
            throw new NotSupportedException();
        }

        public async Task ProcessExportAsync(IExportCommand command)
        {
            if (!(command.Subject is MsaSubject))
            {
                throw new NotSupportedException();
            }

            int count = 0;
            using (ExportPipeline export = ExportPipelineFactory.CreateMemoryPipeline(this.logger, this.destination, false))
            {
                foreach (DataTypeId dataType in command.PrivacyDataTypes)
                {
                    if (!this.data.TryGetValue(dataType, out List<Tuple<DateTimeOffset, string, object>> dataSet))
                    {
                        continue;
                    }

                    foreach (Tuple<DateTimeOffset, string, object> row in dataSet)
                    {
                        await export.ExportAsync(ExportProductId.Unknown, dataType, row.Item1, row.Item2, row.Item3).ConfigureAwait(false);
                        count++;
                    }
                }
            }

            await command.CheckpointAsync(CommandStatus.Complete, count).ConfigureAwait(false);
        }
    }
}
