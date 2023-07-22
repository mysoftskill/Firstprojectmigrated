# Privacy AQS Worker

- [Privacy AQS Worker](#privacy-aqs-worker)
  - [What's the Point](#whats-the-point)
  - [Configuration](#configuration)
    - [Queues](#queues)
      - [Tracking Depth and Views](#tracking-depth-and-views)
  - [Accout Close](#accout-close)
  - [Account Create](#account-create)
    - [ID Mappings](#id-mappings)
    - [Scope Jobs and Xflow](#scope-jobs-and-xflow)
    - [Lock Concept](#lock-concept)
  - [HALP I'VE FALLEN BEHIND](#halp-ive-fallen-behind)
    - [So we're actually slow](#so-were-actually-slow)
      - [Increase Group/Work Taken Size](#increase-groupwork-taken-size)
      - [Increase Processor Count](#increase-processor-count)
  - [Miscellaneous](#miscellaneous)
    - [Useful Links and Tools](#useful-links-and-tools)
      - [Cosmos+Scope](#cosmosscope)
      - [MSA/Identity](#msaidentity)
    - [Distribution Groups](#distribution-groups)

## What's the Point

The privacy AQS worker serves 2 roles:

1. MSA Account Close
   1. User Initiated Account Close
   2. Account Creation Failure
   3. Age Out
2. MSA Account Create

## Configuration

The configuration file can be found [here](\Product\Common\Source\Configuration\Bond\PrivacyAqsWorkerConfiguration.bond).

The configuration enables us to read from multiple queues with different settings per queue. If you look at our [ini file](\Product\Deployment\Configurations\PrivacyAqsWorker\PrivacyAqsWorker.ini) we do this to read from two separate queues.

### Queues

- Mee-LiveIDNotifications - Account Create Notifications
- MeePXS-LiveIDNotifications - Account Close Notifications

#### Tracking Depth and Views

Depth is tracked in Geneva by the [AQS Team](mailto:MSAAQS@microsoft.com).

The AQS team has an alerting threshold on 10,000,000 items in the queue, we want to say way below this value (the 10,000,000 limit I believe is indicating they have started dropping messages).

- [Int Queue Depths](https://jarvis-west.dc.ad.msft.net/dashboard/share/F2783187)
- [Prod Queue Depths](https://jarvis-west.dc.ad.msft.net/dashboard/share/B868BF0D)

[Xpert Scenario View](https://xpert.microsoft.com/osg/views/04ad932e-0e16-4eeb-83ba-46156c03f53c?overrides=%7B%22Source%22%3A%22Environment%3DPROD%3B%22%2C%22Duration%22%3A%22PT1H%22%7D?overrides=%7B%22Source%22%3A%22Environment%3DPROD%3B%22%2C%22Sources%22%3A%5B%22Environment%3DPROD%3B%22%5D%2C%22Duration%22%3A%22PT1H%22%2C%22TimeWindows%22%3A%5B%7B%22StartTime%22%3A%222019-04-11T21%3A01%3A24.609Z%22%2C%22EndTime%22%3A%222019-04-11T22%3A01%3A24.609Z%22%2C%22Duration%22%3A%22PT1H%22%2C%22Name%22%3A%22Default%22%7D%5D%7D) - Custom View for events coming through, times in queue, account close metrics (had xbox account, etc), kinds of failures being seen.

## Accout Close

Please see the [AgeOut](https://microsoft.sharepoint.com/teams/ngphome/ngpx/execution/_layouts/15/Doc.aspx?sourcedoc=%7B8C3A8551-5F48-4AA7-9D8D-C2BC4DFCA87F%7D&file=NGP%20MSA%20Age%20Out%20Design.docx&action=default&mobileredirect=true&DefaultItemOpen=1) doc. It has been recently updated.

## Account Create

The old doc can be found [here](https://microsoft.sharepoint.com/teams/ngphome/ngpx/execution/_layouts/15/Doc.aspx?OR=teams&action=edit&sourcedoc={D42409CE-5DD3-41F5-ACF6-8C2515C3E7B2}). The purpose and high level are correct.

### ID Mappings

Account create generates [mappings](https://be.cosmos15.osdinfra.net/cosmos/PXSCosmos15.Prod/local/puidmapping/) that are used by other teams.

- [Anid](https://be.cosmos15.osdinfra.net/cosmos/PXSCosmos15.Prod/local/puidmapping/PROD/ANID/)
- [Opid](https://be.cosmos15.osdinfra.net/cosmos/PXSCosmos15.Prod/local/puidmapping/PROD/OPID/)
- [Cid](https://be.cosmos15.osdinfra.net/cosmos/PXSCosmos15.Prod/local/puidmapping/PROD/CID/)

There is a [TEST](https://be.cosmos15.osdinfra.net/cosmos/PXSCosmos15.Prod/local/puidmapping/TEST/) with INT data, and [PROD](https://be.cosmos15.osdinfra.net/cosmos/PXSCosmos15.Prod/local/puidmapping/PROD/) with, well PROD data.

Thesse mappings are generated on cosmos15 and are then copied to other cosmos VCs based on need (currently just cosmos08). Everything up to the copy is handled by former [pxs](mailto:meepxseng@microsoft.com), the copy is owned by [privacy services](mailto:meepseng@microsoft.com) (I haven't had issues with this).

### Scope Jobs and Xflow

We have multiple scope scripts, the project is [PuidMappingScopeScript](\Product\CosmosScripts\PuidMappingScopeScript\PuidMappingScopeScript.scopeproj).

These scripts are ran in our [xflow job](https://asimov-xflow/Workflows/Details/Prod.PuidMapping), which **is not automatically deployed**. It is possible to add auto deployment, but the updates are rarely made so it hasn't been done.

There are multiple scripts that get ran that help generate the mapping:

1. [Daily Cooker](\Product\CosmosScripts\PuidMappingScopeScript\Scope.script) - This script parses the raw cosmos streams written by PrivacyAqsWorker, the xflow config can be found [here](https://asimov-xflow/Configs/Details/pxs_puidmapping_prod) and will need to be manually updated.
   - The INT cooker lives in a separate [xflow config](https://asimov-xflow/Configs/Details/TEST.PuidMappingDailyCooker) - Please test updates here first.
2. [Daily Rollup](\Product\CosmosScripts\PuidMappingScopeScript\DailyRollup.script) - This script takes the cooked daily streams and generates a new overall rollup, the xflow config can be found [here](https://asimov-xflow/Configs/Details/PuidMappingDailyRollup) and will need to be manually updated.
   - Currently there is not INT daily rollup, but if needed you can add one to test with.

Xflow updates currently happen in the webpage manually.

### Lock Concept

In account create we use lease blobs as locks for safely writing to cosmos streams from multiple workers/threads. The scope job is configured to look for up to 50 different cosmos hourly streams, currently there are 12 being written to (see the CosmosWritterLimit in the [config](\Product\Deployment\Configurations\PrivacyAqsWorker\PrivacyAqsWorker.ini)). This enables us to increase the writer limit up to 50 without needing to update the scope scripts.

## HALP I'VE FALLEN BEHIND

So... The queues provided by AQS are filling up faster than we're consuming.

AQS will fire a SEV3 on a team once their queue depth hits 10 million, queue depths are estimates from AQS's side, and we can create our own alerts using geneva. Links for the AQS depth views are in [Tracking Depth and Views](#tracking-depth-and-views).

We also have a view in [xpert](https://xpert.microsoft.com/osg/views/04ad932e-0e16-4eeb-83ba-46156c03f53c?overrides=%7B%22Source%22%3A%22Environment%3DPROD%3B%22%2C%22Duration%22%3A%22PT1H%22%7D?overrides=%7B%22Source%22%3A%22Environment%3DPROD%3B%22%2C%22Sources%22%3A%5B%22Environment%3DPROD%3B%22%5D%2C%22Duration%22%3A%22PT1H%22%2C%22TimeWindows%22%3A%5B%7B%22StartTime%22%3A%222019-04-11T21%3A01%3A24.609Z%22%2C%22EndTime%22%3A%222019-04-11T22%3A01%3A24.609Z%22%2C%22Duration%22%3A%22PT1H%22%2C%22Name%22%3A%22Default%22%7D%5D%7D) where we track how long items have been in our queue for, this can be used to help detect if we're falling behind, but isn't accurate (we try to mellow out spikes, so we might see an increase in time in queue, but the queue depth is fine).

1. Why are we falling behind?
   1. Is it partner is down? - Get them to get back up! [(contact page)](https://microsoft.sharepoint-df.com/teams/NGPCommonInfra/SiteAssets/NGP%20Common%20Infra%20Notebook/PCF-PXS-DOD.one#Partner%20Contacts&section-id={99CBD37C-67D2-47F0-AB7B-4CA04E5D2178}&page-id={545DF083-A26B-47A9-AD75-92D7502121E7}&end)
   2. Bad deployment? - [Rollback!](https://microsoft.visualstudio.com/Universal%20Store/_release?definitionId=1575)
   3. We're actually slow...

### So we're actually slow

When we're being slow, there's multiple ways to improve our performance (these are all config changes, not actual code changes):

1. Increase the group size taken for the queue - This allows us to take more work per processor and will reduce calls to partners that support batch operations.
2. Increase the number of processors on the queue - This will allow us to have more threads taking work in parallel and will increase calls to partners.

There's other minor things we can do as well if we see issues with specific partners/services. If it's cosmos, we can increase the number of streams we allow writing in paralell to.

#### Increase Group/Work Taken Size

Currently we request up to 50 groups at a time (create) and 1 group at a time (delete), AQS supports requesting at most 100 groups at a time (we can request more, but it will cap at the internal max), please note that increase the group size will also increase the ammount of time it takes to receive the work.

| Queue Name                 | Group Size Limit | Why                                                                                                                                                                                                                                  |
| -------------------------- | ---------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| MeePXS-LiveIdNotifications | 200 (100)        | Xbox GetXuids only supports upto 200 accounts per request, we can up this limit internally by [updating the xbox adapter](https://msdata.visualstudio.com/ADG_Compliance_Services/_workitems/edit/407888)                            |
| Mee-LiveIdNotifications    | ? (100)          | MSA SAPI for getting Cids currently has no limit set when calling, Cosmos has a 4MB limit when writing to streams, we currently don't check the size of data being sent to cosmos per batch, but I don't think we're close to it yet |

#### Increase Processor Count

Processor count will increase our CPU utilization.

When increasing processors on Mee-LiveIdNotifications be aware of the cosmos limits and lease lock that occurs there.

| Queue Name                 | Processor Count Limit | Why                                                                                                             |
| -------------------------- | --------------------- | --------------------------------------------------------------------------------------------------------------- |
| MeePXS-LiveIdNotifications | ?                     | Xbox requests we keep RPS under 100 RPS, increase our processor count will increase the number of calls to Xbox |
| Mee-LiveIdNotifications    | ?                     | Only partners called are MSA and Cosmos                                                                         |

## Miscellaneous

### Useful Links and Tools

#### Cosmos+Scope

[Cosmos Home](https://aka.ms/cosmos) - Useful for starting Cosmos/Scope Development
[Cosmos Support](https://cosmossupport/) - Very useful if we start seeing connection issues with cosmos
[Scope Studio](https://aka.ms/scopestudio) - Extensions to develop scope in VS2017+2015 hopefully soon 2019...
[Cosmos PowerShell](https://aka.ms/CosmosPowerShell) - PowerShell Modules for interacting with Cosmos Streams, does not support PowerShell-Core
[VS Code Scope](https://marketplace.visualstudio.com/itemdetails?itemName=yiwwan.vscode-scope) - Syntax Highlighter for SCOPE Scripts

#### MSA/Identity

[Identity Docs](https://identitydocs.azurewebsites.net/static/msa/idsapi.html) - MSA/AAD Identity Docs
[MSA SAPI Docs](https://microsoft.sharepoint.com/teams/liveid/docs/idsapi/webframe.html) - For browsing SAPI

### Distribution Groups

In [idweb](https://idweb) you can search for DGs. Here are some recommended ones to join(?), some are good for just shooting help messages at (worth joining to check if someone else had the same question/issue first)

| Group Alias                                 | Display Name                        | Good For                                                                                                                                 |
| ------------------------------------------- | ----------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------- |
| [cosmosan](mailto:cosmosan@microsoft.com)   | Cosmos Announcement                 | Get announcements about cosmos work/breaks                                                                                               |
| [cosmdisc](mailto:cosmosdisc@microsoft.com) | Cosmos Discussion                   | Ask for assistance on cosmos related issues (don't have to join, but can be aware for asking questions)                                  |
| [idtalk](mailto:idtalk@microsoft.com)       | Microsoft Account Discussion        | Get help or information on developing with MSA SAPI or other MSA related issues (don't have to join, but be aware it exists)             |
| [idinfo](mailto:idinfo@microsoft.com)       | MSA & AAD Integration Announcements | Get announcements on work going on with MSA/AAD account integration (they'll announce changes or issues happening)                       |
| [msaaqs](mailto:msaaqs@microsoft.com)       | MSA Modern AQS                      | Bring up issues, ask for help, etc. It's actually useful because you'll catch some issues early from seeing what others are running into |
| [xpertout](mailto:xpertout@microsoft.com)   | Xpert Outage Communications         | This is useful to subscribe to when on call, we have data absense alerts, so it's good to know if it's real or xpert                     |
| [scope](mailto:Ascope@microsoft.com)        | SCOPE Discussion                    | Alias for getting help with scope script development/issues (don't have to join, but be aware it exists)                                 |
| [meepseng](mailto:meepseng@microsoft.com)   | Privacy Services                    | Folks on the team who worked on the Audit Pipeline                                                                                       |
