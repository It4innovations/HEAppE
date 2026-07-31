{{/*
Expand the name of the chart.
*/}}
{{- define "heappe.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create a default fully qualified app name.
*/}}
{{- define "heappe.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{/*
Create chart name and version as used by the chart label.
*/}}
{{- define "heappe.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Common labels
*/}}
{{- define "heappe.labels" -}}
helm.sh/chart: {{ include "heappe.chart" . }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
app.kubernetes.io/part-of: heappe
{{- end }}

{{/*
Selector labels for heappe RestApi
*/}}
{{- define "heappe.api.selectorLabels" -}}
app.kubernetes.io/name: {{ include "heappe.name" . }}-api
app.kubernetes.io/instance: {{ .Release.Name }}
app.kubernetes.io/component: api
{{- end }}

{{/*
Selector labels for datastagingapi
*/}}
{{- define "heappe.datastaging.selectorLabels" -}}
app.kubernetes.io/name: {{ include "heappe.name" . }}-datastaging
app.kubernetes.io/instance: {{ .Release.Name }}
app.kubernetes.io/component: datastaging
{{- end }}

{{/*
Selector labels for mssql
*/}}
{{- define "heappe.mssql.selectorLabels" -}}
app.kubernetes.io/name: {{ include "heappe.name" . }}-mssql
app.kubernetes.io/instance: {{ .Release.Name }}
app.kubernetes.io/component: database
{{- end }}

{{/*
Selector labels for vault
*/}}
{{- define "heappe.vault.selectorLabels" -}}
app.kubernetes.io/name: {{ include "heappe.name" . }}-vault
app.kubernetes.io/instance: {{ .Release.Name }}
app.kubernetes.io/component: vault
{{- end }}

{{/*
Selector labels for vault agent
*/}}
{{- define "heappe.vaultagent.selectorLabels" -}}
app.kubernetes.io/name: {{ include "heappe.name" . }}-vaultagent
app.kubernetes.io/instance: {{ .Release.Name }}
app.kubernetes.io/component: vaultagent
{{- end }}

{{/*
Selector labels for otel-collector
*/}}
{{- define "heappe.otelcollector.selectorLabels" -}}
app.kubernetes.io/name: {{ include "heappe.name" . }}-otel-collector
app.kubernetes.io/instance: {{ .Release.Name }}
app.kubernetes.io/component: otel-collector
{{- end }}

{{/*
Selector labels for jaeger
*/}}
{{- define "heappe.jaeger.selectorLabels" -}}
app.kubernetes.io/name: {{ include "heappe.name" . }}-jaeger
app.kubernetes.io/instance: {{ .Release.Name }}
app.kubernetes.io/component: jaeger
{{- end }}

{{/*
MSSQL fully qualified name
*/}}
{{- define "heappe.mssql.fullname" -}}
{{- printf "%s-mssql" (include "heappe.fullname" .) }}
{{- end }}

{{/*
Vault fully qualified name
*/}}
{{- define "heappe.vault.fullname" -}}
{{- printf "%s-vault" (include "heappe.fullname" .) }}
{{- end }}

{{/*
Vault Agent fully qualified name
*/}}
{{- define "heappe.vaultagent.fullname" -}}
{{- printf "%s-vaultagent" (include "heappe.fullname" .) }}
{{- end }}

{{/*
OTEL Collector fully qualified name
*/}}
{{- define "heappe.otelcollector.fullname" -}}
{{- printf "%s-otel-collector" (include "heappe.fullname" .) }}
{{- end }}

{{/*
Jaeger fully qualified name
*/}}
{{- define "heappe.jaeger.fullname" -}}
{{- printf "%s-jaeger" (include "heappe.fullname" .) }}
{{- end }}

{{/*
MSSQL connection string
*/}}
{{- define "heappe.mssql.connectionString" -}}
{{- if .Values.mssql.external.enabled }}
{{- .Values.mssql.external.connectionString }}
{{- else }}
{{- printf "Server=%s;Database=HEAppEDb;User Id=sa;Password=%s;TrustServerCertificate=True;Encrypt=Yes" (include "heappe.mssql.fullname" .) .Values.mssql.saPassword }}
{{- end }}
{{- end }}

{{/*
OTEL Collector endpoint
*/}}
{{- define "heappe.otelcollector.endpoint" -}}
{{- printf "http://%s:%d" (include "heappe.otelcollector.fullname" .) (int .Values.telemetry.otelCollector.grpcPort) }}
{{- end }}

{{/*
Vault Agent address
*/}}
{{- define "heappe.vaultagent.address" -}}
{{- if .Values.vault.external.enabled }}
{{- .Values.vault.external.address }}
{{- else }}
{{- printf "http://%s:%d" (include "heappe.vaultagent.fullname" .) (int .Values.vault.agent.port) }}
{{- end }}
{{- end }}
