## Generate files with custom passphrase
```
 step ssh certificate 'email@email.email' --provisioner cineca-hpc key_01
```
## Remove passphrase

## Copy files to **general/ssh_agent/keys**
```
key_01
key_01-cert.pub
```

## Initialize SSH-Agent and Run
```
./app/scripts/init.sh
./app/scripts/add_keys 1 key_
```