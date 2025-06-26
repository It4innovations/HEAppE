pid_file = "/tmp/pidfile"

auto_auth {
  method {
    type = "approle"

    config = {
      role_id_file_path = "/vault/config/role_id"
      secret_id_file_path = "/vault/config/secret_id"
      remove_secret_id_file_after_reading = false
    }
  }

  sink {
    type = "file"
    config = {
      path = "/tmp/token"
    }
  }
}

cache {
}

api_proxy {
  use_auto_auth_token = "force"
  enforce_consistency = "always"
}

listener "tcp" {
   address = "0.0.0.0:8100"
   tls_disable = true
}

vault {
  tls_skip_verify = 1
  address = "http://vault:8200"
    retry {
    num_retries = 3
  }
}
