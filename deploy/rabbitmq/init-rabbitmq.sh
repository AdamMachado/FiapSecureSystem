#!/bin/sh
set -eu

RABBITMQ_HOST="${RABBITMQ_HOST:-rabbitmq}"
RABBITMQ_MANAGEMENT_PORT="${RABBITMQ_MANAGEMENT_PORT:-15672}"

RABBITMQ_USER="${RABBITMQ_USER:-${RABBITMQ_USER:-fiap_app}}"
RABBITMQ_PASS="${RABBITMQ_PASS:-${RABBITMQ_PASS:-fiap_app_dev_password}}"
RABBITMQ_VHOST="${RABBITMQ_VHOST:-/}"

# Encode simples para vhost "/".
# Para o vhost padrão "/", a API espera "%2F".
VHOST_ENCODED="$(printf '%s' "$RABBITMQ_VHOST" | sed 's|/|%2F|g')"

API_BASE="http://${RABBITMQ_HOST}:${RABBITMQ_MANAGEMENT_PORT}/api"

curl_api() {
  method="$1"
  path="$2"
  data="${3:-}"

  if [ -n "$data" ]; then
    curl --fail-with-body -sS \
      -u "${RABBITMQ_USER}:${RABBITMQ_PASS}" \
      -H "content-type: application/json" \
      -X "$method" \
      -d "$data" \
      "${API_BASE}${path}"
  else
    curl --fail-with-body -sS \
      -u "${RABBITMQ_USER}:${RABBITMQ_PASS}" \
      -X "$method" \
      "${API_BASE}${path}"
  fi
}

echo "Waiting for RabbitMQ management API..."

attempt=1
max_attempts=120

until curl_api GET "/overview" >/dev/null 2>&1; do
  if [ "$attempt" -ge "$max_attempts" ]; then
    echo "RabbitMQ management API was not ready after ${max_attempts} attempts."
    echo "Last diagnostic attempt:"
    curl -v -u "${RABBITMQ_USER}:${RABBITMQ_PASS}" "${API_BASE}/overview" || true
    exit 1
  fi

  echo "RabbitMQ is not ready yet... attempt ${attempt}/${max_attempts}"
  attempt=$((attempt + 1))
  sleep 2
done

echo "RabbitMQ is ready."

declare_exchange() {
  exchange="$1"
  type="$2"

  echo "Declaring exchange: ${exchange}"

  curl_api PUT \
    "/exchanges/${VHOST_ENCODED}/${exchange}" \
    "{\"type\":\"${type}\",\"durable\":true,\"auto_delete\":false,\"internal\":false,\"arguments\":{}}" \
    >/dev/null
}

declare_queue() {
  exchange_prefix="$1"
  queue="$2"
  routing_key="$3"

  retry_queue="${queue}.retry"
  dlq="${queue}.dlq"

  exchange="${exchange_prefix}.exchange"
  exchange_retry="${exchange_prefix}.retry.exchange"
  exchange_dlx="${exchange_prefix}.dlx"

  echo "Declaring queue: ${queue}"

  curl_api PUT \
    "/queues/${VHOST_ENCODED}/${queue}" \
    "{\"durable\":true,\"auto_delete\":false,\"arguments\":{\"x-dead-letter-exchange\":\"${exchange_retry}\",\"x-dead-letter-routing-key\":\"${retry_queue}\"}}" \
    >/dev/null

  curl_api POST \
    "/bindings/${VHOST_ENCODED}/e/${exchange}/q/${queue}" \
    "{\"routing_key\":\"${routing_key}\",\"arguments\":{}}" \
    >/dev/null

  echo "Declaring retry queue: ${retry_queue}"

  curl_api PUT \
    "/queues/${VHOST_ENCODED}/${retry_queue}" \
    "{\"durable\":true,\"auto_delete\":false,\"arguments\":{\"x-message-ttl\":30000,\"x-dead-letter-exchange\":\"${exchange}\",\"x-dead-letter-routing-key\":\"${routing_key}\"}}" \
    >/dev/null

  curl_api POST \
    "/bindings/${VHOST_ENCODED}/e/${exchange_retry}/q/${retry_queue}" \
    "{\"routing_key\":\"${retry_queue}\",\"arguments\":{}}" \
    >/dev/null

  echo "Declaring DLQ: ${dlq}"

  curl_api PUT \
    "/queues/${VHOST_ENCODED}/${dlq}" \
    "{\"durable\":true,\"auto_delete\":false,\"arguments\":{}}" \
    >/dev/null

  curl_api POST \
    "/bindings/${VHOST_ENCODED}/e/${exchange_dlx}/q/${dlq}" \
    "{\"routing_key\":\"${queue}.dead\",\"arguments\":{}}" \
    >/dev/null
}

echo "Declaring exchanges..."

declare_exchange "analysis.exchange" "topic"
declare_exchange "analysis.retry.exchange" "direct"
declare_exchange "analysis.dlx" "direct"

declare_exchange "report.exchange" "topic"
declare_exchange "report.retry.exchange" "direct"
declare_exchange "report.dlx" "direct"

echo "Declaring Upload queues..."
declare_queue "analysis" "upload.analysis.started" "analysis.started"
declare_queue "analysis" "upload.analysis.completed" "analysis.completed"
declare_queue "analysis" "upload.analysis.failed" "analysis.failed"
declare_queue "report" "upload.report.generated" "report.generated"

echo "Declaring Processing queues..."
declare_queue "analysis" "processing.analysis.requested" "analysis.requested"
declare_queue "analysis" "processing.analysis.execution.requested" "analysis.execution.requested"

echo "Declaring Report queues..."
declare_queue "analysis" "report.analysis.completed" "analysis.completed"

echo "RabbitMQ FiapSecureSystems topology declared."
