#!/bin/sh
set -e

HOST="${RABBITMQ_HOST:-rabbitmq}"
PORT="${RABBITMQ_MANAGEMENT_PORT:-15672}"
RMQ_USER="${RABBITMQ_DEFAULT_USER:-guest}"
RMQ_PASS="${RABBITMQ_DEFAULT_PASS:-guest}"
VHOST="${RABBITMQ_VHOST:-/}"

encode_vhost() {
  if [ "$1" = "/" ]; then
    printf "%%2F"
  else
    printf "%s" "$1"
  fi
}

VHOST_ENCODED="$(encode_vhost "$VHOST")"
BASE_URL="http://${HOST}:${PORT}/api"

echo "Waiting for RabbitMQ Management API..."
until curl -fsu "${RMQ_USER}:${RMQ_PASS}" "${BASE_URL}/overview" >/dev/null 2>&1; do
  sleep 3
done

echo "RabbitMQ Management API is available."

put_exchange() {
  NAME="$1"
  curl -fsu "${RMQ_USER}:${RMQ_PASS}" \
    -H "content-type: application/json" \
    -X PUT "${BASE_URL}/exchanges/${VHOST_ENCODED}/${NAME}" \
    -d '{
      "type":"topic",
      "durable":true,
      "auto_delete":false,
      "internal":false,
      "arguments":{}
    }'
}

put_queue() {
  NAME="$1"
  curl -fsu "${RMQ_USER}:${RMQ_PASS}" \
    -H "content-type: application/json" \
    -X PUT "${BASE_URL}/queues/${VHOST_ENCODED}/${NAME}" \
    -d '{
      "durable":true,
      "auto_delete":false,
      "exclusive":false,
      "arguments":{}
    }'
}

bind_queue() {
  QUEUE="$1"
  ROUTING_KEY="$2"
  curl -fsu "${RMQ_USER}:${RMQ_PASS}" \
    -H "content-type: application/json" \
    -X POST "${BASE_URL}/bindings/${VHOST_ENCODED}/e/analysis.exchange/q/${QUEUE}" \
    -d "{
      \"routing_key\":\"${ROUTING_KEY}\",
      \"arguments\":{}
    }"
}

echo "Declaring exchanges..."
put_exchange "analysis.exchange"
put_exchange "report.exchange"

echo "Declaring analysis queues..."
put_queue "analysis.requested"
put_queue "analysis.started"
put_queue "analysis.completed"
put_queue "analysis.failed"

echo "Binding analysis queues..."
bind_queue "analysis.requested" "analysis.requested"
bind_queue "analysis.started" "analysis.started"
bind_queue "analysis.completed" "analysis.completed"
bind_queue "analysis.failed" "analysis.failed"

echo "RabbitMQ topology initialization completed."