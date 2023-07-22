# Instructions for running 'Device Delete' Scripts:

## Summary
These scripts are used to inject device id deletes into Azure Queue, which are then processed by the Vortex Device Delete worker.

## Pre-requisites
This assumes you have a service change record created before you run anything here. You must always acquire SAS tokens to be able to access the storage account for which the device delete queue resides. Script instructions also go into the details for access required.

## Instructions

1) Get data from partner team and save to a text file on local machine running scripts.
a. Each row of data in the file is expected to be a new global device id in the format g:12345 where 12345 is the device id in decimal format.

2) Run the script 'DeviceDeleteCsvToTable' to import data into ATS. This script won't send traffic to partners, it is just a way of getting device id's into a table storage that we will use for state tracking (so we can pause this if we need to).

3) Run the script 'DeviceDeleteTableToQueue' to start sending device id's into the device delete queue. See the top of the script for instructions. This DOES send traffic to partners. It can be aborted at any time.

