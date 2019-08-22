## Overview

This repository contains a .NET implementation of the Kafka retry system defined in Uber Engineering's [Building Reliable Reprocessing and Dead Letter Queues with Apache Kafka](https://eng.uber.com/reliable-reprocessing/). 

The system consists of three Kafka topics:
* Primary processing queue, which handles first-try attempts at consuming messages.
* Delayed processing queue, which holds initially failed messages to be reprocessed after a certain amount of elapsed time.
* Deadletter queue, containing messages which have also failed reprocessing and must be handled by manual intervention.

This project contains a message producer, a consumer which handles the initial "first-try" processing, and a consumer which handles reprocessing from the second queue, and if necesssary, adds failed messages to the deadletter queue. 

Each message is designed as an "update" operation to be executed on a database. For simplicity, Microsoft's classic [Northwind](https://github.com/Microsoft/sql-server-samples/tree/master/samples/databases/northwind-pubs) database is used as the target to be altered. 

## Producer Logic

The producer simply provides a key/value pair (int, string) indicating the primary key of a record in the `[Employees]` table to be updated, and a name value to be updated on the corresponding employee record. The producer also generates update operations for some record IDs which are not present to act as seed data for the failure cases.

## Consumer Logic

Two consumers are provided, the `Consumer` and `RetryConsumer`. These are functionally identical to one another, with different configuration values that set them to read from different topics.

Each consumer will:
* Pull a message from its primary topic.
* Attempt to perform a SQL `UPDATE` statement on the specified row to alter the Employee's `FirstName` field to the value in the message.
* If a failure occurs or no rows are updated, commit the message to the next retry queue or deadletter queue for further processing. 

### Retrying

Since Kafka lacks a time-delay delivery feature and consumers cannot process messages out of order, a consumer must perform a blocking wait until the delay time is satisfied if a message is pulled from a retry queue early. This behavior does not impact the primary processing queue, but does impact retry queues. Uber writes _"This mechanism follows a leaky bucket pattern where flow rate is expressed by the blocking nature of the delayed message consumption within the retry queues. Consequently, our queues are not so much retry queues as they are delayed processing queues, where the re-execution of error cases is our best-effort delivery: handler invocation will occur at least after the configured timeout but possibly later."_

## Build and Run

Run `docker-compose up -d` in the root folder to initialize the Confluent cluster and Northwind database locally.

Run each project individually and view the results of processing in the Confluent dashboard.