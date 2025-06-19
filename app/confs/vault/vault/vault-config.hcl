ui            = true
api_addr      = "http://localhost:8200"
disable_mlock = true
tls_skip_verify = true
log_level = "info"

storage "file" {
  path = "/vault/data"
}

listener "tcp" {
  address = "0.0.0.0:8200"
  tls_disable = true
}
