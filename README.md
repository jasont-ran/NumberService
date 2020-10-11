# Number Service

Highly available sequential number generator backed by Cosmos DB with guaranteed uniqueness.

Free numbers here!

    PUT https://numberservice-aue.azurewebsites.net/api/numbers/free

## Requirements

* Each request for a new number must return a number that is unique and one greater than the last number generated (for all clients).
* Number should be able to be seeded (start at 10,000 for example), but only the first time it is generated
* Number service must be highly available with an uptime of around 99.99%, or less than 5 minutes of total downtime per month.
* RTO = minutes
* RPO = 0

## Phase 1

* 1 Cosmos DB Region. Zone Redundant.
* 1 Azure Functions (consumption) region
* 99.94% uptime. No region failover
* RPO and RTO for Zone failure = 0
* RPO for Region failure could be hours and RTO for Region failure could be days, assuming the Region never recovers. However an RPO of 0 and an RTO of hours is more likely IMO.
* ~14 RU/s per number
* Single partition (per key)
* Max RU/s per partition = 10,000, so max throughput is 625 per second
* At 400 RU/s provision, max throughput is 28 per second.
* Highest number currently supported (I assume) is Javascript `Number.MAX_SAFE_INTEGER`, which is 9,007,199,254,740,991.
* Stored proc write consistency is strong. If proc can't increment number in atomic operation it will fail with an exception that is thrown to client.
* Read consistency is the default (session). While out of sproc/session reads may not get the latest number, ordering will be consistent. Strong consistency of reads is not a requirement for NumberService.

### Costs

* Cosmos DB, single region (Australia East), ZR, 400 RU/s, 1GB data = NZ$51.21 /month
* Azure Functions, Consumption, Australia East, @28RPS = NZ$23.90 /month