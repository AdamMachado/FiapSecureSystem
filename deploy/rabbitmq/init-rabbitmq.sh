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

ANALYSIS_EXCHANGE="analysis.exchange"
REPORT_EXCHANGE="report.exchange"

PROCESSING_ANALYSIS_REQUESTED_QUEUE="processing.analysis.requested"

UPLOAD_ANALYSIS_STARTED_QUEUE="upload.analysis.started"
UPLOAD_ANALYSIS_COMPLETED_QUEUE="upload.analysis.completed"
UPLOAD_ANALYSIS_FAILED_QUEUE="upload.analysis.failed"

REPORT_ANALYSIS_COMPLETED_QUEUE="report.analysis.completed"

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
  EXCHANGE="$1"
  QUEUE="$2"
  ROUTING_KEY="$3"

  curl -fsu "${RMQ_USER}:${RMQ_PASS}" \
    -H "content-type: application/json" \
    -X POST "${BASE_URL}/bindings/${VHOST_ENCODED}/e/${EXCHANGE}/q/${QUEUE}" \
    -d "{
      \"routing_key\":\"${ROUTING_KEY}\",
      \"arguments\":{}
    }"
}

echo "Declaring exchanges..."
put_exchange "${ANALYSIS_EXCHANGE}"
put_exchange "${REPORT_EXCHANGE}"

echo "Declaring processing queues..."
put_queue "${PROCESSING_ANALYSIS_REQUESTED_QUEUE}"

echo "Declaring upload queues..."
put_queue "${UPLOAD_ANALYSIS_STARTED_QUEUE}"
put_queue "${UPLOAD_ANALYSIS_COMPLETED_QUEUE}"
put_queue "${UPLOAD_ANALYSIS_FAILED_QUEUE}"

echo "Declaring report queues..."
put_queue "${REPORT_ANALYSIS_COMPLETED_QUEUE}"

echo "Binding processing queues..."
bind_queue "${ANALYSIS_EXCHANGE}" "${PROCESSING_ANALYSIS_REQUESTED_QUEUE}" "analysis.requested"

echo "Binding upload queues..."
bind_queue "${ANALYSIS_EXCHANGE}" "${UPLOAD_ANALYSIS_STARTED_QUEUE}" "analysis.started"
bind_queue "${ANALYSIS_EXCHANGE}" "${UPLOAD_ANALYSIS_COMPLETED_QUEUE}" "analysis.completed"
bind_queue "${ANALYSIS_EXCHANGE}" "${UPLOAD_ANALYSIS_FAILED_QUEUE}" "analysis.failed"

echo "Binding report queues..."
bind_queue "${ANALYSIS_EXCHANGE}" "${REPORT_ANALYSIS_COMPLETED_QUEUE}" "analysis.completed"

echo "RabbitMQ topology initialization completed."