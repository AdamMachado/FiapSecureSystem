#!/usr/bin/env bash
set -e

RABBITMQ_HOST="${RABBITMQ_HOST:-rabbitmq}"
RABBITMQ_USER="${RABBITMQ_USER:-fiap_app}"
RABBITMQ_PASS="${RABBITMQ_PASS:-fiap_app_dev_password}"

rabbitmqadmin_cmd() {
  rabbitmqadmin \
    --host="$RABBITMQ_HOST" \
    --username="$RABBITMQ_USER" \
    --password="$RABBITMQ_PASS" \
    "$@"
}

echo "Waiting for RabbitMQ management API..."

until rabbitmqadmin_cmd list exchanges > /dev/null 2>&1; do
  echo "RabbitMQ is not ready yet..."
  sleep 2
done

echo "RabbitMQ is ready."

echo "Declaring exchanges..."

rabbitmqadmin_cmd declare exchange name=analysis.exchange type=topic durable=true
rabbitmqadmin_cmd declare exchange name=analysis.retry.exchange type=direct durable=true
rabbitmqadmin_cmd declare exchange name=analysis.dlx type=direct durable=true

rabbitmqadmin_cmd declare exchange name=report.exchange type=topic durable=true
rabbitmqadmin_cmd declare exchange name=report.retry.exchange type=direct durable=true
rabbitmqadmin_cmd declare exchange name=report.dlx type=direct durable=true

echo "Declaring queues..."

declare_queue() {
  local exchange_prefix="$1"
  local queue="$2"
  local routing_key="$3"

  local retry_queue="${queue}.retry"
  local dlq="${queue}.dlq"

  local exchange="${exchange_prefix}.exchange"
  local exchange_retry="${exchange_prefix}.retry.exchange"
  local exchange_dlx="${exchange_prefix}.dlx"

  local main_queue_arguments
  local retry_queue_arguments

  main_queue_arguments="{\"x-dead-letter-exchange\":\"$exchange_retry\",\"x-dead-letter-routing-key\":\"$retry_queue\"}"
  retry_queue_arguments="{\"x-message-ttl\":30000,\"x-dead-letter-exchange\":\"$exchange\",\"x-dead-letter-routing-key\":\"$routing_key\"}"

  echo "Declaring queue: $queue"

  rabbitmqadmin_cmd declare queue \
    name="$queue" \
    durable=true \
    arguments="$main_queue_arguments"

  rabbitmqadmin_cmd declare binding \
    source="$exchange" \
    destination="$queue" \
    routing_key="$routing_key"

  echo "Declaring retry queue: $retry_queue"

  rabbitmqadmin_cmd declare queue \
    name="$retry_queue" \
    durable=true \
    arguments="$retry_queue_arguments"

  rabbitmqadmin_cmd declare binding \
    source="$exchange_retry" \
    destination="$retry_queue" \
    routing_key="$retry_queue"

  echo "Declaring DLQ: $dlq"

  rabbitmqadmin_cmd declare queue \
    name="$dlq" \
    durable=true

  rabbitmqadmin_cmd declare binding \
    source="$exchange_dlx" \
    destination="$dlq" \
    routing_key="${queue}.dead"
}

echo "Declaring Upload queues..."
declare_queue "analysis" "upload.analysis.started" "analysis.started"
declare_queue "analysis" "upload.analysis.completed" "analysis.completed"
declare_queue "analysis" "upload.analysis.failed" "analysis.failed"
declare_queue "report" "upload.report.generated" "report.generated"

echo "Declaring Processing queues..."
declare_queue "analysis" "processing.analysis.requested" "analysis.requested"

echo "Declaring Report queues..."
declare_queue "analysis" "report.analysis.completed" "analysis.completed"

echo "RabbitMQ FiapSecureSystems topology declared."