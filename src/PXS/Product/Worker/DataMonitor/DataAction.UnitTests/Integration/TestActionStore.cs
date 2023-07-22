// ---------------------------------------------------------------------------
// <copyright file="TestActionStore.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.UnitTests.Integration
{
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.DataMonitor.DataAction.Actions;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Data;

    public class TestActionStore
    {
        public static List<ActionDef> Actions =
            new List<ActionDef>
            {
                new ActionDef
                {
                    Tag = "KustoIncidentFile",
                    Type = ActionSet.ActionType,
                    Def = TestActionStore.KustoIncidentFileDef
                },

                new ActionDef
                {
                    Tag = "TimeApplicabilityCheck",
                    Type = TimeApplicabilityAction.ActionType,
                    Def = TestActionStore.TimeAppicability
                },

                new ActionDef
                {
                    Tag = "KustoIncidentFileRefArgsDef",
                    Type = ActionSet.ActionType,
                    Def = TestActionStore.KustoIncidentFileRefArgsDef
                },
            };

        private const string KustoIncidentFileDef =
@"
{
  'Actions':
  [
    {
      'ExecutionOrder': 0,
      'Tag': 'TimeApplicabilityCheck'
    },

    {
      'ExecutionOrder': 1,
      'ResultTransform': { 'Consts': { 'Select': '$' } },
      'Inline':
      {
        'Tag': 'ConstActionSetDef',
        'Type': 'MODELBUILD-CONST',
        'Def':
        {
          'Severity': 4,
          'EmailFrom': 'ngpincidentfiler@microsoft.com',
          'LockGroupName': 'Sev4IncidentFiling',
          'ExcludedAgents': ""'Agent2','Agent3'""
        }
      }
    },

    {
      'ExecutionOrder': 2,
      'ResultTransform': { 'Agents': { 'Select': 'Table00' } },
      'ArgTransform':
      {
        'CounterSuffix': { 'Const': 'KustoQuerySuffix' }
      },
      'Inline':
      {
        'Tag': 'FindBadAgents',
        'Type': 'MODELBUILD-QUERY-KUSTO',
        'Def':
        {
          'ClusterUrl': 'https://ngpreporting.kusto.windows.net',
          'Database': 'Ngpreporting',
          'Query':
          {
            'TemplateTag': 'KustoQuery',
            'Parameters': { 'Consts': { 'Select': '$.Consts' } }
          }
        }
      }
    },

    {
      'ExecutionOrder': 3,
      'ResultTransform': { 'SentIncidents': { 'Select': 'Incidents' } },
      'ArgTransform':
      {
        'CollectionItemKeyPropertyName': { 'Const': 'AgentId' },
        'Collection': { 'Select': '$.Agents' },
        'DataRowPropertyName': { 'Const': 'AgentDataRow' }
      },
      'Inline':
      {
        'Tag': 'LoopOverKustoResults',
        'Type': 'LOOP-DATASET',
        'Def':
        {
          'Actions':
          [
            {
              'ExecutionOrder': 0,
              'ArgTransform':
              {
                'LockGroupName': { 'Select': '$.Consts.LockGroupName' },
                'LockName': { 'Select': '$.AgentDataRow.AgentId' },
                'RunFrequency': { 'Const': '0.23:00:00' },
                'LeaseTime': { 'Const': '00:30:00' },
              },
              'Inline':
              {
                'Tag': 'LockAgent',
                'Type': 'LOCK-TABLE',
                'Def':
                {
                  'Actions':
                  [
                    {
                      'ExecutionOrder': 0,
                      'ResultTransform': { 'Incidents': { 'Select': '$', 'Mode': 'ArrayAdd' } },
                      'ArgTransform':
                      {
                        'AgentId': { 'Select': '$.AgentDataRow.AgentId' },
                        'Severity': { 'Const': 3 },
                        'CounterSuffix': { 'Const': 'SentIncidentsSuffix' }
                      },
                      'Inline':
                      {
                        'Tag': 'SubmitIncident',
                        'Type': 'TRANSMIT-INCIDENT',
                        'Def':
                        {
                          'Keywords': { 'Inline': 'Keywords' },
                          'Title': 
                          { 
                            'Inline': ""Incident for agent [[<var s:'$.AgentInfo.AgentId' f:n0 var>]]"",
                            'Parameters': { 'AgentInfo': { 'Select': '$.AgentDataRow' } }
                          },
                          'Body':
                          {
                            'TemplateTag': 'IncidentBody',
                            'Parameters': { 'AgentInfo': { 'Select': '$.AgentDataRow' } }
                          },
                          'EventName': 'DeleteAlert'
                        }
                      }
                    }
                  ]
                }
              }
            }
          ]
        }
      }
    },

    {
      'ExecutionOrder': 4,
      'ResultTransform': { 'CurrentTime': { 'Select': '#.Time.Now.Local(""Pacific Standard Time"")' } },
      'Inline':
      {
        'Tag': 'PopulateCurrentTime',
        'Type': 'MODELBUILD-TRANSFORM',
      }
    },

    {
      'ExecutionOrder': 5,
      'ResultTransform': { 'MailResult': { 'Select': '$' } },
      'ArgTransform':
      {
        'To': { 'Const': [ 'ngpincientresults@microsoft.com' ] },
        'CounterSuffix': { 'Const': 'SendEmailSuffix' }
      },
      'Inline':
      {
        'Tag': 'SendResultEmail',
        'Type': 'TRANSMIT-EMAIL',
        'Def':
        {
          'Subject': 
          { 
            'Inline': ""Incidents filed at [[<var s:'$.CurrentTime' f:'yyyy-MM-dd HH:mm:ss' var>]]"",
            'Parameters': { 'CurrentTime': { 'Select': '#.Time.Now.Local(""Pacific Standard Time"")' } }
          },
          'Body':
          {
            'TemplateTag': 'EmailBody',
            'Parameters': { 'Incidents': { 'Select': '$.SentIncidents' } }
          },
          'ReplyToAddress': 'derekm@microsoft.com',
          'FromDisplayName': 'NGP Incident Filer',
          'FromAddress': 'derekm@microsoft.com',
          'Priority': 'High'
        }
      }
    }
  ]
}
";

        private const string KustoIncidentFileRefArgsDef =
@"
{
  'Actions':
  [
    {
      'ExecutionOrder': 0,
      'Tag': 'TimeApplicabilityCheck'
    },

    {
      'ExecutionOrder': 2,
      'ResultTransform': { 'Agents': { 'Select': 'Table00' } },
      'ArgTransform':
      {
        'CounterSuffix': { 'Const': 'KustoQuerySuffix' }
      },
      'Inline':
      {
        'Tag': 'FindBadAgents',
        'Type': 'MODELBUILD-QUERY-KUSTO',
        'Def':
        {
          'ClusterUrl': 'https://ngpreporting.kusto.windows.net',
          'Database': 'Ngpreporting',
          'Query':
          {
            'TemplateTag': 'KustoQuery',
            'Parameters': { 'Consts': { 'Select': '$.Consts' } }
          }
        }
      }
    },

    {
      'ExecutionOrder': 3,
      'ResultTransform': { 'SentIncidents': { 'Select': 'Incidents' } },
      'ArgTransform':
      {
        'CollectionItemKeyPropertyName': { 'Const': 'AgentId' },
        'Collection': { 'Select': '$.Agents' },
        'DataRowPropertyName': { 'Const': 'AgentDataRow' }
      },
      'Inline':
      {
        'Tag': 'LoopOverKustoResults',
        'Type': 'LOOP-DATASET',
        'Def':
        {
          'Actions':
          [
            {
              'ExecutionOrder': 0,
              'ArgTransform':
              {
                'LockGroupName': { 'Select': '$.Consts.LockGroupName' },
                'LockName': { 'Select': '$.AgentDataRow.AgentId' },
                'RunFrequency': { 'Const': '0.23:00:00' },
                'LeaseTime': { 'Const': '00:30:00' },
              },
              'Inline':
              {
                'Tag': 'LockAgent',
                'Type': 'LOCK-TABLE',
                'Def':
                {
                  'Actions':
                  [
                    {
                      'ExecutionOrder': 0,
                      'ResultTransform': { 'Incidents': { 'Select': '$', 'Mode': 'ArrayAdd' } },
                      'ArgTransform':
                      {
                        'AgentId': { 'Select': '$.AgentDataRow.AgentId' },
                        'Severity': { 'Const': 3 },
                        'CounterSuffix': { 'Const': 'SentIncidentsSuffix' }
                      },
                      'Inline':
                      {
                        'Tag': 'SubmitIncident',
                        'Type': 'TRANSMIT-INCIDENT',
                        'Def':
                        {
                          'Keywords': { 'Inline': 'Keywords' },
                          'Title': 
                          { 
                            'Inline': ""Incident for agent [[<var s:'$.AgentInfo.AgentId' f:n0 var>]]"",
                            'Parameters': { 'AgentInfo': { 'Select': '$.AgentDataRow' } }
                          },
                          'Body':
                          {
                            'TemplateTag': 'IncidentBody',
                            'Parameters': { 'AgentInfo': { 'Select': '$.AgentDataRow' } }
                          },
                          'EventName': 'DeleteAlert'
                        }
                      }
                    }
                  ]
                }
              }
            }
          ]
        }
      }
    },

    {
      'ExecutionOrder': 4,
      'ResultTransform': { 'CurrentTime': { 'Select': '#.Time.Now.Local(""Pacific Standard Time"")' } },
      'Inline':
      {
        'Tag': 'PopulateCurrentTime',
        'Type': 'MODELBUILD-TRANSFORM',
      }
    },

    {
      'ExecutionOrder': 5,
      'ResultTransform': { 'MailResult': { 'Select': '$' } },
      'ArgTransform':
      {
        'To': { 'Const': [ 'ngpincientresults@microsoft.com' ] },
        'CounterSuffix': { 'Const': 'SendEmailSuffix' }
      },
      'Inline':
      {
        'Tag': 'SendResultEmail',
        'Type': 'TRANSMIT-EMAIL',
        'Def':
        {
          'Subject': 
          { 
            'Inline': ""Incidents filed at [[<var s:'$.CurrentTime' f:'yyyy-MM-dd HH:mm:ss' var>]]"",
            'Parameters': { 'CurrentTime': { 'Select': '#.Time.Now.Local(""Pacific Standard Time"")' } }
          },
          'Body':
          {
            'TemplateTag': 'EmailBody',
            'Parameters': { 'Incidents': { 'Select': '$.SentIncidents' } }
          },
          'ReplyToAddress': 'derekm@microsoft.com',
          'FromDisplayName': 'NGP Incident Filer',
          'FromAddress': 'derekm@microsoft.com',
          'Priority': 'High'
        }
      }
    }
  ]
}
";


        private const string TimeAppicability =
@"
{
  'AllowedDaysAndTimes': { 'Weekday': [ { 'Start': '11:00:00', 'End':  '17:00:00' } ] },
  'Overrides': 
  { 
    '2018-07-04': [ { 'Exclude':  true } ],
    '2018-09-03': [ { 'Exclude':  true } ],
    '2018-11-22': [ { 'Exclude':  true } ],
    '2018-11-23': [ { 'Exclude':  true } ],
    '2018-12-24': [ { 'Exclude':  true } ],
    '2018-12-25': [ { 'Exclude':  true } ]
  }
}
";
    }
}
